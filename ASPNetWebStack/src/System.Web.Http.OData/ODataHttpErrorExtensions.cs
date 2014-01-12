﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Data.OData;

namespace System.Web.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpError"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ODataHttpErrorExtensions
    {
        /// <summary>
        /// Converts the <paramref name="httpError"/> to an <see cref="ODataError"/>.
        /// </summary>
        /// <param name="httpError">The <see cref="HttpError"/> instance to convert.</param>
        /// <returns>The converted <see cref="ODataError"/></returns>
        public static ODataError ToODataError(this HttpError httpError)
        {
            if (httpError == null)
            {
                throw Error.ArgumentNull("httpError");
            }

            return new ODataError()
            {
                Message = httpError.GetPropertyValue<string>(HttpErrorKeys.MessageKey),
                MessageLanguage = httpError.GetPropertyValue<string>(HttpErrorKeys.MessageLanguageKey),
                ErrorCode = httpError.GetPropertyValue<string>(HttpErrorKeys.ErrorCodeKey),
                InnerError = httpError.ToODataInnerError()
            };
        }

        private static ODataInnerError ToODataInnerError(this HttpError httpError)
        {
            string innerErrorMessage = httpError.GetPropertyValue<string>(HttpErrorKeys.ExceptionMessageKey);
            if (innerErrorMessage == null)
            {
                string messageDetail = httpError.GetPropertyValue<string>(HttpErrorKeys.MessageDetailKey);
                if (messageDetail == null)
                {
                    HttpError modelStateError = httpError.GetPropertyValue<HttpError>(HttpErrorKeys.ModelStateKey);
                    return modelStateError == null ? null : new ODataInnerError { Message = ConvertModelStateErrors(modelStateError) };
                }
                else
                {
                    return new ODataInnerError() { Message = messageDetail };
                }
            }
            else
            {
                ODataInnerError innerError = new ODataInnerError();
                innerError.Message = innerErrorMessage;
                innerError.TypeName = httpError.GetPropertyValue<string>(HttpErrorKeys.ExceptionTypeKey);
                innerError.StackTrace = httpError.GetPropertyValue<string>(HttpErrorKeys.StackTraceKey);
                HttpError innerExceptionError = httpError.GetPropertyValue<HttpError>(HttpErrorKeys.InnerExceptionKey);
                if (innerExceptionError != null)
                {
                    innerError.InnerError = innerExceptionError.ToODataInnerError();
                }
                return innerError;
            }
        }

        // Convert the model state errors in to a string (for debugging only).
        // This should be improved once ODataError allows more details.
        private static string ConvertModelStateErrors(HttpError error)
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, object> modelStateError in error)
            {
                if (modelStateError.Value != null)
                {
                    builder.Append(modelStateError.Key);
                    builder.Append(" : ");

                    IEnumerable<string> errorMessages = modelStateError.Value as IEnumerable<string>;
                    if (errorMessages != null)
                    {
                        foreach (string errorMessage in errorMessages)
                        {
                            builder.AppendLine(errorMessage);
                        }
                    }
                    else
                    {
                        builder.AppendLine(modelStateError.Value.ToString());
                    }
                }
            }

            return builder.ToString();
        }
    }
}
