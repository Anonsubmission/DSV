﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Cors;
using System.Web.Http.Hosting;
using Microsoft.TestCommon;

namespace System.Web.Http.Cors
{
    public class CorsMessageHandlerTest
    {
        [Fact]
        public void Constructor_NullConfig_Throws()
        {
            Assert.ThrowsArgumentNull(() => new CorsMessageHandler(null), "httpConfiguration");
        }

        [Fact]
        public void SendAsync_DoesNotAddHeaders_WhenOriginIsMissing()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            HttpServer server = new HttpServer(config);
            CorsMessageHandler corsHandler = new CorsMessageHandler(config);
            corsHandler.InnerHandler = server;
            HttpMessageInvoker invoker = new HttpMessageInvoker(corsHandler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/sample");

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;

            Assert.Empty(response.Headers);
        }

        [Theory]
        [InlineData("GET", "*")]
        [InlineData("DELETE", "*")]
        [InlineData("HEAD", "http://example.com")]
        public void SendAsync_ReturnsAllowAOrigin(string httpMethod, string expectedOrigin)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            HttpServer server = new HttpServer(config);
            CorsMessageHandler corsHandler = new CorsMessageHandler(config);
            corsHandler.InnerHandler = server;
            HttpMessageInvoker invoker = new HttpMessageInvoker(corsHandler);
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/sample");
            request.Headers.Add(CorsConstants.Origin, "http://example.com");

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;
            string origin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();

            Assert.Equal(expectedOrigin, origin);
        }

        [Theory]
        [InlineData("DELETE", "*", "foo,bar")]
        [InlineData("PUT", "http://localhost", "content-type,custom")]
        public void SendAsync_Preflight_ReturnsAllowMethodsAndAllowHeaders(string requestedMethod, string expectedOrigin, string requestedHeaders)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            HttpServer server = new HttpServer(config);
            CorsMessageHandler corsHandler = new CorsMessageHandler(config);
            corsHandler.InnerHandler = server;
            HttpMessageInvoker invoker = new HttpMessageInvoker(corsHandler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            request.Headers.Add(CorsConstants.Origin, "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, requestedMethod);
            request.Headers.Add(CorsConstants.AccessControlRequestHeaders, requestedHeaders);

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;
            string origin = response.Headers.GetValues(CorsConstants.AccessControlAllowOrigin).FirstOrDefault();
            string allowMethod = response.Headers.GetValues(CorsConstants.AccessControlAllowMethods).FirstOrDefault();
            string[] allowHeaders = response.Headers.GetValues(CorsConstants.AccessControlAllowHeaders).FirstOrDefault().Split(',');
            string[] requestedHeaderArray = requestedHeaders.Split(',');

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedOrigin, origin);
            Assert.Equal(requestedMethod, allowMethod);
            foreach (string requestedHeader in requestedHeaderArray)
            {
                Assert.Contains(requestedHeader, allowHeaders);
            }
        }

        [Fact]
        public void SendAsync_Preflight_ReturnsOriginalResponse_WhenNoCorsPolicyProviderIsFound()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            config.MessageHandlers.Add(new CorsMessageHandler(config));
            HttpServer server = new HttpServer(config);
            HttpMessageInvoker invoker = new HttpMessageInvoker(server);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/default");
            request.Headers.Add(CorsConstants.Origin, "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public void SendAsync_Preflight_ReturnsBadRequest_WhenOriginIsNotAllowed()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            config.MessageHandlers.Add(new CorsMessageHandler(config));
            HttpServer server = new HttpServer(config);
            HttpMessageInvoker invoker = new HttpMessageInvoker(server);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/default");
            request.Headers.Add(CorsConstants.Origin, "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "POST");

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("{\"Message\":\"The origin 'http://localhost' is not allowed.\"}", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void SendAsync_HandlesExceptions_ThrownDuringPreflight()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            HttpServer server = new HttpServer(config);
            CorsMessageHandler corsHandler = new CorsMessageHandler(config);
            corsHandler.InnerHandler = server;
            HttpMessageInvoker invoker = new HttpMessageInvoker(corsHandler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            request.Headers.Add(CorsConstants.Origin, "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "RandomMethod");

            HttpResponseMessage response = invoker.SendAsync(request, CancellationToken.None).Result;

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public void HandleCorsRequestAsync_NullConfig_Throws()
        {
            CorsMessageHandler corsHandler = new CorsMessageHandler(new HttpConfiguration());
            Assert.ThrowsArgumentNull(() =>
            {
                HttpResponseMessage response = corsHandler.HandleCorsRequestAsync(null, new CorsRequestContext(), CancellationToken.None).Result;
            },
            "request");
        }

        [Fact]
        public void HandleCorsRequestAsync_NullContext_Throws()
        {
            CorsMessageHandler corsHandler = new CorsMessageHandler(new HttpConfiguration());
            Assert.ThrowsArgumentNull(() =>
            {
                HttpResponseMessage response = corsHandler.HandleCorsRequestAsync(new HttpRequestMessage(), null, CancellationToken.None).Result;
            },
            "corsRequestContext");
        }

        [Fact]
        public void HandleCorsPreflightRequestAsync_ReturnsBadRequestWhenAccessControlRequestMethodIsInvalid()
        {
            CorsMessageHandler corsHandler = new CorsMessageHandler(new HttpConfiguration());
            HttpResponseMessage response = corsHandler.HandleCorsPreflightRequestAsync(new HttpRequestMessage(),
                new CorsRequestContext
                {
                    AccessControlRequestMethod = "Get-http://localhost"
                },
                CancellationToken.None).Result;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The \"Access-Control-Request-Method\" header value 'Get-http://localhost' is invalid.", response.Content.ReadAsAsync<HttpError>().Result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void HandleCorsPreflightRequestAsync_ReturnsBadRequestWhenAccessControlRequestMethodIsNullOrEmpty(string accessControlRequestMethod)
        {
            CorsMessageHandler corsHandler = new CorsMessageHandler(new HttpConfiguration());
            HttpResponseMessage response = corsHandler.HandleCorsPreflightRequestAsync(new HttpRequestMessage(),
                new CorsRequestContext
                {
                    AccessControlRequestMethod = accessControlRequestMethod
                },
                CancellationToken.None).Result;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The \"Access-Control-Request-Method\" header value cannot be null or empty.", response.Content.ReadAsAsync<HttpError>().Result.Message);
        }

        [Fact]
        public void HandleCorsPreflightRequestAsync_NullContext_Throws()
        {
            CorsMessageHandler corsHandler = new CorsMessageHandler(new HttpConfiguration());
            Assert.ThrowsArgumentNull(() =>
            {
                HttpResponseMessage response = corsHandler.HandleCorsPreflightRequestAsync(new HttpRequestMessage(), null, CancellationToken.None).Result;
            },
            "corsRequestContext");
        }
    }
}