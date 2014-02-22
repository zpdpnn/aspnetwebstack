// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Properties;

namespace System.Web.Http.Routing
{
    public class DefaultDirectRouteProvider : IDirectRouteProvider
    {
        public virtual IReadOnlyCollection<RouteEntry> GetDirectRoutes(
            HttpControllerDescriptor controllerDescriptor, 
            IReadOnlyCollection<HttpActionDescriptor> actionDescriptors,
            IInlineConstraintResolver constraintResolver)
        {
            List<RouteEntry> entries = new List<RouteEntry>();

            List<ReflectedHttpActionDescriptor> actionsWithoutRoutes = new List<ReflectedHttpActionDescriptor>();

            foreach (ReflectedHttpActionDescriptor action in actionDescriptors.OfType<ReflectedHttpActionDescriptor>())
            {
                IReadOnlyCollection<IDirectRouteFactory> factories = GetActionRouteFactories(action);

                // Ignore the Route attributes from inherited actions.
                if (action.MethodInfo != null &&
                    action.MethodInfo.DeclaringType != controllerDescriptor.ControllerType)
                {
                    factories = null;
                }

                if (factories != null && factories.Count > 0)
                {
                    IReadOnlyCollection<RouteEntry> actionEntries = GetActionDirectRoutes(action, factories, constraintResolver);
                    if (actionEntries != null)
                    {
                        entries.AddRange(actionEntries);
                    }
                }
                else
                {
                    // IF there are no routes on the specific action, attach it to the controller routes (if any).
                    actionsWithoutRoutes.Add(action);
                }
            }

            if (actionsWithoutRoutes.Count > 0)
            {
                IReadOnlyCollection<IDirectRouteFactory> controllerFactories = GetControllerRouteFactories(controllerDescriptor);
                if (controllerFactories != null && controllerFactories.Count > 0)
                {
                    IReadOnlyCollection<RouteEntry> controllerEntries = GetControllerDirectRoutes(
                        controllerDescriptor,
                        actionsWithoutRoutes, 
                        controllerFactories, 
                        constraintResolver);

                    if (controllerEntries != null)
                    {
                        entries.AddRange(controllerEntries);
                    }
                }
            }

            return entries;
        }

        protected virtual IReadOnlyCollection<IDirectRouteFactory> GetControllerRouteFactories(HttpControllerDescriptor controllerDescriptor)
        {
            Collection<IDirectRouteFactory> newFactories = controllerDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: false);

            Collection<IHttpRouteInfoProvider> oldProviders = controllerDescriptor.GetCustomAttributes<IHttpRouteInfoProvider>(inherit: false);

            List<IDirectRouteFactory> combined = new List<IDirectRouteFactory>();
            combined.AddRange(newFactories);

            foreach (IHttpRouteInfoProvider oldProvider in oldProviders)
            {
                if (oldProvider is IDirectRouteFactory)
                {
                    continue;
                }

                combined.Add(new RouteInfoDirectRouteFactory(oldProvider));
            }

            return combined;
        }

        protected virtual IReadOnlyCollection<IDirectRouteFactory> GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            Collection<IDirectRouteFactory> newFactories = actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: false);

            Collection<IHttpRouteInfoProvider> oldProviders = actionDescriptor.GetCustomAttributes<IHttpRouteInfoProvider>(inherit: false);

            List<IDirectRouteFactory> combined = new List<IDirectRouteFactory>();
            combined.AddRange(newFactories);

            foreach (IHttpRouteInfoProvider oldProvider in oldProviders)
            {
                if (oldProvider is IDirectRouteFactory)
                {
                    continue;
                }

                combined.Add(new RouteInfoDirectRouteFactory(oldProvider));
            }

            return combined;
        }

        protected virtual IReadOnlyCollection<RouteEntry> GetControllerDirectRoutes(
            HttpControllerDescriptor controllerDescriptor,
            IReadOnlyCollection<HttpActionDescriptor> actionDescriptors,
            IReadOnlyCollection<IDirectRouteFactory> factories,
            IInlineConstraintResolver constraintResolver)
        {
            return CreateRouteEntries(
                GetRoutePrefix(controllerDescriptor), 
                factories, 
                actionDescriptors, 
                constraintResolver, 
                targetIsAction: false);
        }

        public IReadOnlyCollection<RouteEntry> GetActionDirectRoutes(
            HttpActionDescriptor actionDescriptor, 
            IReadOnlyCollection<IDirectRouteFactory> factories,
            IInlineConstraintResolver constraintResolver)
        {
            return CreateRouteEntries(
                GetRoutePrefix(actionDescriptor.ControllerDescriptor), 
                factories, 
                new HttpActionDescriptor[] { actionDescriptor }, 
                constraintResolver, 
                targetIsAction: true);
        }

        protected virtual string GetRoutePrefix(HttpControllerDescriptor controllerDescriptor)
        {
            Collection<IRoutePrefix> attributes = controllerDescriptor.GetCustomAttributes<IRoutePrefix>(inherit: false);

            if (attributes == null)
            {
                return null;
            }

            if (attributes.Count > 1)
            {
                string errorMessage = Error.Format(SRResources.RoutePrefix_CannotSupportMultiRoutePrefix, controllerDescriptor.ControllerType.FullName);
                throw new InvalidOperationException(errorMessage);
            }

            if (attributes.Count == 1)
            {
                IRoutePrefix attribute = attributes[0];

                if (attribute != null)
                {
                    string prefix = attribute.Prefix;
                    if (prefix == null)
                    {
                        string errorMessage = Error.Format(
                            SRResources.RoutePrefix_PrefixCannotBeNull,
                            controllerDescriptor.ControllerType.FullName);
                        throw new InvalidOperationException(errorMessage);
                    }

                    if (prefix.EndsWith("/", StringComparison.Ordinal))
                    {
                        throw Error.InvalidOperation(SRResources.AttributeRoutes_InvalidPrefix, prefix,
                            controllerDescriptor.ControllerName);
                    }

                    return prefix;
                }
            }

            return null;
        }

        private static IReadOnlyCollection<RouteEntry> CreateRouteEntries(
            string prefix,
            IReadOnlyCollection<IDirectRouteFactory> factories,
            IReadOnlyCollection<HttpActionDescriptor> actions, 
            IInlineConstraintResolver constraintResolver, 
            bool targetIsAction)
        {
            List<RouteEntry> entries = new List<RouteEntry>();
            foreach (IDirectRouteFactory factory in factories)
            {
                RouteEntry entry = CreateRouteEntry(prefix, factory, actions, constraintResolver, targetIsAction);
                entries.Add(entry);
            }

            return entries;
        }

        private static RouteEntry CreateRouteEntry(
            string prefix,
            IDirectRouteFactory factory,
            IReadOnlyCollection<HttpActionDescriptor> actions,
            IInlineConstraintResolver constraintResolver,
            bool targetIsAction)
        {
            Contract.Assert(factory != null);

            DirectRouteFactoryContext context = new DirectRouteFactoryContext(prefix, actions, constraintResolver, targetIsAction);
            RouteEntry entry = factory.CreateRoute(context);

            if (entry == null)
            {
                throw Error.InvalidOperation(SRResources.TypeMethodMustNotReturnNull,
                    typeof(IDirectRouteFactory).Name, "CreateRoute");
            }

            IHttpRoute route = entry.Route;
            Contract.Assert(route != null);

            HttpActionDescriptor[] targetActions = route.GetTargetActionDescriptors();

            if (targetActions == null || targetActions.Length == 0)
            {
                throw new InvalidOperationException(SRResources.DirectRoute_MissingActionDescriptors);
            }

            if (route.Handler != null)
            {
                throw new InvalidOperationException(SRResources.DirectRoute_HandlerNotSupported);
            }

            return entry;
        }
    }
}
