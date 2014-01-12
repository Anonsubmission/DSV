﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Web;
using Facebook;
using Microsoft.AspNet.Mvc.Facebook.Client;

namespace Microsoft.AspNet.Mvc.Facebook
{
    internal sealed class FacebookAuthenticationModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += (sender, e) =>
            {
                HttpContext httpContext = ((HttpApplication)sender).Context;

                dynamic signedRequest = FacebookRequestHelpers.GetSignedRequest(
                    new HttpContextWrapper(httpContext),
                    rawSignedRequest =>
                    {
                        FacebookConfiguration config = GlobalFacebookConfiguration.Configuration;
                        FacebookClient client = config.ClientProvider.CreateClient();
                        return client.ParseSignedRequest(rawSignedRequest);
                    });

                if (signedRequest != null)
                {
                    string userId = signedRequest.user_id;
                    if (!String.IsNullOrEmpty(userId))
                    {
                        ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
                        Thread.CurrentPrincipal = principal;
                        httpContext.User = principal;
                    }
                }
            };
        }

        public void Dispose()
        {
        }
    }
}