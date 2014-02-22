// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
#if ASPNETWEBAPI
using TActionDescriptor = System.Web.Http.Controllers.HttpActionDescriptor;
using TControllerDescriptor = System.Web.Http.Controllers.HttpControllerDescriptor;
#else
using TActionDescriptor = System.Web.Mvc.ControllerDescriptor;
using TControllerDescriptor = System.Web.Mvc.ActionDescriptor;
#endif

#if ASPNETWEBAPI
namespace System.Web.Http.Routing
#else
namespace System.Web.Mvc.Routing
#endif
{
    /// <summary>
    /// Defines a provider for routes that directly target action descriptors (attribute routes).
    /// </summary>
    public interface IDirectRouteProvider
    {
#if ASPNETWEBAPI
        /// <summary>Gets the direct routes for a controller.</summary>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        /// <param name="actionDescriptors">The action descriptors.</param>
        /// <param name="constraintResolver">The inline constraint resolver.</param>
        /// <returns>A set of route entries for the controller.</returns>
        IReadOnlyCollection<RouteEntry> GetDirectRoutes(
            TControllerDescriptor controllerDescriptor, 
            IReadOnlyCollection<TActionDescriptor> actionDescriptors,
            IInlineConstraintResolver constraintResolver);
#else
        /// <summary>Gets the direct routes for a controller.</summary>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        /// <param name="actionDescriptors">The action descriptors.</param>
        /// <returns>A set of route entries for the controller.</returns>
        IReadOnlyCollection<RouteEntry> GetDirectRoutes(TControllerDescriptor controllerDescriptor, IReadOnlyCollection<TActionDescriptor> actionDescriptors);
#endif
    }
}
