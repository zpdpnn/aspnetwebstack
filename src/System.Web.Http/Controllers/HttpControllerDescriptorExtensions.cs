// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Web.Http.Routing;

namespace System.Web.Http.Controllers
{
    internal static class HttpControllerDescriptorExtensions
    {
        private const string AttributeRoutedPropertyKey = "MS_IsAttributeRouted";

        public static bool IsAttributeRouted(this HttpControllerDescriptor controllerDescriptor)
        {
            if (controllerDescriptor == null)
            {
                throw new ArgumentNullException("controllerDescriptor");
            }

            object value;
            controllerDescriptor.Properties.TryGetValue(AttributeRoutedPropertyKey, out value);

            // We fall back to the attributes here so that we continue to do the right thing when 
            // MapAttributeRoutes isn't called.
            return 
                value as bool? ?? false ||
                controllerDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: false).Any() || 
                controllerDescriptor.GetCustomAttributes<IHttpRouteInfoProvider>(inherit: false).Any();
        }

        public static void SetIsAttributeRouted(this HttpControllerDescriptor controllerDescriptor, bool value)
        {
            if (controllerDescriptor == null)
            {
                throw new ArgumentNullException("controllerDescriptor");
            }

            controllerDescriptor.Properties[AttributeRoutedPropertyKey] = value;
        }
    }
}
