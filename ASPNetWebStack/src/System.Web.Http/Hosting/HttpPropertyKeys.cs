﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Web.Http.Routing;

namespace System.Web.Http.Hosting
{
    /// <summary>
    /// Provides common keys for properties stored in the <see cref="HttpRequestMessage.Properties"/>
    /// </summary>
    public static class HttpPropertyKeys
    {
        /// <summary>
        /// Provides a key for the <see cref="HttpConfiguration"/> associated with this request.
        /// </summary>
        public static readonly string HttpConfigurationKey = "MS_HttpConfiguration";

        /// <summary>
        /// Provides a key for the <see cref="IHttpRouteData"/> associated with this request.
        /// </summary>
        public static readonly string HttpRouteDataKey = "MS_HttpRouteData";

        /// <summary>
        /// Provides a key for the <see cref="UrlHelper"/> instance associated with this request.
        /// </summary>
        public static readonly string UrlHelperKey = "MS_UrlHelper";

        /// <summary>
        /// Provides a key for the <see cref="System.Web.Http.Controllers.HttpActionDescriptor"/> associated with this request.
        /// </summary>
        public static readonly string HttpActionDescriptorKey = "MS_HttpActionDescriptor";

        /// <summary>
        /// Provides a key for the current <see cref="SynchronizationContext"/> stored in <see cref="HttpRequestMessage.Properties"/>.
        /// If <see cref="SynchronizationContext.Current"/> is <c>null</c> then no context is stored.
        /// </summary>
        public static readonly string SynchronizationContextKey = "MS_SynchronizationContext";

        /// <summary>
        /// Provides a key for the collection of resources that should be disposed when a request is disposed.
        /// </summary>
        public static readonly string DisposableRequestResourcesKey = "MS_DisposableRequestResources";

        /// <summary>
        /// Provides a key for the dependency scope for this request.
        /// </summary>
        public static readonly string DependencyScope = "MS_DependencyScope";

        /// <summary>
        /// Provides a key for the client certificate for this request.
        /// </summary>
        public static readonly string ClientCertificateKey = "MS_ClientCertificate";

        /// <summary>
        /// Provides a key for a delegate which can retrieve the client certificate for this request.
        /// </summary>
        public static readonly string RetrieveClientCertificateDelegateKey = "MS_RetrieveClientCertificateDelegate";

        /// <summary>
        /// Provides a key for the <see cref="Guid"/> stored in <see cref="HttpRequestMessage.Properties"/>.
        /// This is the correlation id for that request.
        /// </summary>
        public static readonly string RequestCorrelationKey = "MS_RequestId";

        /// <summary>
        /// Provides a key that indicates whether the request originates from a local address.
        /// </summary>
        public static readonly string IsLocalKey = "MS_IsLocal";

        /// <summary>
        /// Provides a key that indicates whether the request failed to match a route.
        /// </summary>
        public static readonly string NoRouteMatched = "MS_NoRouteMatched";

        /// <summary>
        /// Provides a key for the root virtual path for a request.
        /// </summary>
        public static readonly string VirtualPathRoot = "MS_VirtualPathRoot";

        /// <summary>
        /// Provides a key that indicates whether error details are to be included in the response for this HTTP request.
        /// </summary>
        public static readonly string IncludeErrorDetailKey = "MS_IncludeErrorDetail";

        /// <summary>
        /// Provides a key for the parsed query string stored in <see cref="HttpRequestMessage.Properties"/>.
        /// </summary>
        public static readonly string RequestQueryNameValuePairsKey = "MS_QueryNameValuePairs";

        /// <summary>
        /// Provides a key that indicates whether a custom route threw a <see cref="HttpResponseException"/>. The value corresponds to the
        /// response message from <see cref="HttpResponseException.Response"/>.
        /// </summary>
        public static readonly string RoutingErrorResponseKey = "MS_RoutingError";

        /// <summary>
        /// Provides a key that indicates whether the request is a batch request.
        /// </summary>
        public static readonly string IsBatchRequest = "MS_BatchRequest";
    }
}