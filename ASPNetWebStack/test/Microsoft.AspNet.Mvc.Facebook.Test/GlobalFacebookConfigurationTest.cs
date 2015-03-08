﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.TestCommon;

namespace Microsoft.AspNet.Mvc.Facebook.Test
{
    public class GlobalFacebookConfigurationTest
    {
        [Fact]
        public void Default_Configuration()
        {
            FacebookConfiguration config = GlobalFacebookConfiguration.Configuration;
            Assert.Null(config.AppId);
            Assert.Null(config.AppNamespace);
            Assert.Null(config.AppSecret);
            Assert.NotNull(config.AppUrl);
            Assert.Null(config.AuthorizationRedirectPath);
            Assert.NotNull(config.ClientProvider);
            Assert.NotNull(config.PermissionService);
            Assert.NotNull(config.Properties);
        }
    }
}