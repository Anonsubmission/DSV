﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using Facebook;
using Microsoft.AspNet.Mvc.Facebook.Client;

namespace Microsoft.AspNet.Mvc.Facebook.ModelBinders
{
    /// <summary>
    /// Model binds an action method parameter to a <see cref="FacebookRedirectContext"/>.
    /// </summary>
    public class FacebookRedirectContextModelBinder : IModelBinder
    {
        private FacebookConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookRedirectContextModelBinder" /> class.
        /// </summary>
        /// <param name="config">The <see cref="FacebookConfiguration"/>.</param>
        public FacebookRedirectContextModelBinder(FacebookConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _config = config;
        }

        /// <summary>
        /// Binds the model to a value by using the specified controller context and binding context.
        /// </summary>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="bindingContext">The binding context.</param>
        /// <returns>
        /// The bound value.
        /// </returns>
        public virtual object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            HttpRequestBase request = controllerContext.HttpContext.Request;
            string originUrl = request.QueryString["originUrl"];
            string permissions = request.QueryString["permissions"];

            if (!String.IsNullOrEmpty(originUrl))
            {
                if (!originUrl.StartsWith(_config.AppUrl, StringComparison.OrdinalIgnoreCase))
                {
                    bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                        String.Format(CultureInfo.CurrentCulture, Resources.UrlCannotBeExternal, "originUrl", _config.AppUrl));
                }
            }
            else
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                    String.Format(CultureInfo.CurrentCulture, Resources.ParameterIsRequired, "originUrl"));
            }

            if (String.IsNullOrEmpty(permissions))
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                    String.Format(CultureInfo.CurrentCulture, Resources.ParameterIsRequired, "permissions"));
            }

            string redirectUrl = null;
            string[] requiredPermissions = permissions != null ? permissions.Split(',') : new string[0];
            if (bindingContext.ModelState.IsValid)
            {
                FacebookClient client = _config.ClientProvider.CreateClient();
                redirectUrl = client.GetLoginUrl(originUrl, _config.AppId, permissions).AbsoluteUri;
            }

            return new FacebookRedirectContext
            {
                OriginUrl = originUrl,
                RequiredPermissions = requiredPermissions,
                RedirectUrl = redirectUrl,
                Configuration = _config
            };
        }
    }
}