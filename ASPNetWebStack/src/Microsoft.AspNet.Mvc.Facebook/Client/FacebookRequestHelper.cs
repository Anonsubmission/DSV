﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Web;

namespace Microsoft.AspNet.Mvc.Facebook.Client
{
    internal static class FacebookRequestHelpers
    {
        private static readonly string SignedRequestKey = "signed_request";
        private static readonly string ParsedSignedRequestKey = "parsed_signed_request";

        public static dynamic GetSignedRequest(HttpContextBase context, Func<string, object> parseSignedRequest)
        {
            if (context.Items.Contains(ParsedSignedRequestKey))
            {
                return context.Items[ParsedSignedRequestKey];
            }

            string rawSignedRequest = context.Request.Form[SignedRequestKey] ?? context.Request.QueryString[SignedRequestKey];
            object signedRequest = null;
            if (!String.IsNullOrEmpty(rawSignedRequest))
            {
                signedRequest = parseSignedRequest(rawSignedRequest);
                context.Items.Add(ParsedSignedRequestKey, signedRequest);
            }
            return signedRequest;
        }
    }
}
