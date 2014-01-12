﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http.Controllers;

namespace System.Web.Http.ModelBinding.Binders
{
    // Returns a binder that can extract a ValueProviderResult.RawValue and return it directly.
    public sealed class TypeMatchModelBinderProvider : ModelBinderProvider
    {
        private static readonly TypeMatchModelBinder _binder = new TypeMatchModelBinder();

        public override IModelBinder GetBinder(HttpConfiguration configuration, Type modelType)
        {
            return _binder;
        }
    }
}
