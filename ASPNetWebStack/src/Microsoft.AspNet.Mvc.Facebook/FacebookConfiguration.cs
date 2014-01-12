﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Globalization;
using Microsoft.AspNet.Mvc.Facebook.Providers;

namespace Microsoft.AspNet.Mvc.Facebook
{
    /// <summary>
    /// Configuration for the Facebook application.
    /// </summary>
    public class FacebookConfiguration
    {
        private static readonly string FacebookAppBaseUrl = "https://apps.facebook.com";
        private readonly ConcurrentDictionary<object, object> _properties = new ConcurrentDictionary<object, object>();
        private string _appUrl;
        private string _authorizationRedirectPath;

        /// <summary>
        /// Gets or sets the App ID.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the App Secret.
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the App Namespace.
        /// </summary>
        public string AppNamespace { get; set; }

        /// <summary>
        /// Gets or sets the URL path that the <see cref="Microsoft.AspNet.Mvc.Facebook.Authorization.FacebookAuthorizeFilter"/> will redirect to when the user did not grant the required permissions.
        /// </summary>
        public string AuthorizationRedirectPath
        {
            get
            {
                return _authorizationRedirectPath;
            }
            set
            {
                // Check for '~/' prefix while allowing null or empty value to be set.
                if (!String.IsNullOrEmpty(value) && !value.StartsWith("~/"))
                {
                    throw new ArgumentException(Resources.InvalidAuthorizationRedirectPath, "value");
                }
                _authorizationRedirectPath = value;
            }
        }

        /// <summary>
        /// Gets or sets the absolute URL for the Facebook App.
        /// </summary>
        public string AppUrl
        {
            get
            {
                if (String.IsNullOrEmpty(_appUrl))
                {
                    _appUrl = GetAppUrl();
                }
                return _appUrl;
            }
            set
            {
                _appUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IFacebookClientProvider"/>.
        /// </summary>
        public IFacebookClientProvider ClientProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IFacebookPermissionService"/>.
        /// </summary>
        public IFacebookPermissionService PermissionService { get; set; }

        /// <summary>
        /// Gets the additional properties associated with this instance.
        /// </summary>
        public ConcurrentDictionary<object, object> Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// Loads the configuration properties from app settings.
        /// </summary>
        /// <remarks>
        /// It will map the following keys from appSettings to the corresponding properties:
        /// Facebook:AppId = AppId,
        /// Facebook:AppSecret = AppSecret,
        /// Facebook:AppNamespace = AppNamespace,
        /// Facebook:AppUrl = AppUrl,
        /// Facebook:AuthorizationRedirectPath = AuthorizationRedirectPath.
        /// </remarks>
        public virtual void LoadFromAppSettings()
        {
            AppId = ConfigurationManager.AppSettings[FacebookAppSettingKeys.AppId];
            if (String.IsNullOrEmpty(AppId))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Resources.AppSettingIsRequired,
                    FacebookAppSettingKeys.AppId));
            }

            AppSecret = ConfigurationManager.AppSettings[FacebookAppSettingKeys.AppSecret];
            if (String.IsNullOrEmpty(AppSecret))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Resources.AppSettingIsRequired,
                    FacebookAppSettingKeys.AppSecret));
            }

            AppNamespace = ConfigurationManager.AppSettings[FacebookAppSettingKeys.AppNamespace];
            AppUrl = ConfigurationManager.AppSettings[FacebookAppSettingKeys.AppUrl];
            AuthorizationRedirectPath = ConfigurationManager.AppSettings[FacebookAppSettingKeys.AuthorizationRedirectPath];
        }

        private string GetAppUrl()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                FacebookAppBaseUrl,
                String.IsNullOrEmpty(AppNamespace) ? AppId : AppNamespace);
        }
    }
}