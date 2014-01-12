﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http.Tracing
{
    internal static class TraceLevelHelper
    {
        public static bool IsDefined(TraceLevel traceLevel)
        {
            return traceLevel == TraceLevel.Off ||
                   traceLevel == TraceLevel.Debug ||
                   traceLevel == TraceLevel.Info ||
                   traceLevel == TraceLevel.Warn ||
                   traceLevel == TraceLevel.Error ||
                   traceLevel == TraceLevel.Fatal;
        }

        public static void Validate(TraceLevel value, string parameterValue)
        {
            if (!IsDefined(value))
            {
                throw Error.InvalidEnumArgument(parameterValue, (int)value, typeof(TraceLevel));
            }
        }
    }
}
