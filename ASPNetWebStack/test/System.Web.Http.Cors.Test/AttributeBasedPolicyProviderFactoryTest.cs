﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Web.Cors;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Cors
{
    public class AttributeBasedPolicyProviderFactoryTest
    {
        [Fact]
        public void GetCorsPolicyProvider_NullRequest_Throws()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            Assert.ThrowsArgumentNull(() =>
                providerFactory.GetCorsPolicyProvider(null),
                "request");
        }

        [Theory]
        [InlineData("DELETE", "", typeof(EnableCorsAttribute))]
        [InlineData("Post", "", typeof(DisableCorsAttribute))]
        [InlineData("get", "", typeof(EnableCorsAttribute))]
        [InlineData("GET", "/3", typeof(DisableCorsAttribute))]
        public void GetCorsPolicyProvider_Preflight_ReturnsExpectedPolicyProvider(string httpMethod, string path, Type expectedProviderType)
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample" + path);
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, httpMethod);
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            IHttpRoute route = config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route.GetRouteData("/", request);

            ICorsPolicyProvider provider = providerFactory.GetCorsPolicyProvider(request);

            Assert.True(request.GetCorsRequestContext().IsPreflight);
            Assert.IsType(expectedProviderType, provider);
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_NoHttpConfiguration_Throws()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();

            // No HttpConfiguration set on the request.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
            HttpConfiguration config = new HttpConfiguration();
            IHttpRoute route = config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route.GetRouteData("/", request);

            Assert.Throws<InvalidOperationException>(() =>
                providerFactory.GetCorsPolicyProvider(request),
                "The request does not have an associated configuration object.");
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_NoRouteData_Throws()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();

            // No RouteData set on the request.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

            Assert.Throws<InvalidOperationException>(() =>
                providerFactory.GetCorsPolicyProvider(request),
                "No route data was found for this request.");
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_ReturnsDefaultPolicyProvider_WhenActionSelectionFails()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            ICorsPolicyProvider mockProvider = new Mock<ICorsPolicyProvider>().Object;
            providerFactory.DefaultPolicyProvider = mockProvider;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "RandomMethod");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            IHttpRoute route = config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route.GetRouteData("/", request);

            ICorsPolicyProvider provider = providerFactory.GetCorsPolicyProvider(request);

            Assert.True(request.GetCorsRequestContext().IsPreflight);
            Assert.Same(mockProvider, provider);
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_Throws_WhenNoDefaultPolicyProviderAndActionSelectionFails()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "RandomMethod");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            IHttpRoute route = config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route.GetRouteData("/", request);

            Assert.True(request.GetCorsRequestContext().IsPreflight);
            Assert.Throws<HttpResponseException>(() =>
                providerFactory.GetCorsPolicyProvider(request));
        }

        [Fact]
        public void GetCorsPolicyProvider_ReturnsDefaultPolicyProvider()
        {
            ICorsPolicyProvider mockProvider = new Mock<ICorsPolicyProvider>().Object;
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            providerFactory.DefaultPolicyProvider = mockProvider;
            HttpRequestMessage request = new HttpRequestMessage();
            Func<string> action = new DefaultController().Get;
            request.Properties[HttpPropertyKeys.HttpActionDescriptorKey] = new ReflectedHttpActionDescriptor
            {
                MethodInfo = action.Method
            };
            request.Headers.Add("Origin", "http://example.com");

            ICorsPolicyProvider policyProvider = providerFactory.GetCorsPolicyProvider(request);

            Assert.Same(mockProvider, policyProvider);
        }

        [Fact]
        public void GetCorsPolicyProvider_ReturnsPolicyProvider_OnController()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage();
            Func<string> action = new SampleController().Get;
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor
            {
                ControllerName = "Sample",
                ControllerType = typeof(SampleController)
            };
            request.Properties[HttpPropertyKeys.HttpActionDescriptorKey] = new ReflectedHttpActionDescriptor
            {
                MethodInfo = action.Method,
                ControllerDescriptor = controllerDescriptor
            };
            request.Headers.Add("Origin", "http://example.com");

            ICorsPolicyProvider policyProvider = providerFactory.GetCorsPolicyProvider(request);

            Assert.NotNull(policyProvider);
            Assert.IsType(typeof(EnableCorsAttribute), policyProvider);
        }

        [Fact]
        public void GetCorsPolicyProvider_ReturnsPolicyProvider_OnAction()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage();
            Func<string> action = new SampleController().Post;
            request.Properties[HttpPropertyKeys.HttpActionDescriptorKey] = new ReflectedHttpActionDescriptor
            {
                MethodInfo = action.Method
            };
            request.Headers.Add("Origin", "http://example.com");

            ICorsPolicyProvider policyProvider = providerFactory.GetCorsPolicyProvider(request);

            Assert.NotNull(policyProvider);
            Assert.IsType(typeof(DisableCorsAttribute), policyProvider);
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_ReturnsPolicyProviderUsingPerControllerConfiguration()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/percontrollerconfig");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "httpmethod");
            HttpConfiguration config = new HttpConfiguration();
            request.SetConfiguration(config);
            config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });

            ICorsPolicyProvider provider = providerFactory.GetCorsPolicyProvider(request);

            Assert.True(request.GetCorsRequestContext().IsPreflight);
            EnableCorsAttribute enableCorsAttribute = Assert.IsType<EnableCorsAttribute>(provider);
            Assert.Equal(1, enableCorsAttribute.Origins.Count());
            Assert.Equal("http://example.com", enableCorsAttribute.Origins.First());
        }

        [Fact]
        public void GetCorsPolicyProvider_Preflight_DoesNotUseRouteDataOnTheRequest()
        {
            AttributeBasedPolicyProviderFactory providerFactory = new AttributeBasedPolicyProviderFactory();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/sample");
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "Put");
            HttpConfiguration config = new HttpConfiguration();
            request.SetConfiguration(config);
            var route = config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });
            request.SetRouteData(new HttpRouteData(route, new HttpRouteValueDictionary(new { action = "Options", controller = "sample", id = 2 })));

            ICorsPolicyProvider provider = providerFactory.GetCorsPolicyProvider(request);

            Assert.True(request.GetCorsRequestContext().IsPreflight);
            EnableCorsAttribute enableCorsAttribute = Assert.IsType<EnableCorsAttribute>(provider);
            Assert.Equal(2, enableCorsAttribute.Origins.Count());
            Assert.Equal("http://example.com", enableCorsAttribute.Origins[0]);
            Assert.Equal("http://localhost", enableCorsAttribute.Origins[1]);
        }
    }
}