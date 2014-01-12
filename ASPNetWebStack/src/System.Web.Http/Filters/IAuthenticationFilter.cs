﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Web.Http.Filters
{
    /// <summary>Defines a filter that performs authentication.</summary>
    public interface IAuthenticationFilter : IFilter
    {
        /// <summary>Authenticates the request.</summary>
        /// <param name="context">The authentication context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> that will perform authentication.</returns>
        Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken);

        /// <summary>Adds an authentication challenge to the inner <see cref="IHttpActionResult"/>.</summary>
        /// <param name="context">The authentication challenge context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> that will perform the authentication challenge.</returns>
        Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken);
    }
}
