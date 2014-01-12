﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Formatter.Deserialization
{
    /// <summary>
    /// Represents a factory that creates an <see cref="ODataDeserializer"/>.
    /// </summary>
    public abstract class ODataDeserializerProvider
    {
        /// <summary>
        /// Gets the <see cref="ODataEdmTypeDeserializer"/> for the given EDM type.
        /// </summary>
        /// <param name="edmType">The EDM type.</param>
        /// <returns>An <see cref="ODataEdmTypeDeserializer"/> that can deserialize the given EDM type.</returns>
        public abstract ODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType);

        /// <summary>
        /// Gets an <see cref="ODataDeserializer"/> for the given type.
        /// </summary>
        /// <param name="model">The EDM model.</param>
        /// <param name="type">The CLR type.</param>
        /// <returns>An <see cref="ODataDeserializer"/> that can deserialize the given type.</returns>
        public abstract ODataDeserializer GetODataDeserializer(IEdmModel model, Type type);
    }
}
