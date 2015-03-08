﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.ModelBinding.Binders;
using System.Web.Http.Properties;
using System.Web.Http.Validation;
using System.Web.Http.Validation.Providers;
using System.Web.Http.ValueProviders;
using System.Web.Http.ValueProviders.Providers;

namespace System.Web.Http.ModelBinding
{
    public static class FormDataCollectionExtensions
    {
        // This is a helper method to use Model Binding over a JQuery syntax. 
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys
        // x[] --> x
        // [] --> ""
        // x[field]  --> x.field, where field is not a number
        internal static string NormalizeJQueryToMvc(string key)
        {
            if (key == null)
            {
                return String.Empty;
            }

            StringBuilder sb = null;
            int i = 0;
            while (true)
            {
                int indexOpen = key.IndexOf('[', i);
                if (indexOpen < 0)
                {
                    // Fast path, no normalization needed.
                    // This skips the string conversion and allocating the string builder.
                    if (i == 0)
                    {
                        return key;
                    }
                    sb = sb ?? new StringBuilder();
                    sb.Append(key, i, key.Length - i);
                    break; // no more brackets
                }

                sb = sb ?? new StringBuilder();
                sb.Append(key, i, indexOpen - i); // everything up to "["

                // Find closing bracket.
                int indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw Error.Argument("key", SRResources.JQuerySyntaxMissingClosingBracket);
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty bracket. Signifies array. Just remove. 
                }
                else
                {
                    if (Char.IsDigit(key[indexOpen + 1]))
                    {
                        // array index. Leave unchanged. 
                        sb.Append(key, indexOpen, indexClose - indexOpen + 1);
                    }
                    else
                    {
                        // Field name.  Convert to dot notation. 
                        sb.Append('.');
                        sb.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                    }
                }

                i = indexClose + 1;
                if (i >= key.Length)
                {
                    break; // end of string
                }
            }
            return sb.ToString();
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetJQueryNameValuePairs(this FormDataCollection formData)
        {
            if (formData == null)
            {
                throw Error.ArgumentNull("formData");
            }

            int count = 0;

            foreach (KeyValuePair<string, string> kv in formData)
            {
                ThrowIfMaxHttpCollectionKeysExceeded(count);

                string key = NormalizeJQueryToMvc(kv.Key);
                string value = kv.Value ?? String.Empty;
                yield return new KeyValuePair<string, string>(key, value);

                count++;
            }
        }

        private static void ThrowIfMaxHttpCollectionKeysExceeded(int count)
        {
            if (count >= MediaTypeFormatter.MaxHttpCollectionKeys)
            {
                throw Error.InvalidOperation(SRResources.MaxHttpCollectionKeyLimitReached, MediaTypeFormatter.MaxHttpCollectionKeys, typeof(MediaTypeFormatter));
            }
        }

        // Create a IValueProvider for the given form, assuming a JQuery syntax.
        internal static IValueProvider GetJQueryValueProvider(this FormDataCollection formData)
        {
            if (formData == null)
            {
                throw Error.ArgumentNull("formData");
            }

            IEnumerable<KeyValuePair<string, string>> nvc = formData.GetJQueryNameValuePairs();
            return new NameValuePairsValueProvider(nvc, CultureInfo.InvariantCulture);
        }

        public static T ReadAs<T>(this FormDataCollection formData)
        {
            return (T)ReadAs(formData, typeof(T));
        }

        public static T ReadAs<T>(this FormDataCollection formData, HttpActionContext actionContext)
        {
            return (T)ReadAs(formData, typeof(T), String.Empty, actionContext: actionContext);
        }

        public static object ReadAs(this FormDataCollection formData, Type type)
        {
            return ReadAs(formData, type, String.Empty, requiredMemberSelector: null, formatterLogger: null);
        }

        public static object ReadAs(this FormDataCollection formData, Type type, HttpActionContext actionContext)
        {
            return ReadAs(formData, type, String.Empty, actionContext);
        }

        public static T ReadAs<T>(this FormDataCollection formData, string modelName, IRequiredMemberSelector requiredMemberSelector, IFormatterLogger formatterLogger)
        {
            return (T)ReadAs(formData, typeof(T), modelName, requiredMemberSelector, formatterLogger);
        }

        public static T ReadAs<T>(this FormDataCollection formData, string modelName, HttpActionContext actionContext)
        {
            return (T)ReadAs(formData, typeof(T), modelName, actionContext);
        }

        public static object ReadAs(this FormDataCollection formData, Type type, string modelName, HttpActionContext actionContext)
        {
            if (formData == null)
            {
                throw Error.ArgumentNull("formData");
            }
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (actionContext == null)
            {
                throw Error.ArgumentNull("actionContext");
            }

            return ReadAsInternal(formData, type, modelName, actionContext);
        }

        /// <summary>
        /// Deserialize the form data to the given type, using model binding.  
        /// </summary>
        /// <param name="formData">collection with parsed form url data</param>
        /// <param name="type">target type to read as</param>
        /// <param name="modelName">null or empty to read the entire form as a single object. This is common for body data. 
        /// Or the name of a model to do a partial binding against the form data. This is common for extracting individual fields.</param>
        /// <param name="requiredMemberSelector">The <see cref="IRequiredMemberSelector"/> used to determine required members.</param>
        /// <param name="formatterLogger">The <see cref="IFormatterLogger"/> to log events to.</param>
        /// <returns>best attempt to bind the object. The best attempt may be null.</returns>
        public static object ReadAs(this FormDataCollection formData, Type type, string modelName, IRequiredMemberSelector requiredMemberSelector, IFormatterLogger formatterLogger)
        {
            if (formData == null)
            {
                throw Error.ArgumentNull("formData");
            }
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            using (HttpConfiguration config = new HttpConfiguration())
            {
                // The HttpActionContext provides access to configuration for ModelBinders, and is also provided
                // to the IModelBinder when binding occurs. Since an HttpActionContext was not provided, create a default.
                HttpActionContext actionContext = CreateActionContextForModelBinding(config);

                bool validateRequiredMembers = requiredMemberSelector != null && formatterLogger != null;
                if (validateRequiredMembers)
                {
                    // Set a ModelValidatorProvider that understands the IRequiredMemberSelector
                    config.Services.Replace(typeof(ModelValidatorProvider), new RequiredMemberModelValidatorProvider(requiredMemberSelector));
                }

                object result = ReadAs(formData, type, modelName, actionContext);

                // The model binding will log any errors to the HttpActionContext's ModelState. Since this is a context
                // that we created and doesn't map to a real action invocation, we want to forward the errors to 
                // the user-specified IFormatterLogger.
                if (formatterLogger != null)
                {
                    foreach (KeyValuePair<string, ModelState> modelStatePair in actionContext.ModelState)
                    {
                        foreach (ModelError modelError in modelStatePair.Value.Errors)
                        {
                            if (modelError.Exception != null)
                            {
                                formatterLogger.LogError(modelStatePair.Key, modelError.Exception);
                            }
                            else
                            {
                                formatterLogger.LogError(modelStatePair.Key, modelError.ErrorMessage);
                            }
                        }
                    }
                }

                return result;
            }
        }

        private static object ReadAsInternal(this FormDataCollection formData, Type type, string modelName, HttpActionContext actionContext)
        {
            Contract.Assert(formData != null);
            Contract.Assert(type != null);
            Contract.Assert(actionContext != null);

            IValueProvider valueProvider = formData.GetJQueryValueProvider();
            ModelBindingContext bindingContext = CreateModelBindingContext(actionContext, modelName ?? String.Empty, type, valueProvider);

            ModelBinderProvider modelBinderProvider = CreateModelBindingProvider(actionContext);

            IModelBinder modelBinder = modelBinderProvider.GetBinder(actionContext.ControllerContext.Configuration, type);
            bool haveResult = modelBinder.BindModel(actionContext, bindingContext);
            if (haveResult)
            {
                return bindingContext.Model;
            }

            return MediaTypeFormatter.GetDefaultValueForType(type);
        }

        // Helper for ReadAs() to get a ModelBinderProvider to read FormUrl data. 
        private static ModelBinderProvider CreateModelBindingProvider(HttpActionContext actionContext)
        {
            Contract.Assert(actionContext != null);

            ServicesContainer cs = actionContext.ControllerContext.Configuration.Services;
            IEnumerable<ModelBinderProvider> providers = cs.GetModelBinderProviders();
            ModelBinderProvider modelBinderProvider = new CompositeModelBinderProvider(providers);
            return modelBinderProvider;
        }

        // Helper for ReadAs() to get a ModelBindingContext to invoke model binding over FormUrl data. 
        private static ModelBindingContext CreateModelBindingContext(HttpActionContext actionContext, string modelName, Type type, IValueProvider vp)
        {
            Contract.Assert(actionContext != null);
            Contract.Assert(type != null);
            Contract.Assert(vp != null);

            ServicesContainer cs = actionContext.ControllerContext.Configuration.Services;
            ModelMetadataProvider metadataProvider = cs.GetModelMetadataProvider();

            ModelBindingContext ctx = new ModelBindingContext()
            {
                ModelName = modelName,
                FallbackToEmptyPrefix = false,
                ModelMetadata = metadataProvider.GetMetadataForType(null, type),
                ModelState = actionContext.ModelState,
                ValueProvider = vp
            };
            return ctx;
        }

        // Creates a default action context to invoke model binding
        private static HttpActionContext CreateActionContextForModelBinding(HttpConfiguration config)
        {
            Contract.Assert(config != null);

            HttpControllerContext controllerContext = new HttpControllerContext() { Configuration = config };
            controllerContext.ControllerDescriptor = new HttpControllerDescriptor(config);

            HttpActionContext actionContext = new HttpActionContext { ControllerContext = controllerContext };

            return actionContext;
        }
    }
}
