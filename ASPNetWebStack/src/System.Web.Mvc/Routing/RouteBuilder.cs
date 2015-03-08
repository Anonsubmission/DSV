﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Routing;

namespace System.Web.Mvc.Routing
{
    /// <summary>
    /// Builds <see cref="Route"/> instances based on route information.
    /// </summary>
    public class RouteBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteBuilder" /> class using the default inline constraint resolver.
        /// </summary>
        public RouteBuilder()
            : this(new DefaultInlineConstraintResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteBuilder" /> class.
        /// </summary>
        /// <param name="constraintResolver">The <see cref="IInlineConstraintResolver"/> to use for resolving inline constraints.</param>
        public RouteBuilder(IInlineConstraintResolver constraintResolver)
        {
            if (constraintResolver == null)
            {
                throw Error.ArgumentNull("constraintResolver");
            }

            ConstraintResolver = constraintResolver;
        }

        public IInlineConstraintResolver ConstraintResolver { get; private set; }

        /// <summary>
        /// Builds an <see cref="Route"/> for a particular action.
        /// </summary>
        /// <param name="routeTemplate">The tokenized route template for the route.</param>
        /// <param name="allowedMethods">The HTTP methods supported by the route. A null value specify that all possible methods are supported.</param>
        /// <param name="controllerName">The name of the associated controller.</param>
        /// <param name="actionName">The name of the associated action.</param>
        /// <param name="targetMethod">The method that the route attribute has been applied on.</param>
        /// <param name="areaName"></param>
        /// <returns>The generated <see cref="Route"/>.</returns>
        public Route BuildDirectRoute(string routeTemplate, IEnumerable<string> allowedMethods, string controllerName, string actionName, MethodInfo targetMethod, string areaName)
        {
            if (routeTemplate == null)
            {
                throw Error.ArgumentNull("routeTemplate");
            }

            if (controllerName == null)
            {
                throw Error.ArgumentNull("controllerName");
            }

            if (actionName == null)
            {
                throw Error.ArgumentNull("actionName");
            }

            RouteValueDictionary defaults = new RouteValueDictionary
            {
                { "controller", controllerName },
                { "action", actionName }
            };

            RouteValueDictionary constraints = new RouteValueDictionary();
            if (allowedMethods != null)
            {
                // Current method constraint implementation is inefficient since it matches before running the constraint.
                // Consider checking the HTTP method first in a custom route as a performance optimization.
                constraints.Add("httpMethod", new HttpMethodConstraint(allowedMethods.ToArray()));
            }

            RouteValueDictionary dataTokens = new RouteValueDictionary();
            if (areaName != null)
            {
                dataTokens.Add(RouteDataTokenKeys.Area, areaName);
                dataTokens.Add(RouteDataTokenKeys.UseNamespaceFallback, value: false);
                if (targetMethod.DeclaringType != null)
                {
                    dataTokens.Add(RouteDataTokenKeys.Namespaces, new[] { targetMethod.DeclaringType.Namespace });
                }
            }

            string detokenizedRouteTemplate = InlineRouteTemplateParser.ParseRouteTemplate(routeTemplate, defaults, constraints, ConstraintResolver);

            return BuildDirectRoute(defaults, constraints, dataTokens, detokenizedRouteTemplate, targetMethod);
        }

        /// <summary>
        /// Builds an <see cref="Route"/>.
        /// </summary>
        /// <param name="defaults">The route defaults.</param>
        /// <param name="constraints">The route constraints.</param>
        /// <param name="dataTokens"></param>
        /// <param name="routeTemplate">The detokenized route template.</param>
        /// <param name="targetMethod">The method that the route attribute has been applied on.</param>
        /// <returns>The generated <see cref="Route"/>.</returns>
        public virtual Route BuildDirectRoute(RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, string routeTemplate, MethodInfo targetMethod)
        {
            Route route = new Route(routeTemplate, new MvcRouteHandler())
            {
                Defaults = defaults,
                Constraints = constraints,
                DataTokens = dataTokens
            };

            route.SetTargetActionMethod(targetMethod);
            return route;
        }
    }
}