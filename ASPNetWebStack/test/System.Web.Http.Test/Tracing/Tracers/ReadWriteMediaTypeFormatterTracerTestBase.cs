﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Tracing.Tracers
{
    // This abstract base for MediaTypeFormatterTracers extends the core tests
    // by adding read/write tests.  BufferedMediaTypeFormatter sealed its async
    // read and write methods and they cannot be overridden by mocks.  This
    // abstract base is used for all MediaTypeFormatters whose read/write methods
    // can be overridden.
    internal abstract class ReadWriteMediaTypeFormatterTracerTestBase<TFormatter, TTracer> : MediaTypeFormatterTracerTestBase<TFormatter, TTracer>
        where TFormatter : MediaTypeFormatter
        where TTracer : MediaTypeFormatter
    {
        [Fact]
        public void ReadFromStreamAsync_Traces()
        {
            // Arrange
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            mockFormatter.Setup(f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContent>(), It.IsAny<IFormatterLogger>()))
                .Returns(TaskHelpers.FromResult<object>("sampleValue"));
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);

            TraceRecord[] expectedTraces = new TraceRecord[]
                {
                    new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) 
                        { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                    new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) 
                        { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
                };

            // Act
            Task<object> task = tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content, null);
            string result = task.Result as string;

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Equal("sampleValue", result);
        }

        [Fact]
        public void ReadFromStreamAsync_Traces_And_Throws_When_Inner_Throws()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            mockFormatter.Setup(
                f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContent>(), It.IsAny<IFormatterLogger>())).Throws(exception);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) 
                    { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error) 
                    { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
            };

            // Act
            Exception thrown = Assert.Throws<InvalidOperationException>(() => tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content, null));

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void ReadFromStreamAsync_Traces_And_Faults_When_Inner_Faults()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(exception);

            mockFormatter.Setup(f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContent>(), It.IsAny<IFormatterLogger>()))
                .Returns(tcs.Task);
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info)
                    { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error)
                    { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
            };

            // Act
            Task<object> task = tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content, null);

            // Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => task.Wait());
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void WriteToStreamAsync_Traces()
        {
            // Arrange
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            mockFormatter.Setup(f => f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(), It.IsAny<HttpContent>(), It.IsAny<TransportContext>()))
                .Returns(TaskHelpers.Completed());
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info)
                    { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info)
                    { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Task task = tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content, transportContext: null);
            task.Wait();

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
        }

        [Fact]
        public void WriteToStreamAsync_Traces_And_Throws_When_Inner_Throws()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            mockFormatter.Setup(f => f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(),
                                                         It.IsAny<HttpContent>(), It.IsAny<TransportContext>()))
                .Throws(exception);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info)
                    { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error)
                    { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Exception thrown = Assert.Throws<InvalidOperationException>(() => tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content, transportContext: null));

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void WriteToStreamAsync_Traces_And_Faults_When_Inner_Faults()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<TFormatter> mockFormatter = new Mock<TFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(exception);

            mockFormatter.Setup(f => f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(), It.IsAny<HttpContent>(), It.IsAny<TransportContext>()))
                .Returns(tcs.Task);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatter tracer = CreateTracer(mockFormatter.Object, request, traceWriter);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info)
                    { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error)
                    { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Task task = tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content, transportContext: null);

            // Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => task.Wait());
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }
    }
}
