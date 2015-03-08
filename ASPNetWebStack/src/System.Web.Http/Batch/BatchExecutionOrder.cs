﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http.Batch
{
    /// <summary>
    /// Defines the order of execution for batch requests.
    /// </summary>
    public enum BatchExecutionOrder
    {
        /// <summary>
        /// Executes the batch requests sequentially.
        /// </summary>
        Sequential = 0,

        /// <summary>
        /// Executes the batch requests non-sequentially.
        /// </summary>
        NonSequential
    }
}