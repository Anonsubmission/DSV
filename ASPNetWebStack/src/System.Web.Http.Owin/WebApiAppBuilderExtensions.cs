﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Owin;

namespace Owin
{
    /// <summary>
    /// Provides extension methods for the <see cref="IAppBuilder"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WebApiAppBuilderExtensions
    {
        private static readonly IHostBufferPolicySelector _defaultBufferPolicySelector = new OwinBufferPolicySelector();

        /// <summary>
        /// Adds a component to the OWIN pipeline for running a Web API endpoint.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configuration">The <see cref="HttpConfiguration"/> used to configure the endpoint.</param>
        /// <returns>The application builder.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Not out of scope")]
        public static IAppBuilder UseWebApi(this IAppBuilder builder, HttpConfiguration configuration)
        {
            IHostBufferPolicySelector bufferPolicySelector = configuration.Services.GetHostBufferPolicySelector() ?? _defaultBufferPolicySelector;
            return builder.Use(typeof(HttpMessageHandlerAdapter), new HttpServer(configuration), bufferPolicySelector);
        }

        /// <summary>
        /// Adds a component to the OWIN pipeline for running a Web API endpoint.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configuration">The <see cref="HttpConfiguration"/> used to configure the endpoint.</param>
        /// <param name="dispatcher">The dispatcher responsible for handling incoming requests.</param>
        /// <returns>The application builder.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Not out of scope")]
        public static IAppBuilder UseWebApi(this IAppBuilder builder, HttpConfiguration configuration, HttpMessageHandler dispatcher)
        {
            IHostBufferPolicySelector bufferPolicySelector = configuration.Services.GetHostBufferPolicySelector() ?? _defaultBufferPolicySelector;
            return builder.Use(typeof(HttpMessageHandlerAdapter), new HttpServer(configuration, dispatcher), bufferPolicySelector);
        }

        /// <summary>
        /// Adds a component to the OWIN pipeline for running an <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <returns>The application builder.</returns>
        public static IAppBuilder UseHttpMessageHandler(this IAppBuilder builder, HttpMessageHandler messageHandler)
        {
            return builder.Use(typeof(HttpMessageHandlerAdapter), messageHandler, _defaultBufferPolicySelector);
        }
    }
}
