﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Cors
{
    /// <summary>
    /// Provides an abstraction for evaluating CORS requests based on <see cref="CorsPolicy"/>.
    /// </summary>
    public interface ICorsEngine
    {
        /// <summary>
        /// Evaluates the policy.
        /// </summary>
        /// <param name="requestContext">The <see cref="CorsRequestContext"/>.</param>
        /// <param name="policy">The <see cref="CorsPolicy"/>.</param>
        /// <returns>The <see cref="CorsResult"/></returns>
        CorsResult EvaluatePolicy(CorsRequestContext requestContext, CorsPolicy policy);
    }
}