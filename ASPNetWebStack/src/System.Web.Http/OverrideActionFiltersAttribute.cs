﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http.Filters;

namespace System.Web.Http
{
    /// <summary>Represents a filter attribute that overrides action filters defined at a higher level.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class OverrideActionFiltersAttribute : Attribute, IOverrideFilter
    {
        /// <inheritdoc />
        public bool AllowMultiple
        {
            get { return false; }
        }

        /// <inheritdoc />
        public Type FiltersToOverride
        {
            get { return typeof(IActionFilter); }
        }
    }
}
