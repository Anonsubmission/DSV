﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.Facebook.Models
{
    /// <summary>
    /// Change entry from Facebook as part of Realtime Updates.
    /// </summary>
    public class ChangeEntry
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [JsonProperty("UId")]
        public long UserId { get; set; }

        /// <summary>
        /// Gets or sets the changed fields.
        /// </summary>
        [JsonProperty("Changed_Fields")]
        public IEnumerable<string> ChangedFields { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        public long Time { get; set; }
    }
}