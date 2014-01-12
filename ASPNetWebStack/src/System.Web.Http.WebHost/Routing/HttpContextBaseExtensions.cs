﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Net.Http;

namespace System.Web.Http.WebHost.Routing
{
    internal static class HttpContextBaseExtensions
    {
        internal static readonly string HttpRequestMessageKey = "MS_HttpRequestMessage";

        public static HttpRequestMessage GetHttpRequestMessage(this HttpContextBase context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (context.Items == null || !context.Items.Contains(HttpRequestMessageKey))
            {
                return null;
            }

            return context.Items[HttpRequestMessageKey] as HttpRequestMessage;
        }

        public static void SetHttpRequestMessage(this HttpContextBase context, HttpRequestMessage request)
        {
            if (context.Items != null)
            {
                context.Items.Add(HttpRequestMessageKey, request);
            }
        }

        public static HttpRequestMessage GetOrCreateHttpRequestMessage(this HttpContextBase context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            HttpRequestMessage request = context.GetHttpRequestMessage();
            if (request == null)
            {
                request = HttpControllerHandler.ConvertRequest(context);
                context.SetHttpRequestMessage(request);
            }

            return request;
        }

        public static void SetRoutingError(this HttpContextBase context, HttpResponseMessage errorResponse)
        {
            Contract.Assert(context != null);
            Contract.Assert(errorResponse != null);

            HttpRequestMessage request = context.GetOrCreateHttpRequestMessage();
            request.SetRoutingErrorResponse(errorResponse);
        }

        public static HttpResponseMessage GetRoutingError(this HttpContextBase context)
        {
            Contract.Assert(context != null);

            HttpRequestMessage request = context.GetOrCreateHttpRequestMessage();
            return request.GetRoutingErrorResponse();
        }
    }
}
