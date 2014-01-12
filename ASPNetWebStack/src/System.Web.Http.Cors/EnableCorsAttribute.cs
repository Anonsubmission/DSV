﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors.Properties;

namespace System.Web.Http.Cors
{
    /// <summary>
    /// This class defines an attribute that can be applied to an action or a controller to enable CORS.
    /// By default, it allows all origins, methods and headers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Attribute arguments are accessible as collections.")]
    public sealed class EnableCorsAttribute : Attribute, ICorsPolicyProvider
    {
        private CorsPolicy _corsPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableCorsAttribute" /> class.
        /// </summary>
        /// <param name="origins">Comma-separated list of origins that are allowed to access the resource. Use "*" to allow all.</param>
        /// <param name="headers">Comma-separated list of headers that are supported by the resource. Use "*" to allow all. Use null or empty string to allow none.</param>
        /// <param name="methods">Comma-separated list of methods that are supported by the resource. Use "*" to allow all. Use null or empty string to allow none.</param>
        public EnableCorsAttribute(string origins, string headers, string methods)
            : this(origins, headers, methods, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableCorsAttribute" /> class.
        /// </summary>
        /// <param name="origins">Comma-separated list of origins that are allowed to access the resource. Use "*" to allow all.</param>
        /// <param name="headers">Comma-separated list of headers that are supported by the resource. Use "*" to allow all. Use null or empty string to allow none.</param>
        /// <param name="methods">Comma-separated list of methods that are supported by the resource. Use "*" to allow all. Use null or empty string to allow none.</param>
        /// <param name="exposedHeaders">Comma-separated list of headers that the resource might use and can be exposed. Use null or empty string to expose none.</param>
        public EnableCorsAttribute(string origins, string headers, string methods, string exposedHeaders)
        {
            if (String.IsNullOrEmpty(origins))
            {
                throw new ArgumentException(
                    SRResources.ArgumentCannotBeNullOrEmpty,
                    "origins");
            }

            _corsPolicy = new CorsPolicy();
            if (origins == "*")
            {
                _corsPolicy.AllowAnyOrigin = true;
            }
            else
            {
                AddCommaSeparatedValuesToCollection(origins, _corsPolicy.Origins);
            }

            if (!String.IsNullOrEmpty(headers))
            {
                if (headers == "*")
                {
                    _corsPolicy.AllowAnyHeader = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(headers, _corsPolicy.Headers);
                }
            }

            if (!String.IsNullOrEmpty(methods))
            {
                if (methods == "*")
                {
                    _corsPolicy.AllowAnyMethod = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(methods, _corsPolicy.Methods);
                }
            }

            if (!String.IsNullOrEmpty(exposedHeaders))
            {
                AddCommaSeparatedValuesToCollection(exposedHeaders, _corsPolicy.ExposedHeaders);
            }
        }

        /// <summary>
        /// Gets the headers that the resource might use and can be exposed.
        /// </summary>
        public IList<string> ExposedHeaders
        {
            get
            {
                return _corsPolicy.ExposedHeaders;
            }
        }

        /// <summary>
        /// Gets the headers that are supported by the resource.
        /// </summary>
        public IList<string> Headers
        {
            get
            {
                return _corsPolicy.Headers;
            }
        }

        /// <summary>
        /// Gets the methods that are supported by the resource.
        /// </summary>
        public IList<string> Methods
        {
            get
            {
                return _corsPolicy.Methods;
            }
        }

        /// <summary>
        /// Gets the origins that are allowed to access the resource.
        /// </summary>
        public IList<string> Origins
        {
            get
            {
                return _corsPolicy.Origins;
            }
        }

        /// <summary>
        /// Gets or sets the number of seconds the results of a preflight request can be cached.
        /// </summary>
        public long PreflightMaxAge
        {
            get
            {
                return _corsPolicy.PreflightMaxAge ?? -1;
            }
            set
            {
                _corsPolicy.PreflightMaxAge = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the resource supports user credentials in the request.
        /// </summary>
        public bool SupportsCredentials
        {
            get
            {
                return _corsPolicy.SupportsCredentials;
            }
            set
            {
                _corsPolicy.SupportsCredentials = value;
            }
        }

        /// <inheritdoc />
        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_corsPolicy);
        }

        private static void AddCommaSeparatedValuesToCollection(string commaSeparatedValues, IList<string> collection)
        {
            string[] values = commaSeparatedValues.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i].Trim();
                if (!String.IsNullOrEmpty(value))
                {
                    collection.Add(value);
                }
            }
        }
    }
}