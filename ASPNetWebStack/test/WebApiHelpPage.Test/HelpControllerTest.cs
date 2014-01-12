﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.Mvc;
using Microsoft.TestCommon;
using ROOT_PROJECT_NAMESPACE.Areas.HelpPage;
using ROOT_PROJECT_NAMESPACE.Areas.HelpPage.Controllers;

namespace WebApiHelpPageWebHost.UnitTest
{
    public class HelpControllerTest
    {
        [Fact]
        public void Constructor_Default()
        {
            HelpController controller = new HelpController();
            Assert.Same(GlobalConfiguration.Configuration, controller.Configuration);
        }

        [Fact]
        public void Constructor_OneParameter()
        {
            HttpConfiguration config = new HttpConfiguration();
            HelpController controller = new HelpController(config);
            Assert.Same(config, controller.Configuration);
        }

        [Fact]
        public void Index_ReturnsCachedModels()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { id = RouteParameter.Optional });
            HelpController controller = new HelpController(config);

            ViewResult result = Assert.IsType<ViewResult>(controller.Index());
            ViewResult result2 = Assert.IsType<ViewResult>(controller.Index());

            Assert.NotNull(result.Model);
            Assert.NotNull(result2.Model);

            // Make sure the model is cached
            Assert.Same(config.Services.GetApiExplorer().ApiDescriptions, result.Model);
            Assert.Same(result.Model, result2.Model);
        }

        [Theory]
        [InlineData("Get-Values")]
        [InlineData("get-values")]
        [InlineData("Get-Values_name")]
        [InlineData("get-values_NAME")]
        [InlineData("Get-Values-id")]
        [InlineData("Get-Values-ID")]
        [InlineData("Post-Values")]
        [InlineData("POST-VALUES")]
        [InlineData("Put-Values-id")]
        [InlineData("Put-VALUES-ID")]
        [InlineData("Put-Values")]
        [InlineData("Put-VALUES")]
        [InlineData("Delete-Values-id")]
        [InlineData("Delete-VALUES-id")]
        [InlineData("Patch-Values")]
        [InlineData("Patch-VALUES")]
        [InlineData("Options-Values")]
        [InlineData("OpTions-VALUES")]
        [InlineData("Head-Values-id")]
        [InlineData("HEAD-VALUES-id")]
        public void API_ReturnsCachedModels(string apiDescriptionId)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { id = RouteParameter.Optional });
            HelpController controller = new HelpController(config);

            ViewResult result = Assert.IsType<ViewResult>(controller.Api(apiDescriptionId));
            ViewResult result2 = Assert.IsType<ViewResult>(controller.Api(apiDescriptionId));

            Assert.NotNull(result.Model);
            Assert.NotNull(result2.Model);

            // Make sure the model is cached
            Assert.Same(config.GetHelpPageApiModel(apiDescriptionId), result.Model);
            Assert.Same(result.Model, result2.Model);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("@@@@@@@")]
        public void API_ReturnsNullModels_WhenApiIdIsInvalid(string apiDescriptionId)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { id = RouteParameter.Optional });
            HelpController controller = new HelpController(config);

            ViewResult result = Assert.IsType<ViewResult>(controller.Api(apiDescriptionId));
            ViewResult result2 = Assert.IsType<ViewResult>(controller.Api(apiDescriptionId));

            Assert.Null(result.Model);
            Assert.Null(result2.Model);
        }
    }
}
