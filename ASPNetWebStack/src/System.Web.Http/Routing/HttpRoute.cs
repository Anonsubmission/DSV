﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http.Properties;

namespace System.Web.Http.Routing
{
    /// <summary>
    /// Route class for self-host (i.e. hosted outside of ASP.NET). This class is mostly the
    /// same as the System.Web.Routing.Route implementation.
    /// This class has the same URL matching functionality as System.Web.Routing.Route. However,
    /// in order for this route to match when generating URLs, a special "httproute" key must be
    /// specified when generating the URL.
    /// </summary>
    public class HttpRoute : IHttpRoute
    {
        /// <summary>
        /// Key used to signify that a route URL generation request should include HTTP routes (e.g. Web API).
        /// If this key is not specified then no HTTP routes will match.
        /// </summary>
        public static readonly string HttpRouteKey = "httproute";

        private string _routeTemplate;
        private HttpRouteValueDictionary _defaults;
        private HttpRouteValueDictionary _constraints;
        private HttpRouteValueDictionary _dataTokens;

        public HttpRoute()
            : this(routeTemplate: null, defaults: null, constraints: null, dataTokens: null, handler: null)
        {
        }

        public HttpRoute(string routeTemplate)
            : this(routeTemplate, defaults: null, constraints: null, dataTokens: null, handler: null)
        {
        }

        public HttpRoute(string routeTemplate, HttpRouteValueDictionary defaults)
            : this(routeTemplate, defaults, constraints: null, dataTokens: null, handler: null)
        {
        }

        public HttpRoute(string routeTemplate, HttpRouteValueDictionary defaults, HttpRouteValueDictionary constraints)
            : this(routeTemplate, defaults, constraints, dataTokens: null, handler: null)
        {
        }

        public HttpRoute(string routeTemplate, HttpRouteValueDictionary defaults, HttpRouteValueDictionary constraints, HttpRouteValueDictionary dataTokens)
            : this(routeTemplate, defaults, constraints, dataTokens, handler: null)
        {
        }

        public HttpRoute(string routeTemplate, HttpRouteValueDictionary defaults, HttpRouteValueDictionary constraints, HttpRouteValueDictionary dataTokens, HttpMessageHandler handler)
        {
            _routeTemplate = routeTemplate == null ? String.Empty : routeTemplate;
            _defaults = defaults ?? new HttpRouteValueDictionary();
            _constraints = constraints ?? new HttpRouteValueDictionary();
            _dataTokens = dataTokens ?? new HttpRouteValueDictionary();
            Handler = handler;

            // The parser will throw for invalid routes.
            ParsedRoute = HttpRouteParser.Parse(RouteTemplate);
        }

        public IDictionary<string, object> Defaults
        {
            get { return _defaults; }
        }

        public IDictionary<string, object> Constraints
        {
            get { return _constraints; }
        }

        public IDictionary<string, object> DataTokens
        {
            get { return _dataTokens; }
        }

        public HttpMessageHandler Handler { get; private set; }

        public string RouteTemplate
        {
            get { return _routeTemplate; }
        }

        internal HttpParsedRoute ParsedRoute { get; private set; }

        public virtual IHttpRouteData GetRouteData(string virtualPathRoot, HttpRequestMessage request)
        {
            if (virtualPathRoot == null)
            {
                throw Error.ArgumentNull("virtualPathRoot");
            }

            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            // Note: we don't validate host/port as this is expected to be done at the host level
            string requestPath = "/" + request.RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (!requestPath.StartsWith(virtualPathRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relativeRequestPath = null;
            int virtualPathLength = virtualPathRoot.Length;
            if (requestPath.Length > virtualPathLength && requestPath[virtualPathLength] == '/')
            {
                relativeRequestPath = requestPath.Substring(virtualPathLength + 1);
            }
            else
            {
                relativeRequestPath = requestPath.Substring(virtualPathLength);
            }

            HttpRouteValueDictionary values = ParsedRoute.Match(relativeRequestPath, _defaults);
            if (values == null)
            {
                // If we got back a null value set, that means the URI did not match
                return null;
            }

            // Validate the values
            if (!ProcessConstraints(request, values, HttpRouteDirection.UriResolution))
            {
                return null;
            }

            return new HttpRouteData(this, values);
        }

        /// <summary>
        /// Attempt to generate a URI that represents the values passed in based on current
        /// values from the <see cref="HttpRouteData"/> and new values using the specified <see cref="HttpRoute"/>.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="values">The route values.</param>
        /// <returns>A <see cref="HttpVirtualPathData"/> instance or null if URI cannot be generated.</returns>
        public virtual IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request, IDictionary<string, object> values)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            // Only perform URL generation if the "httproute" key was specified. This allows these
            // routes to be ignored when a regular MVC app tries to generate URLs. Without this special
            // key an HTTP route used for Web API would normally take over almost all the routes in a
            // typical app.
            if (values != null && !values.Keys.Contains(HttpRouteKey, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }
            // Remove the value from the collection so that it doesn't affect the generated URL
            var newValues = GetRouteDictionaryWithoutHttpRouteKey(values);

            IHttpRouteData routeData = request.GetRouteData();
            IDictionary<string, object> requestValues = routeData == null ? null : routeData.Values;

            BoundRouteTemplate result = ParsedRoute.Bind(requestValues, newValues, _defaults, _constraints);
            if (result == null)
            {
                return null;
            }

            // Verify that the route matches the validation rules
            if (!ProcessConstraints(request, result.Values, HttpRouteDirection.UriGeneration))
            {
                return null;
            }

            return new HttpVirtualPathData(this, result.BoundTemplate);
        }

        private static IDictionary<string, object> GetRouteDictionaryWithoutHttpRouteKey(IDictionary<string, object> routeValues)
        {
            var newRouteValues = new HttpRouteValueDictionary();
            if (routeValues != null)
            {
                foreach (var routeValue in routeValues)
                {
                    if (!String.Equals(routeValue.Key, HttpRouteKey, StringComparison.OrdinalIgnoreCase))
                    {
                        newRouteValues.Add(routeValue.Key, routeValue.Value);
                    }
                }
            }
            return newRouteValues;
        }

        protected virtual bool ProcessConstraint(HttpRequestMessage request, object constraint, string parameterName, HttpRouteValueDictionary values, HttpRouteDirection routeDirection)
        {
            IHttpRouteConstraint customConstraint = constraint as IHttpRouteConstraint;
            if (customConstraint != null)
            {
                return customConstraint.Match(request, this, parameterName, values, routeDirection);
            }

            // If there was no custom constraint, then treat the constraint as a string which represents a Regex.
            string constraintsRule = constraint as string;
            if (constraintsRule == null)
            {
                throw Error.InvalidOperation(SRResources.Route_ValidationMustBeStringOrCustomConstraint, parameterName, RouteTemplate, typeof(IHttpRouteConstraint).Name);
            }

            object parameterValue;
            values.TryGetValue(parameterName, out parameterValue);
            string parameterValueString = Convert.ToString(parameterValue, CultureInfo.InvariantCulture);
            string constraintsRegEx = "^(" + constraintsRule + ")$";
            return Regex.IsMatch(parameterValueString, constraintsRegEx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private bool ProcessConstraints(HttpRequestMessage request, HttpRouteValueDictionary values, HttpRouteDirection routeDirection)
        {
            if (Constraints != null)
            {
                foreach (KeyValuePair<string, object> constraintsItem in Constraints)
                {
                    if (!ProcessConstraint(request, constraintsItem.Value, constraintsItem.Key, values, routeDirection))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
