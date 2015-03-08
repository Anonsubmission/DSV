﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.TestCommon;

namespace System.Web.Http.Dispatcher
{
    public class CustomControllerTypeResolverTest : HttpServerTestBase
    {
        internal static readonly string ExpectedContent = "Hello World!";

        public CustomControllerTypeResolverTest()
            : base("http://localhost/")
        {
        }

        protected override void ApplyConfiguration(HttpConfiguration configuration)
        {
            // Add default route
            configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Set our own assembly resolver where we add the assemblies we need           
            CustomControllerTypeResolver customHttpControllerTypeResolver = new CustomControllerTypeResolver();
            configuration.Services.Replace(typeof(IHttpControllerTypeResolver), customHttpControllerTypeResolver);
        }

        [Fact]
        public void CustomControllerTypeResolver_ReplacesControllerTypeAndNameConvention()
        {
            // Arrange
            string address = BaseAddress + "api/custominternal";

            // Act
            HttpResponseMessage response = Client.GetAsync(address).Result;
            string expectedContent = response.Content.ReadAsStringAsync().Result;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ExpectedContent, expectedContent);
        }
    }

    internal class CustomControllerTypeResolver : IHttpControllerTypeResolver
    {
        public ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return new List<Type> { typeof(CustomInternalController) };
        }
    }

    /// <summary>
    /// Test controller which is declared internal so wouldn't not get picked up by
    /// <see cref="DefaultHttpControllerTypeResolver"/>.
    /// </summary>
    internal class CustomInternalController : ApiController
    {
        public HttpResponseMessage Get()
        {
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = new StringContent(CustomControllerTypeResolverTest.ExpectedContent);
            return response;
        }
    }
}
