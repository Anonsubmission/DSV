﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Builder;
using Microsoft.Data.Edm;

namespace System.Web.Http
{
    internal static class HttpActionDescriptorExtensions
    {
        internal const string EdmModelKey = "MS_EdmModel";

        internal static IEdmModel GetEdmModel(this HttpActionDescriptor actionDescriptor, Type entityClrType)
        {
            if (actionDescriptor == null)
            {
                throw Error.ArgumentNull("actionDescriptor");
            }

            if (entityClrType == null)
            {
                throw Error.ArgumentNull("entityClrType");
            }

            // save the EdmModel to the action descriptor
            return actionDescriptor.Properties.GetOrAdd(EdmModelKey + entityClrType.FullName, _ =>
                    {
                        ODataConventionModelBuilder builder = new ODataConventionModelBuilder(actionDescriptor.Configuration, isQueryCompositionMode: true);
                        EntityTypeConfiguration entityTypeConfiguration = builder.AddEntity(entityClrType);
                        builder.AddEntitySet(entityClrType.Name, entityTypeConfiguration);
                        IEdmModel edmModel = builder.GetEdmModel();
                        Contract.Assert(edmModel != null);
                        return edmModel;
                    }) as IEdmModel;
        }
    }
}
