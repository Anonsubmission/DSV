﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http.OData.Properties;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// Represents a single order by expression in the $orderby clause.
    /// </summary>
    public abstract class OrderByNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByNode"/> class.
        /// </summary>
        /// <param name="direction">The direction of the sort order.</param>
        protected OrderByNode(OrderByDirection direction)
        {
            Direction = direction;
        }

        /// <summary>
        /// Gets the <see cref="OrderByDirection"/> for the current node.
        /// </summary>
        public OrderByDirection Direction { get; private set; }

        /// <summary>
        /// Creates a list of <see cref="OrderByPropertyNode"/> instances from a linked list of <see cref="OrderByClause"/> instances.
        /// </summary>
        /// <param name="orderByClause">The head of the <see cref="OrderByClause"/> linked list.</param>
        /// <returns>The list of new <see cref="OrderByPropertyNode"/> instances.</returns>
        public static IList<OrderByNode> CreateCollection(OrderByClause orderByClause)
        {
            List<OrderByNode> result = new List<OrderByNode>();
            for (OrderByClause clause = orderByClause; clause != null; clause = clause.ThenBy)
            {
                if (clause.Expression is NonentityRangeVariableReferenceNode || clause.Expression is EntityRangeVariableReferenceNode)
                {
                    result.Add(new OrderByItNode(clause.Direction));
                }
                else
                {
                    SingleValuePropertyAccessNode property = clause.Expression as SingleValuePropertyAccessNode;

                    if (property == null || !(property.Source is EntityRangeVariableReferenceNode))
                    {
                        throw new ODataException(SRResources.OrderByClauseNotSupported);
                    }

                    result.Add(new OrderByPropertyNode(property.Property, clause.Direction));
                }
            }

            return result;
        }
    }
}
