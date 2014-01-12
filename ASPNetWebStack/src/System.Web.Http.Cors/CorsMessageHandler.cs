﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors.Properties;

namespace System.Web.Http.Cors
{
    /// <summary>
    /// Custom <see cref="DelegatingHandler"/> for handling CORS requests.
    /// </summary>
    public class CorsMessageHandler : DelegatingHandler
    {
        private HttpConfiguration _httpConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsMessageHandler"/> class.
        /// </summary>
        /// <param name="httpConfiguration">The <see cref="HttpConfiguration"/>.</param>
        /// <exception cref="System.ArgumentNullException">httpConfiguration</exception>
        public CorsMessageHandler(HttpConfiguration httpConfiguration)
        {
            if (httpConfiguration == null)
            {
                throw new ArgumentNullException("httpConfiguration");
            }

            _httpConfiguration = httpConfiguration;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1" />. The task object representing the asynchronous operation.
        /// </returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CorsRequestContext corsRequestContext = request.GetCorsRequestContext();
            if (corsRequestContext != null)
            {
                try
                {
                    if (corsRequestContext.IsPreflight)
                    {
                        return await HandleCorsPreflightRequestAsync(request, corsRequestContext, cancellationToken);
                    }
                    else
                    {
                        return await HandleCorsRequestAsync(request, corsRequestContext, cancellationToken);
                    }
                }
                catch (Exception exception)
                {
                    return HandleException(request, exception);
                }
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// Handles the actual CORS request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="corsRequestContext">The <see cref="CorsRequestContext"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// request
        /// or
        /// corsRequestContext
        /// </exception>
        public virtual async Task<HttpResponseMessage> HandleCorsRequestAsync(HttpRequestMessage request, CorsRequestContext corsRequestContext, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (corsRequestContext == null)
            {
                throw new ArgumentNullException("corsRequestContext");
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            CorsPolicy corsPolicy = await GetCorsPolicyAsync(request, cancellationToken);
            if (corsPolicy != null)
            {
                CorsResult result;
                if (TryEvaluateCorsPolicy(corsRequestContext, corsPolicy, out result))
                {
                    if (response != null)
                    {
                        response.WriteCorsHeaders(result);
                    }
                }
            }
            return response;
        }

        /// <summary>
        /// Handles the preflight request specified by CORS.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="corsRequestContext">The cors request context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/></returns>
        /// <exception cref="System.ArgumentNullException">
        /// request
        /// or
        /// corsRequestContext
        /// </exception>
        public virtual async Task<HttpResponseMessage> HandleCorsPreflightRequestAsync(HttpRequestMessage request, CorsRequestContext corsRequestContext, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (corsRequestContext == null)
            {
                throw new ArgumentNullException("corsRequestContext");
            }

            try
            {
                // Make sure Access-Control-Request-Method is valid.
                new HttpMethod(corsRequestContext.AccessControlRequestMethod);
            }
            catch (ArgumentException)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        SRResources.AccessControlRequestMethodCannotBeNullOrEmpty);
            }
            catch (FormatException)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    String.Format(CultureInfo.CurrentCulture,
                        SRResources.InvalidAccessControlRequestMethod,
                        corsRequestContext.AccessControlRequestMethod));
            }

            CorsPolicy corsPolicy = await GetCorsPolicyAsync(request, cancellationToken);
            if (corsPolicy != null)
            {
                HttpResponseMessage response = null;
                CorsResult result;
                if (TryEvaluateCorsPolicy(corsRequestContext, corsPolicy, out result))
                {
                    response = request.CreateResponse(HttpStatusCode.OK);
                    response.WriteCorsHeaders(result);
                }
                else
                {
                    response = result != null ?
                        request.CreateErrorResponse(HttpStatusCode.BadRequest, String.Join(" | ", result.ErrorMessages)) :
                        request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return response;
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller owns HttpRequestMessage instance.")]
        private static HttpResponseMessage HandleException(HttpRequestMessage request, Exception exception)
        {
            HttpResponseException httpResponseException = exception as HttpResponseException;

            if (httpResponseException != null)
            {
                return httpResponseException.Response;
            }

            return request.CreateErrorResponse(HttpStatusCode.InternalServerError, exception);
        }

        private async Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CorsPolicy corsPolicy = null;
            ICorsPolicyProviderFactory corsPolicyProviderFactory = _httpConfiguration.GetCorsPolicyProviderFactory();
            ICorsPolicyProvider corsPolicyProvider = corsPolicyProviderFactory.GetCorsPolicyProvider(request);
            if (corsPolicyProvider != null)
            {
                corsPolicy = await corsPolicyProvider.GetCorsPolicyAsync(request, cancellationToken);
            }
            return corsPolicy;
        }

        private bool TryEvaluateCorsPolicy(CorsRequestContext requestContext, CorsPolicy corsPolicy, out CorsResult corsResult)
        {
            ICorsEngine engine = _httpConfiguration.GetCorsEngine();
            corsResult = engine.EvaluatePolicy(requestContext, corsPolicy);
            return corsResult != null && corsResult.IsValid;
        }
    }
}