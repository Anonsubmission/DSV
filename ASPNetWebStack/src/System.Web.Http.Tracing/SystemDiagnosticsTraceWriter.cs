﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Tracing.Properties;

namespace System.Web.Http.Tracing
{
    /// <summary>
    /// Implementation of <see cref="ITraceWriter"/> that traces to <see cref="System.Diagnostics.Trace"/>
    /// </summary>
    public class SystemDiagnosticsTraceWriter : ITraceWriter
    {
        // Duplicate of internal category name traced by WebApi for start/end of request
        private const string SystemWebHttpRequestCategory = "System.Web.Http.Request";

        // Duplicates of internal constants in HttpError
        private const string MessageKey = "Message";
        private const string MessageDetailKey = "MessageDetail";
        private const string ModelStateKey = "ModelState";
        private const string ExceptionMessageKey = "ExceptionMessage";
        private const string ExceptionTypeKey = "ExceptionType";
        private const string StackTraceKey = "StackTrace";
        private const string InnerExceptionKey = "InnerException";

        private static readonly TraceEventType[] TraceLevelToTraceEventType = new TraceEventType[]
        {
            // TraceLevel.Off
            (TraceEventType)0,

            // TraceLevel.Debug
            TraceEventType.Verbose,

            // TraceLevel.Info
            TraceEventType.Information,

            // TraceLevel.Warn
            TraceEventType.Warning,

            // TraceLevel.Error
            TraceEventType.Error,

            // TraceLevel.Fatal
            TraceEventType.Critical
        };

        private TraceLevel _minLevel = TraceLevel.Info;

        /// <summary>
        /// Gets or sets the minimum trace level.
        /// </summary>
        /// <value>
        /// Any <see cref="System.Web.Http.Tracing.TraceLevel"/> below this
        /// level will be ignored. The default for this property
        /// is <see cref="TraceLevel.Info"/>.
        /// </value>
        public TraceLevel MinimumLevel
        {
            get
            {
                return _minLevel;
            }
            set
            {
                if (value < TraceLevel.Off || value > TraceLevel.Fatal)
                {
                    throw Error.ArgumentOutOfRange("value", 
                                                    value, 
                                                    SRResources.TraceLevelOutOfRange);
                }

                _minLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the formatted message
        /// should be the verbose format, meaning it displays all fields
        /// of the <see cref="TraceRecord"/>.
        /// </summary>
        /// <value><c>true</c> means all <see cref="TraceRecord"/> fields
        /// will be traced, <c>false</c> means only minimal information
        /// will be traced. The default value is <c>false</c>.</value>
        public bool IsVerbose { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TraceSource"/> to which the
        /// traces will be sent.
        /// </summary>
        /// <value>
        /// This property allows a custom <see cref="TraceSource"/> 
        /// to be used when writing the traces.
        /// This allows an application to configure and use its
        /// own <see cref="TraceSource"/> other than the default
        /// <see cref="System.Diagnostics.Trace"/>.
        /// If the value is <c>null</c>, this trace writer will
        /// send traces to <see cref="System.Diagnostics.Trace"/>.
        /// </value>
        public TraceSource TraceSource { get; set; }

        /// <summary>
        /// Writes a trace to <see cref="System.Diagnostics.Trace"/> if the
        /// <paramref name="level"/> is greater than or equal <see cref="MinimumLevel"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> associated with this trace. 
        /// It may be <c>null</c> but the resulting trace will contain no correlation ID.</param>
        /// <param name="category">The category for the trace. This can be any user-defined
        /// value. It is not interpreted by this implementation but is written to the trace.</param>
        /// <param name="level">The <see cref="TraceLevel"/> of this trace. If it is less than
        /// <see cref="MinimumLevel"/>, this trace request will be ignored.</param>
        /// <param name="traceAction">The user callback to invoke to fill in a <see cref="TraceRecord"/>
        /// with additional information to add to the trace.</param>
        public virtual void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
        {
            if (category == null)
            {
                throw Error.ArgumentNull("category");
            }

            if (traceAction == null)
            {
                throw Error.ArgumentNull("traceAction");
            }

            if (level < TraceLevel.Off || level > TraceLevel.Fatal)
            {
                throw Error.ArgumentOutOfRange("level",
                                                level,
                                                SRResources.TraceLevelOutOfRange);
            }

            if (MinimumLevel == TraceLevel.Off || level < MinimumLevel)
            {
                return;
            }

            TraceRecord traceRecord = new TraceRecord(request, category, level);
            traceAction(traceRecord);
            TranslateHttpResponseException(traceRecord);
            string message = Format(traceRecord);
            if (!String.IsNullOrEmpty(message))
            {
                // Level may have changed in Translate above
                TraceMessage(traceRecord.Level, message);
            }
        }

        /// <summary>
        /// Formats the contents of the given <see cref="TraceRecord"/> into
        /// a single string containing comma-separated name-value pairs
        /// for each <see cref="TraceRecord"/> property.
        /// </summary>
        /// <param name="traceRecord">The <see cref="TraceRecord"/> from which 
        /// to produce the result.</param>
        /// <returns>A string containing comma-separated name-value pairs.</returns>
        public virtual string Format(TraceRecord traceRecord)
        {
            if (traceRecord == null)
            {
                throw Error.ArgumentNull("traceRecord");
            }

            // The first and last traces are injected by the tracing system itself.
            // We use these to format unique strings identifying the incoming request
            // and the outgoing response.
            if (String.Equals(traceRecord.Category, SystemWebHttpRequestCategory, StringComparison.Ordinal))
            {
                return FormatRequestEnvelope(traceRecord);
            }

            List<string> messages = new List<string>();

            if (!IsVerbose)
            {
                // In short format mode, we trace only End traces because it is
                // where the results of each operation will appear.
                if (traceRecord.Kind == TraceKind.Begin)
                {
                    return null;
                }
            }
            else
            {
                messages.Add(Error.Format(SRResources.TimeLevelKindFormat,
                                            FormatDateTime(traceRecord.Timestamp),
                                            traceRecord.Level.ToString(),
                                            traceRecord.Kind.ToString()));

                if (!String.IsNullOrEmpty(traceRecord.Category))
                {
                    messages.Add(Error.Format(SRResources.CategoryFormat, traceRecord.Category));
                }

                messages.Add(Error.Format(SRResources.IdFormat, traceRecord.RequestId.ToString()));
            }

            if (!String.IsNullOrEmpty(traceRecord.Message))
            {
                messages.Add(Error.Format(SRResources.MessageFormat, traceRecord.Message));
            }

            if (traceRecord.Operator != null || traceRecord.Operation != null)
            {
                messages.Add(Error.Format(SRResources.OperationFormat, traceRecord.Operator, traceRecord.Operation));
            }

            if (traceRecord.Status != 0)
            {
                messages.Add(Error.Format(SRResources.HttpStatusFormat, (int)traceRecord.Status, traceRecord.Status.ToString()));
            }

            if (traceRecord.Exception != null)
            {
                messages.Add(Error.Format(SRResources.ExceptionFormat, traceRecord.Exception.ToString()));
            }

            return String.Join(", ", messages);
        }

        /// <summary>
        /// Formats the given <see cref="TraceRecord"/> into a string describing
        /// either the initial receipt of the incoming request or the final send
        /// of the response, depending on <see cref="TraceKind"/>.
        /// </summary>
        /// <param name="traceRecord">The <see cref="TraceRecord"/> from which to 
        /// produce the result.</param>
        /// <returns>A string containing comma-separated name-value pairs.</returns>
        public virtual string FormatRequestEnvelope(TraceRecord traceRecord)
        {
            if (traceRecord == null)
            {
                throw Error.ArgumentNull("traceRecord");
            }

            List<string> messages = new List<string>();

            if (IsVerbose)
            {
                messages.Add(Error.Format((traceRecord.Kind == TraceKind.Begin)
                                                ? SRResources.TimeRequestFormat
                                                : SRResources.TimeResponseFormat,
                                            FormatDateTime(traceRecord.Timestamp)));
            }
            else
            {
                messages.Add((traceRecord.Kind == TraceKind.Begin)
                                ? SRResources.ShortRequestFormat
                                : SRResources.ShortResponseFormat);
            }

            if (traceRecord.Status != 0)
            {
                messages.Add(Error.Format(SRResources.HttpStatusFormat, (int)traceRecord.Status, traceRecord.Status.ToString()));
            }

            if (traceRecord.Request != null)
            {
                messages.Add(Error.Format(SRResources.HttpMethodFormat, traceRecord.Request.Method));

                if (traceRecord.Request.RequestUri != null)
                {
                    messages.Add(Error.Format(SRResources.UrlFormat, traceRecord.Request.RequestUri.ToString()));
                }
            }

            if (IsVerbose)
            {
                messages.Add(Error.Format(SRResources.IdFormat, traceRecord.RequestId.ToString()));
            }

            // The Message and Exception fields do not contain interesting information unless
            // there is a problem, so they appear after the more informative trace information.
            if (!String.IsNullOrEmpty(traceRecord.Message))
            {
                messages.Add(Error.Format(SRResources.MessageFormat, traceRecord.Message));
            }

            if (traceRecord.Exception != null)
            {
                messages.Add(Error.Format(SRResources.ExceptionFormat, traceRecord.Exception.ToString()));
            }

            return String.Join(", ", messages);
        }

        /// <summary>
        /// Examines the given <see cref="TraceRecord"/> to determine whether it
        /// contains an <see cref="HttpResponseException"/> and if so, modifies
        /// the <see cref="TraceRecord"/> to capture more detailed information.
        /// </summary>
        /// <param name="traceRecord">The <see cref="TraceRecord"/> to examine and modify.</param>
        public virtual void TranslateHttpResponseException(TraceRecord traceRecord)
        {
            if (traceRecord == null)
            {
                throw Error.ArgumentNull("traceRecord");
            }

            var httpResponseException = ExtractHttpResponseException(traceRecord);

            if (httpResponseException == null)
            {
                return;
            }

            HttpResponseMessage response = httpResponseException.Response;
            Contract.Assert(response != null);

            // If the status has been set already, do not overwrite it,
            // otherwise propagate the status into the record.
            if (traceRecord.Status == 0)
            {
                traceRecord.Status = response.StatusCode;
            }

            // Client level errors are downgraded to TraceLevel.Warn
            if ((int)response.StatusCode < (int)HttpStatusCode.InternalServerError)
            {
                traceRecord.Level = TraceLevel.Warn;
            }

            // Non errors are downgraded to TraceLevel.Info
            if ((int)response.StatusCode < (int)HttpStatusCode.BadRequest)
            {
                traceRecord.Level = TraceLevel.Info;
            }

            // HttpResponseExceptions often contain HttpError instances that carry
            // detailed information that may be filtered out by IncludeErrorDetailPolicy
            // before reaching the client. Capture it here for the trace.
            ObjectContent objectContent = response.Content as ObjectContent;
            if (objectContent == null)
            {
                return;
            }

            HttpError httpError = objectContent.Value as HttpError;
            if (httpError == null)
            {
                return;
            }

            object messageObject = null;
            object messageDetailsObject = null;

            List<string> messages = new List<string>();

            if (httpError.TryGetValue(MessageKey, out messageObject))
            {
                messages.Add(Error.Format(SRResources.HttpErrorUserMessageFormat, messageObject));
            }

            if (httpError.TryGetValue(MessageDetailKey, out messageDetailsObject))
            {
                messages.Add(Error.Format(SRResources.HttpErrorMessageDetailFormat, messageDetailsObject));
            }

            // Extract the exception from this HttpError and then incrementally
            // walk down all inner exceptions.
            AddExceptions(httpError, messages);

            // ModelState errors are handled with a nested HttpError
            object modelStateErrorObject = null;
            if (httpError.TryGetValue(ModelStateKey, out modelStateErrorObject))
            {
                HttpError modelStateError = modelStateErrorObject as HttpError;
                if (modelStateError != null)
                {
                    messages.Add(FormatModelStateErrors(modelStateError));
                }
            }

            traceRecord.Message = String.Join(", ", messages);
        }

        /// <summary>
        /// Formats a <see cref="DateTime"/> for the trace.
        /// </summary>
        /// <remarks>
        /// The default implementation uses the ISO 8601 convention
        /// for round-trippable dates so they can be parsed.
        /// </remarks>
        /// <param name="dateTime">The <see cref="DateTime"/></param>
        /// <returns>The <see cref="DateTime"/> formatted as a string</returns>
        public virtual string FormatDateTime(DateTime dateTime)
        {
            // The 'o' format is ISO 8601 for a round-trippable DateTime.
            // It is culture-invariant and can be parsed.
            return dateTime.ToString("o", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Unpacks any exceptions in the given <see cref="HttpError"/> and adds
        /// them into a collection of name-value pairs that can be composed into a single string.
        /// </summary>
        /// <remarks>
        /// This helper also iterates over all inner exceptions and unpacks them too.
        /// </remarks>
        /// <param name="httpError">The <see cref="HttpError"/> to unpack.</param>
        /// <param name="messages">A collection of messages to which the new information should be added.</param>
        private static void AddExceptions(HttpError httpError, List<string> messages)
        {
            Contract.Assert(httpError != null);
            Contract.Assert(messages != null);

            object exceptionMessageObject = null;
            object exceptionTypeObject = null;
            object stackTraceObject = null;
            object innerExceptionObject = null;

            for (int i = 0; httpError != null; i++)
            {
                // For uniqueness, key names append the depth of inner exception
                string indexText = i == 0 ? String.Empty : Error.Format("[{0}]", i);

                if (httpError.TryGetValue(ExceptionTypeKey, out exceptionTypeObject))
                {
                    messages.Add(Error.Format(SRResources.HttpErrorExceptionTypeFormat, indexText, exceptionTypeObject));
                }

                if (httpError.TryGetValue(ExceptionMessageKey, out exceptionMessageObject))
                {
                    messages.Add(Error.Format(SRResources.HttpErrorExceptionMessageFormat, indexText, exceptionMessageObject));
                }

                if (httpError.TryGetValue(StackTraceKey, out stackTraceObject))
                {
                    messages.Add(Error.Format(SRResources.HttpErrorStackTraceFormat, indexText, stackTraceObject));
                }

                if (!httpError.TryGetValue(InnerExceptionKey, out innerExceptionObject))
                {
                    break;
                }

                Contract.Assert(!Object.ReferenceEquals(httpError, innerExceptionObject));

                httpError = innerExceptionObject as HttpError;
            }
        }

        private static HttpResponseException ExtractHttpResponseException(TraceRecord traceRecord)
        {
            return ExtractHttpResponseException(traceRecord.Exception);
        }

        private static HttpResponseException ExtractHttpResponseException(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            var httpResponseException = exception as HttpResponseException;
            if (httpResponseException != null)
            {
                return httpResponseException;
            }

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                httpResponseException = aggregateException
                    .Flatten()
                    .InnerExceptions
                    .Select(ExtractHttpResponseException)
                    .Where(ex => ex != null && ex.Response != null)
                    .OrderByDescending(ex => ex.Response.StatusCode)
                    .FirstOrDefault();
                return httpResponseException;
            }

            return ExtractHttpResponseException(exception.InnerException);
        }

        private static string FormatModelStateErrors(HttpError modelStateError)
        {
            Contract.Assert(modelStateError != null);

            List<string> messages = new List<string>();
            foreach (var pair in modelStateError)
            {
                IEnumerable<string> errorList = pair.Value as IEnumerable<string>;
                if (errorList != null)
                {
                    messages.Add(Error.Format(SRResources.HttpErrorModelStatePairFormat, pair.Key, String.Join(", ", errorList)));
                }
            }

            return Error.Format(SRResources.HttpErrorModelStateErrorFormat, String.Join(", ", messages));
        }

        private void TraceMessage(TraceLevel level, string message)
        {
            Contract.Assert(level >= TraceLevel.Off && level <= TraceLevel.Fatal);

            // If the user registered a custom TraceSource, we write a trace event to it
            // directly, preserving the event type.
            TraceSource traceSource = TraceSource;
            if (traceSource != null)
            {
                traceSource.TraceEvent(eventType: TraceLevelToTraceEventType[(int)level], id: 0, message: message);
                return;
            }

            // If there is no custom TraceSource, trace to System.Diagnostics.Trace.
            // But System.Diagnostics.Trace does not offer a public API to trace
            // TraceEventType.Verbose or TraceEventType.Critical, meaning our
            // TraceLevel.Debug and TraceLevel.Fatal cannot be report directly.
            // Consequently, we translate Verbose to Trace.WriteLine and
            // Critical to TraceEventType.Error.
            // Windows Azure Diagnostics' TraceListener already translates
            // a WriteLine to a Verbose, so on Azure the Debug trace will be
            // handled properly.
            switch (level)
            {
                case TraceLevel.Off:
                    return;

                case TraceLevel.Debug:
                    System.Diagnostics.Trace.WriteLine(message);
                    return;

                case TraceLevel.Info:
                    System.Diagnostics.Trace.TraceInformation(message);
                    return;

                case TraceLevel.Warn:
                    System.Diagnostics.Trace.TraceWarning(message);
                    return;

                case TraceLevel.Error:
                case TraceLevel.Fatal:
                    System.Diagnostics.Trace.TraceError(message);
                    return;
            }
        }
    }
}
