﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http.Tracing
{
    internal static class TraceKindHelper
    {
        public static bool IsDefined(TraceKind traceKind)
        {
            return traceKind == TraceKind.Trace ||
                   traceKind == TraceKind.Begin ||
                   traceKind == TraceKind.End;
        }

        public static void Validate(TraceKind value, string parameterValue)
        {
            if (!IsDefined(value))
            {
                throw Error.InvalidEnumArgument(parameterValue, (int)value, typeof(TraceKind));
            }
        }
    }
}
