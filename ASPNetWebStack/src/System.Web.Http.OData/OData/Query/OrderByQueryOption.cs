﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Properties;
using System.Web.Http.OData.Query.Validators;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// This defines a $orderby OData query option for querying.
    /// </summary>
    public class OrderByQueryOption
    {
        private OrderByClause _orderByClause;
        private IList<OrderByNode> _orderByNodes;

        /// <summary>
        /// Initialize a new instance of <see cref="OrderByQueryOption"/> based on the raw $orderby value and 
        /// an EdmModel from <see cref="ODataQueryContext"/>.
        /// </summary>
        /// <param name="rawValue">The raw value for $orderby query. It can be null or empty.</param>
        /// <param name="context">The <see cref="ODataQueryContext"/> which contains the <see cref="IEdmModel"/> and some type information</param>
        public OrderByQueryOption(string rawValue, ODataQueryContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (String.IsNullOrEmpty(rawValue))
            {
                throw Error.ArgumentNullOrEmpty("rawValue");
            }

            Context = context;
            RawValue = rawValue;
            Validator = new OrderByQueryValidator();
        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>.
        /// </summary>
        public ODataQueryContext Context { get; private set; }

        /// <summary>
        /// Gets the mutable list of <see cref="OrderByPropertyNode"/> instances for this query option.
        /// </summary>
        public IList<OrderByNode> OrderByNodes
        {
            get
            {
                if (_orderByNodes == null)
                {
                    _orderByNodes = OrderByNode.CreateCollection(OrderByClause);
                }
                return _orderByNodes;
            }
        }

        /// <summary>
        ///  Gets the raw $orderby value.
        /// </summary>
        public string RawValue { get; private set; }

        /// <summary>
        /// Gets or sets the OrderBy Query Validator.
        /// </summary>
        public OrderByQueryValidator Validator { get; set; }

        /// <summary>
        /// Gets the parsed <see cref="OrderByClause"/> for this query option.
        /// </summary>
        private OrderByClause OrderByClause
        {
            get
            {
                if (_orderByClause == null)
                {
                    _orderByClause = ODataUriParser.ParseOrderBy(RawValue, Context.Model, Context.ElementType);
                }
                return _orderByClause;
            }
        }

        /// <summary>
        /// Apply the $orderby query to the given IQueryable.
        /// </summary>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <returns>The new <see cref="IQueryable"/> after the orderby query has been applied to.</returns>
        public IOrderedQueryable<T> ApplyTo<T>(IQueryable<T> query)
        {
            return ApplyToCore(query) as IOrderedQueryable<T>;
        }

        /// <summary>
        /// Apply the $orderby query to the given IQueryable.
        /// </summary>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <returns>The new <see cref="IQueryable"/> after the orderby query has been applied to.</returns>
        public IOrderedQueryable ApplyTo(IQueryable query)
        {
            return ApplyToCore(query);
        }

        /// <summary>
        /// Validate the orderby query based on the given <paramref name="validationSettings"/>. It throws an ODataException if validation failed.
        /// </summary>
        /// <param name="validationSettings">The <see cref="ODataValidationSettings"/> instance which contains all the validation settings.</param>
        public void Validate(ODataValidationSettings validationSettings)
        {
            if (validationSettings == null)
            {
                throw Error.ArgumentNull("validationSettings");
            }

            if (Validator != null)
            {
                Validator.Validate(this, validationSettings);
            }
        }

        private IOrderedQueryable ApplyToCore(IQueryable query)
        {
            ICollection<OrderByNode> nodes = OrderByNodes;

            bool alreadyOrdered = false;
            IQueryable querySoFar = query;

            HashSet<IEdmProperty> propertiesSoFar = new HashSet<IEdmProperty>();
            bool orderByItSeen = false;

            foreach (OrderByNode node in nodes)
            {
                OrderByPropertyNode propertyNode = node as OrderByPropertyNode;

                if (propertyNode != null)
                {
                    IEdmProperty property = propertyNode.Property;
                    OrderByDirection direction = propertyNode.Direction;

                    // This check prevents queries with duplicate properties (e.g. $orderby=Id,Id,Id,Id...) from causing stack overflows
                    if (propertiesSoFar.Contains(property))
                    {
                        throw new ODataException(Error.Format(SRResources.OrderByDuplicateProperty, property.Name));
                    }
                    propertiesSoFar.Add(property);

                    querySoFar = ExpressionHelpers.OrderByProperty(querySoFar, property, direction, Context.ElementClrType, alreadyOrdered);
                    alreadyOrdered = true;
                }
                else
                {
                    // This check prevents queries with duplicate nodes (e.g. $orderby=$it,$it,$it,$it...) from causing stack overflows
                    if (orderByItSeen)
                    {
                        throw new ODataException(Error.Format(SRResources.OrderByDuplicateIt));
                    }

                    querySoFar = ExpressionHelpers.OrderByIt(querySoFar, node.Direction, Context.ElementClrType, alreadyOrdered);
                    alreadyOrdered = true;
                    orderByItSeen = true;
                }
            }

            return querySoFar as IOrderedQueryable;
        }
    }
}
