﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;

namespace System.Web.Http.Cors
{
    /// <summary>
    /// Provides an abstraction for getting the <see cref="ICorsPolicyProvider"/>.
    /// </summary>
    public interface ICorsPolicyProviderFactory
    {
        /// <summary>
        /// Gets the <see cref="ICorsPolicyProvider"/> for the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="ICorsPolicyProvider"/>.</returns>
        ICorsPolicyProvider GetCorsPolicyProvider(HttpRequestMessage request);
    }
}