﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;

namespace System.Web.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpRouteCollection"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ODataHttpRouteCollectionExtensions
    {
        /// <summary>
        /// Maps the specified OData route.
        /// </summary>
        /// <param name="routes">A collection of routes for the application.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        public static void MapODataRoute(this HttpRouteCollection routes, string routeName, string routePrefix, IEdmModel model)
        {
            routes.MapODataRoute(routeName, routePrefix, model, batchHandler: null);
        }

        /// <summary>
        /// Maps the specified OData route. When the <paramref name="batchHandler"/> is provided, it will create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="routes">A collection of routes for the application.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        public static void MapODataRoute(this HttpRouteCollection routes, string routeName, string routePrefix, IEdmModel model, ODataBatchHandler batchHandler)
        {
            routes.MapODataRoute(routeName, routePrefix, model, new DefaultODataPathHandler(), ODataRoutingConventions.CreateDefault(), batchHandler);
        }

        /// <summary>
        /// Maps the specified OData route.
        /// </summary>
        /// <param name="routes">A collection of routes for the application.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler"/> to use for parsing the OData path.</param>
        /// <param name="routingConventions">The OData routing conventions to use for controller and action selection.</param>
        public static void MapODataRoute(this HttpRouteCollection routes, string routeName, string routePrefix, IEdmModel model,
            IODataPathHandler pathHandler, IEnumerable<IODataRoutingConvention> routingConventions)
        {
            routes.MapODataRoute(routeName, routePrefix, model, pathHandler, routingConventions, batchHandler: null);
        }

        /// <summary>
        /// Maps the specified OData route. When the <paramref name="batchHandler"/> is provided, it will create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="routes">A collection of routes for the application.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler" /> to use for parsing the OData path.</param>
        /// <param name="routingConventions">The OData routing conventions to use for controller and action selection.</param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We want the handler to be a batch handler.")]
        public static void MapODataRoute(this HttpRouteCollection routes, string routeName, string routePrefix, IEdmModel model,
            IODataPathHandler pathHandler, IEnumerable<IODataRoutingConvention> routingConventions, ODataBatchHandler batchHandler)
        {
            if (routes == null)
            {
                throw Error.ArgumentNull("routes");
            }

            if (!String.IsNullOrEmpty(routePrefix))
            {
                int prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    // Remove the last trailing slash if it has one.
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }

            if (batchHandler != null)
            {
                batchHandler.ODataRouteName = routeName;
                string batchTemplate = String.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch : routePrefix + '/' + ODataRouteConstants.Batch;
                routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
            }

            ODataPathRouteConstraint routeConstraint = new ODataPathRouteConstraint(pathHandler, model, routeName, routingConventions);
            routes.Add(routeName, new ODataRoute(routePrefix, routeConstraint));
        }
    }
}