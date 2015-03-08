﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Newtonsoft.Json;

namespace System.Web.Http.OData.Query.Expressions
{
    /// <summary>
    /// Represents a container class that contains properties that are either selected or expanded using $select and $expand.
    /// </summary>
    /// <typeparam name="TElement">The element being selected and expanded.</typeparam>
    [JsonConverter(typeof(SelectExpandWrapperConverter))]
    internal class SelectExpandWrapper<TElement> : IEdmEntityObject, IDictionaryConvertible
    {
        private Dictionary<string, object> _containerDict;

        /// <summary>
        /// Gets or sets the instance of the element being selected and expanded.
        /// </summary>
        public TElement Instance { get; set; }

        /// <summary>
        /// An ID to uniquely identify the model in the <see cref="ModelContainer"/>.
        /// </summary>
        public string ModelID { get; set; }

        /// <summary>
        /// Gets or sets the EDM type name of the element being selected and expanded. 
        /// </summary>
        /// <remarks>This is required by the <see cref="ODataMediaTypeFormatter"/> during serialization. If the instance property is not
        /// null, the type name will not be set as the type name can be figured from the instance runtime type.</remarks>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the property container that contains the properties being expanded. 
        /// </summary>
        public PropertyContainer Container { get; set; }

        /// <inheritdoc />
        public IEdmTypeReference GetEdmType()
        {
            IEdmModel model = GetModel();

            if (TypeName != null)
            {
                IEdmEntityType entityType = model.FindDeclaredType(TypeName) as IEdmEntityType;
                if (entityType == null)
                {
                    throw Error.InvalidOperation(SRResources.EntityTypeNotInModel, TypeName);
                }

                return new EdmEntityTypeReference(entityType, isNullable: false);
            }
            else
            {
                Type elementType = GetElementType();
                return model.GetEdmTypeReference(elementType);
            }
        }

        /// <inheritdoc />
        public bool TryGetPropertyValue(string propertyName, out object value)
        {
            // look into the container first to see if it has that property. container would have it 
            // if the property was expanded.
            if (Container != null)
            {
                _containerDict = _containerDict ?? Container.ToDictionary(includeAutoSelected: true);
                if (_containerDict.TryGetValue(propertyName, out value))
                {
                    return true;
                }
            }

            // fall back to the instance.
            Type elementType = GetElementType();
            PropertyInfo property = elementType.GetProperty(propertyName);
            if (property != null && Instance != null)
            {
                value = property.GetValue(Instance);
                return true;
            }

            value = null;
            return false;
        }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            if (Container != null)
            {
                dictionary = Container.ToDictionary(includeAutoSelected: false);
            }

            // The user asked for all the structural properties on this instance.
            if (Instance != null)
            {
                foreach (IEdmStructuralProperty property in GetEdmType().AsStructured().StructuralProperties())
                {
                    object propertyValue;
                    if (TryGetPropertyValue(property.Name, out propertyValue))
                    {
                        dictionary[property.Name] = propertyValue;
                    }
                }
            }

            return dictionary;
        }

        private Type GetElementType()
        {
            return Instance == null ? typeof(TElement) : Instance.GetType();
        }

        private IEdmModel GetModel()
        {
            Contract.Assert(ModelID != null);

            return ModelContainer.GetModel(ModelID);
        }
    }
}
