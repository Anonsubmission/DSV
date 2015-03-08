﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Facebook;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.Facebook.Providers
{
    /// <summary>
    /// Default implementation of <see cref="IFacebookClientProvider"/>.
    /// </summary>
    public class DefaultFacebookClientProvider : IFacebookClientProvider
    {
        private FacebookConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultFacebookClientProvider" /> class.
        /// </summary>
        /// <param name="configuration">The <see cref="FacebookConfiguration"/>.</param>
        public DefaultFacebookClientProvider(FacebookConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _config = configuration;
        }

        /// <summary>
        /// Creates a <see cref="FacebookClient"/> with AppId and AppSecret that uses Json.NET for serialization and deserialization.
        /// </summary>
        /// <returns>The <see cref="FacebookClient"/> instance.</returns>
        public virtual FacebookClient CreateClient()
        {
            FacebookClient client = new FacebookClient();
            client.AppId = _config.AppId;
            client.AppSecret = _config.AppSecret;
            client.SetJsonSerializers(JsonConvert.SerializeObject, JsonConvert.DeserializeObject);
            return client;
        }
    }
}