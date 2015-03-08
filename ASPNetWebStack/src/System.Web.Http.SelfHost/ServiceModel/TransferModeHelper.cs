﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.ServiceModel;

namespace System.Web.Http.SelfHost.ServiceModel
{
    internal static class TransferModeHelper
    {
        public static bool IsDefined(TransferMode transferMode)
        {
            return transferMode == TransferMode.Buffered ||
                   transferMode == TransferMode.Streamed ||
                   transferMode == TransferMode.StreamedRequest ||
                   transferMode == TransferMode.StreamedResponse;
        }

        public static void Validate(TransferMode value, string parameterValue)
        {
            if (!IsDefined(value))
            {
                throw Error.InvalidEnumArgument(parameterValue, (int)value, typeof(TransferMode));
            }
        }
    }
}
