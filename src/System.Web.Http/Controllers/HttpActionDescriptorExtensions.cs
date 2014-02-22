// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Web.Http.Routing;

namespace System.Web.Http.Controllers
{
    internal static class HttpActionDescriptorExtensions
    {
        private const string AttributeRoutedPropertyKey = "MS_IsAttributeRouted";

        public static bool IsAttributeRouted(this HttpActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException("actionDescriptor");
            }

            object value;
            actionDescriptor.Properties.TryGetValue(AttributeRoutedPropertyKey, out value);

            // We fall back to the attributes here so that we continue to do the right thing when 
            // MapAttributeRoutes isn't called.
            return
                value as bool? ?? false ||
                actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: false).Any() ||
                actionDescriptor.GetCustomAttributes<IHttpRouteInfoProvider>(inherit: false).Any();
        }

        public static void SetIsAttributeRouted(this HttpActionDescriptor actionDescriptor, bool value)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException("actionDescriptor");
            }

            actionDescriptor.Properties[AttributeRoutedPropertyKey] = value;
        }
    }
}
