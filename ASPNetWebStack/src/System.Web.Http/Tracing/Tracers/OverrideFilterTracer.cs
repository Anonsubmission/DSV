﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http.Filters;
using System.Web.Http.Services;

namespace System.Web.Http.Tracing.Tracers
{
    internal class OverrideFilterTracer : FilterTracer, IOverrideFilter, IDecorator<IOverrideFilter>
    {
        private readonly IOverrideFilter _innerFilter;

        public OverrideFilterTracer(IOverrideFilter innerFilter, ITraceWriter traceWriter)
            : base(innerFilter, traceWriter)
        {
            _innerFilter = innerFilter;
        }

        public new IOverrideFilter Inner
        {
            get { return _innerFilter; }
        }

        public Type FiltersToOverride
        {
            get { return _innerFilter.FiltersToOverride; }
        }
    }
}
