﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Formatter.Serialization
{
    internal static class ODataSerializerProviderExtensions
    {
        public static ODataEdmTypeSerializer GetEdmTypeSerializer(this ODataSerializerProvider serializerProvider, IEdmModel model, object instance)
        {
            Contract.Assert(serializerProvider != null);
            Contract.Assert(model != null);
            Contract.Assert(instance != null);

            Contract.Assert(instance != null);

            IEdmObject edmObject = instance as IEdmObject;
            if (edmObject != null)
            {
                return serializerProvider.GetEdmTypeSerializer(edmObject.GetEdmType());
            }

            return serializerProvider.GetODataPayloadSerializer(model, instance.GetType()) as ODataEdmTypeSerializer;
        }
    }
}
