﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
namespace Microsoft.AspNet.Mvc.Facebook.Test.Types
{
    public class UserTypeWithIgnoredProperties
    {
        public string Id { get; set; }

        [JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        public FacebookConnection<FacebookPicture> Picture { get; set; }
    }
}
