﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Web.Mvc;
using Facebook;
using Microsoft.AspNet.Mvc.Facebook.Providers;
using Moq;

namespace Microsoft.AspNet.Mvc.Facebook.Test.Helpers
{
    internal class MockHelpers
    {
        public static ControllerContext CreateControllerContext(NameValueCollection requestFormData = null, NameValueCollection requestQueryData = null, Uri requestUrl = null)
        {
            Mock<ControllerContext> controllerContext = new Mock<ControllerContext>();
            controllerContext.Setup(c => c.HttpContext.Items).Returns(new Dictionary<object, object>());
            controllerContext.Setup(c => c.HttpContext.Request.Url).Returns(requestUrl ?? new Uri("http://example.com"));
            controllerContext.Setup(c => c.HttpContext.Request.AppRelativeCurrentExecutionFilePath).Returns("~/");
            controllerContext.Setup(c => c.HttpContext.Request.Form).Returns(requestFormData ?? new NameValueCollection());
            controllerContext.Setup(c => c.HttpContext.Request.QueryString).Returns(requestQueryData ?? new NameValueCollection());
            return controllerContext.Object;
        }

        public static ActionDescriptor CreateActionDescriptor(object[] actionAuthorizeAttributes = null, object[] controllerAuthorizeAttributes = null)
        {
            Mock<ActionDescriptor> actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(a => a.GetCustomAttributes(typeof(FacebookAuthorizeAttribute), true)).Returns(actionAuthorizeAttributes ?? new object[0]);
            actionDescriptor.Setup(a => a.ControllerDescriptor.GetCustomAttributes(typeof(FacebookAuthorizeAttribute), true)).Returns(controllerAuthorizeAttributes ?? new object[0]);
            return actionDescriptor.Object;
        }

        public static FacebookConfiguration CreateConfiguration(FacebookClient client = null, IFacebookPermissionService permissionService = null)
        {
            FacebookConfiguration config = new FacebookConfiguration();

            if (client == null)
            {
                config.ClientProvider = new DefaultFacebookClientProvider(config);
                config.AppId = "DefaultAppId";
                config.AppSecret = "DefaultAppSecret";
            }
            else
            {
                Mock<IFacebookClientProvider> clientProvider = new Mock<IFacebookClientProvider>();
                clientProvider.Setup(cp => cp.CreateClient()).Returns(client);
                config.ClientProvider = clientProvider.Object;
                config.AppId = client.AppId ?? "DefaultAppId";
                config.AppSecret = client.AppSecret ?? "DefaultAppSecret";
            }
            config.PermissionService = permissionService ?? new DefaultFacebookPermissionService(config);
            return config;
        }

        public static FacebookClient CreateFacebookClient()
        {
            Mock<FacebookClient> client = new Mock<FacebookClient>();
            dynamic signedRequestParameters = new ExpandoObject();
            signedRequestParameters.user_id = "sampleId";
            signedRequestParameters.oauth_token = "sampleToken";
            client.Setup(c => c.ParseSignedRequest(It.IsAny<string>())).Returns((object)signedRequestParameters);
            client.Setup(c => c.GetLoginUrl(It.IsAny<object>())).Returns(new Uri("https://www.facebook.com/dialog/oauth?redirect_uri=example.com"));
            return client.Object;
        }

        public static IFacebookPermissionService CreatePermissionService(string[] permissionsToReturn)
        {
            Mock<IFacebookPermissionService> client = new Mock<IFacebookPermissionService>();
            client.Setup(p => p.GetUserPermissions(It.IsAny<string>(), It.IsAny<string>())).Returns(permissionsToReturn);
            return client.Object;
        }
    }
}