﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http.Properties;
using System.Web.Http.Services;

namespace System.Web.Http.Tracing.Tracers
{
    /// <summary>
    /// Tracer to monitor <see cref="MediaTypeFormatter"/> instances.
    /// </summary>
    internal class MediaTypeFormatterTracer : MediaTypeFormatter, IFormatterTracer, IDecorator<MediaTypeFormatter>
    {
        private const string ReadFromStreamAsyncMethodName = "ReadFromStreamAsync";
        private const string WriteToStreamAsyncMethodName = "WriteToStreamAsync";
        private const string GetPerRequestFormatterInstanceMethodName = "GetPerRequestFormatterInstance";

        private readonly MediaTypeFormatter _inner;

        public MediaTypeFormatterTracer(MediaTypeFormatter innerFormatter, ITraceWriter traceWriter, HttpRequestMessage request)
        {
            Contract.Assert(innerFormatter != null);
            Contract.Assert(traceWriter != null);

            InnerFormatter = innerFormatter;
            TraceWriter = traceWriter;
            Request = request;
            _inner = innerFormatter;

            // copy all non-overridable members from inner formatter
            CopyNonOverriableMembersFromInner(this);
        }

        public MediaTypeFormatter Inner
        {
            get { return _inner; }
        }

        public MediaTypeFormatter InnerFormatter { get; private set; }

        public ITraceWriter TraceWriter { get; set; }

        public HttpRequestMessage Request { get; set; }

        public static MediaTypeFormatter ActualMediaTypeFormatter(MediaTypeFormatter formatter)
        {
            IFormatterTracer tracer = formatter as IFormatterTracer;
            return tracer == null ? formatter : tracer.InnerFormatter;
        }

        public static MediaTypeFormatter CreateTracer(MediaTypeFormatter formatter, ITraceWriter traceWriter, HttpRequestMessage request)
        {
            // If we have been asked to wrap a tracer around a formatter, it could be
            // already wrapped, and there is nothing to do.  But if we see it is a tracer
            // that is not associated with a request, we wrap it into a new tracer that
            // does have a request.  The only formatter tracers without requests are the
            // ones in the default MediaTypeFormatterCollection in the HttpConfiguration.
            IFormatterTracer formatterTracer = formatter as IFormatterTracer;
            if (formatterTracer != null)
            {
                if (formatterTracer.Request == request)
                {
                    return formatter;
                }

                formatter = formatterTracer.InnerFormatter;
            }

            MediaTypeFormatter tracer = null;

            XmlMediaTypeFormatter xmlFormatter = formatter as XmlMediaTypeFormatter;
            JsonMediaTypeFormatter jsonFormatter = formatter as JsonMediaTypeFormatter;
            FormUrlEncodedMediaTypeFormatter formUrlFormatter = formatter as FormUrlEncodedMediaTypeFormatter;
            BufferedMediaTypeFormatter bufferedFormatter = formatter as BufferedMediaTypeFormatter;

            // We special-case Xml, Json and FormUrlEncoded formatters because we expect to be able
            // to find them with IsAssignableFrom in the MediaTypeFormatterCollection.
            if (xmlFormatter != null)
            {
                tracer = new XmlMediaTypeFormatterTracer(xmlFormatter, traceWriter, request);
            }
            else if (jsonFormatter != null)
            {
                tracer = new JsonMediaTypeFormatterTracer(jsonFormatter, traceWriter, request);
            }
            else if (formUrlFormatter != null)
            {
                tracer = new FormUrlEncodedMediaTypeFormatterTracer(formUrlFormatter, traceWriter, request);
            }
            else if (bufferedFormatter != null)
            {
                tracer = new BufferedMediaTypeFormatterTracer(bufferedFormatter, traceWriter, request);
            }
            else
            {
                tracer = new MediaTypeFormatterTracer(formatter, traceWriter, request);
            }

            return tracer;
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            MediaTypeFormatter formatter = null;

            TraceWriter.TraceBeginEnd(
                request,
                TraceCategories.FormattingCategory,
                TraceLevel.Info,
                InnerFormatter.GetType().Name,
                GetPerRequestFormatterInstanceMethodName,
                beginTrace: (tr) =>
                {
                    tr.Message = Error.Format(
                        SRResources.TraceGetPerRequestFormatterMessage,
                        InnerFormatter.GetType().Name,
                        type.Name,
                        mediaType);
                },
                execute: () => { formatter = InnerFormatter.GetPerRequestFormatterInstance(type, request, mediaType); },
                endTrace: (tr) =>
                {
                    if (formatter == null)
                    {
                        tr.Message = SRResources.TraceGetPerRequestNullFormatterEndMessage;
                    }
                    else
                    {
                        string formatMessage =
                            Object.ReferenceEquals(MediaTypeFormatterTracer.ActualMediaTypeFormatter(formatter),
                                                   InnerFormatter)
                                ? SRResources.TraceGetPerRequestFormatterEndMessage
                                : SRResources.TraceGetPerRequestFormatterEndMessageNew;

                        tr.Message = Error.Format(formatMessage, formatter.GetType().Name);
                    }
                },
                errorTrace: null);

            if (formatter != null && !(formatter is IFormatterTracer))
            {
                formatter = MediaTypeFormatterTracer.CreateTracer(formatter, TraceWriter, request);
            }

            return formatter;
        }

        public override bool CanReadType(Type type)
        {
            return InnerFormatter.CanReadType(type);
        }

        public override bool CanWriteType(Type type)
        {
            return InnerFormatter.CanWriteType(type);
        }

        public override bool Equals(object obj)
        {
            return InnerFormatter.Equals(obj);
        }

        public override int GetHashCode()
        {
            return InnerFormatter.GetHashCode();
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            InnerFormatter.SetDefaultContentHeaders(type, headers, mediaType);
        }

        public override string ToString()
        {
            return InnerFormatter.ToString();
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            HttpContentHeaders contentHeaders = content == null ? null : content.Headers;
            MediaTypeHeaderValue contentType = contentHeaders == null ? null : contentHeaders.ContentType;

            return TraceWriter.TraceBeginEndAsync<object>(
                Request,
                TraceCategories.FormattingCategory,
                TraceLevel.Info,
                InnerFormatter.GetType().Name,
                ReadFromStreamAsyncMethodName,
                beginTrace: (tr) =>
                {
                    tr.Message = Error.Format(
                                        SRResources.TraceReadFromStreamMessage,
                                        type.Name,
                                        contentType == null ? SRResources.TraceNoneObjectMessage : contentType.ToString());
                },

                execute: () => InnerFormatter.ReadFromStreamAsync(type, readStream, content, formatterLogger),

                endTrace: (tr, value) =>
                {
                    tr.Message = Error.Format(
                                        SRResources.TraceReadFromStreamValueMessage,
                                        FormattingUtilities.ValueToString(value, CultureInfo.CurrentCulture));
                },

                errorTrace: null);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            HttpContentHeaders contentHeaders = content == null ? null : content.Headers;
            MediaTypeHeaderValue contentType = contentHeaders == null
                                       ? null
                                       : contentHeaders.ContentType;

            return TraceWriter.TraceBeginEndAsync(
                Request,
                TraceCategories.FormattingCategory,
                TraceLevel.Info,
                InnerFormatter.GetType().Name,
                WriteToStreamAsyncMethodName,
                beginTrace: (tr) =>
                {
                    tr.Message = Error.Format(
                                        SRResources.TraceWriteToStreamMessage,
                                        FormattingUtilities.ValueToString(value, CultureInfo.CurrentCulture),
                                        type.Name,
                                        contentType == null ? SRResources.TraceNoneObjectMessage : contentType.ToString());
                },
                execute: () => InnerFormatter.WriteToStreamAsync(type, value, writeStream, content, transportContext),
                endTrace: null,
                errorTrace: null);
        }

        public void CopyNonOverriableMembersFromInner(MediaTypeFormatter toFormatter)
        {
            toFormatter.SupportedMediaTypes.Clear();
            foreach (var mediaType in InnerFormatter.SupportedMediaTypes)
            {
                toFormatter.SupportedMediaTypes.Add(mediaType);
            }

            toFormatter.SupportedEncodings.Clear();
            foreach (var encoding in InnerFormatter.SupportedEncodings)
            {
                toFormatter.SupportedEncodings.Add(encoding);
            }

            toFormatter.MediaTypeMappings.Clear();
            foreach (var mapping in InnerFormatter.MediaTypeMappings)
            {
                toFormatter.MediaTypeMappings.Add(mapping);
            }

            toFormatter.RequiredMemberSelector = InnerFormatter.RequiredMemberSelector;
        }
    }
}
