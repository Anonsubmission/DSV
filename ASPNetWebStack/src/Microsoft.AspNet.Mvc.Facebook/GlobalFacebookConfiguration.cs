﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Facebook.Providers;

namespace Microsoft.AspNet.Mvc.Facebook
{
    /// <summary>
    /// Provides a global <see cref="FacebookConfiguration"/> for ASP.NET applications.
    /// </summary>
    public static class GlobalFacebookConfiguration
    {
        private static readonly Lazy<FacebookConfiguration> _configuration = new Lazy<FacebookConfiguration>(
        () =>
        {
            FacebookConfiguration config = new FacebookConfiguration();
            config.ClientProvider = new DefaultFacebookClientProvider(config);
            config.PermissionService = new DefaultFacebookPermissionService(config);
            return config;
        });

        /// <summary>
        /// Gets the global <see cref="FacebookConfiguration"/>.
        /// </summary>
        public static FacebookConfiguration Configuration
        {
            get
            {
                return _configuration.Value;
            }
        }
    }
}