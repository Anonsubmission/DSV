﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Formatter.Serialization
{
    /// <summary>
    /// Represents an <see cref="ODataSerializer"/> for serializing the raw value of an <see cref="IEdmPrimitiveType"/>.
    /// </summary>
    public class ODataRawValueSerializer : ODataSerializer
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ODataPrimitiveSerializer"/>.
        /// </summary>
        public ODataRawValueSerializer()
            : base(ODataPayloadKind.Value)
        {
        }

        /// <inheritdoc/>
        public override void WriteObject(object graph, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            if (messageWriter == null)
            {
                throw Error.ArgumentNull("messageWriter");
            }

            messageWriter.WriteValue(ODataPrimitiveSerializer.ConvertUnsupportedPrimitives(graph));
        }
    }
}
