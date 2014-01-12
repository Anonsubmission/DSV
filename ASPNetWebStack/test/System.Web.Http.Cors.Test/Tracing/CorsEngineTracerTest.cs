﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Cors;
using System.Web.Http.Tracing;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Cors.Tracing
{
    public class CorsEngineTracerTest
    {
        [Fact]
        public void EvaluatePolicy_CallsInner()
        {
            Mock<ITraceWriter> traceWriterMock = new Mock<ITraceWriter>();
            Mock<ICorsEngine> corsEngineMock = new Mock<ICorsEngine>();
            bool innerIsCalled = false;
            corsEngineMock.Setup(engine => engine.EvaluatePolicy(It.IsAny<CorsRequestContext>(), It.IsAny<CorsPolicy>())).Returns(() =>
            {
                innerIsCalled = true;
                return new CorsResult();
            });
            CorsEngineTracer tracer = new CorsEngineTracer(corsEngineMock.Object, traceWriterMock.Object);

            tracer.EvaluatePolicy(new CorsRequestContext(), new CorsPolicy());

            Assert.True(innerIsCalled);
        }

        [Fact]
        public void EvaluatePolicy_EmitTraces()
        {
            TraceRecord beginTrace = null;
            TraceRecord endTrace = null;
            Mock<ITraceWriter> traceWriterMock = new Mock<ITraceWriter>();
            traceWriterMock
                .Setup(t => t.Trace(It.IsAny<HttpRequestMessage>(), It.IsAny<string>(), It.IsAny<TraceLevel>(), It.IsAny<Action<TraceRecord>>()))
                .Callback<HttpRequestMessage, string, TraceLevel, Action<TraceRecord>>((request, category, level, traceAction) =>
                {
                    TraceRecord traceRecord = new TraceRecord(request, category, level);
                    traceAction(traceRecord);
                    if (traceRecord.Kind == TraceKind.Begin)
                    {
                        beginTrace = traceRecord;
                    }
                    else if (traceRecord.Kind == TraceKind.End)
                    {
                        endTrace = traceRecord;
                    }
                });
            Mock<ICorsEngine> corsEngineMock = new Mock<ICorsEngine>();
            corsEngineMock
                .Setup(engine => engine.EvaluatePolicy(It.IsAny<CorsRequestContext>(), It.IsAny<CorsPolicy>()))
                .Returns(new CorsResult());
            CorsEngineTracer tracer = new CorsEngineTracer(corsEngineMock.Object, traceWriterMock.Object);

            tracer.EvaluatePolicy(new CorsRequestContext(), new CorsPolicy());

            Assert.NotNull(beginTrace);
            Assert.Equal(TraceCategories.CorsCategory, beginTrace.Category);
            Assert.Equal(TraceLevel.Info, beginTrace.Level);
            Assert.Equal("EvaluatePolicy", beginTrace.Operation);

            Assert.NotNull(endTrace);
            Assert.Equal(TraceCategories.CorsCategory, endTrace.Category);
            Assert.Equal(TraceLevel.Info, endTrace.Level);
            Assert.Equal("EvaluatePolicy", endTrace.Operation);
            Assert.Equal(
                @"CorsResult returned: 'IsValid: True, AllowCredentials: False, PreflightMaxAge: null, AllowOrigin: , AllowExposedHeaders: {}, AllowHeaders: {}, AllowMethods: {}, ErrorMessages: {}'",
                endTrace.Message);
        }
    }
}