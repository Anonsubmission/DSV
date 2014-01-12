﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Tracing.Tracers
{
    public class AuthenticationFilterTracerTests
    {
        [Fact]
        public void Inner_ReturnsSpecifiedInstance()
        {
            // Arrange
            IAuthenticationFilter expectedInner = CreateDummyFilter();
            ITraceWriter tracer = CreateDummyTracer();
            AuthenticationFilterTracer product = CreateProductUnderTest(expectedInner, tracer);

            // Act
            IAuthenticationFilter inner = product.Inner;

            // Assert
            Assert.Same(expectedInner, inner);
        }

        [Fact]
        public void AuthenticateAsync_DelegatesToInnerFilter()
        {
            // Arrange
            HttpAuthenticationContext expectedAuthenticationContext = CreateAuthenticationContext();
            CancellationToken expectedCancellationToken = CreateCancellationToken();
            Mock<IAuthenticationFilter> mock = new Mock<IAuthenticationFilter>();
            int calls = 0;
            mock.Setup(f => f.AuthenticateAsync(expectedAuthenticationContext, expectedCancellationToken)).Callback(
                () => { calls++; }).Returns(() => Task.FromResult<object>(null));
            IAuthenticationFilter filter = mock.Object;
            ITraceWriter tracer = CreateStubTracer();
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);

            // Act
            Task task = product.AuthenticateAsync(expectedAuthenticationContext, expectedCancellationToken);

            // Assert
            Assert.NotNull(task);
            task.Wait();
            Assert.Equal(1, calls);
        }

        [Fact]
        public void AuthenticateAsync_Traces()
        {
            // Arrange
            CancellationToken cancellationToken = CreateCancellationToken();
            IAuthenticationFilter filter = CreateStubFilter();
            TraceRecord record = null;
            ITraceWriter tracer = CreateTracer((r) => { record = r; });
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                HttpAuthenticationContext authenticationContext = CreateAuthenticationContext(expectedRequest);

                // Act
                Task task = product.AuthenticateAsync(authenticationContext, cancellationToken);

                // Assert
                Assert.NotNull(task);
                task.Wait();
                Assert.NotNull(record);
                Assert.Same(expectedRequest, record.Request);
                Assert.Same(TraceCategories.FiltersCategory, record.Category);
                Assert.Equal(TraceLevel.Info, record.Level);
                Assert.Equal(TraceKind.End, record.Kind);
                Assert.Equal(filter.GetType().Name, record.Operator);
                Assert.Equal("AuthenticateAsync", record.Operation);
                Assert.Null(record.Exception);
                Assert.Null(record.Message);
            }
        }

        [Fact]
        public void AuthenticateAsync_Traces_WhenContextIsNull()
        {
            // Arrange
            CancellationToken cancellationToken = CreateCancellationToken();
            IAuthenticationFilter filter = CreateStubFilter();
            TraceRecord record = null;
            ITraceWriter tracer = CreateTracer((r) => { record = r; });
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);
            HttpAuthenticationContext authenticationContext = null;

            // Act
            Task task = product.AuthenticateAsync(authenticationContext, cancellationToken);

            // Assert
            Assert.NotNull(task);
            task.Wait();
            Assert.NotNull(record);
            Assert.Null(record.Request);
        }

        [Fact]
        public void ChallengeAsync_DelegatesToInnerFilter()
        {
            // Arrange
            HttpAuthenticationChallengeContext expectedChallengeContext = CreateChallengeContext();
            CancellationToken expectedCancellationToken = CreateCancellationToken();
            IHttpActionResult expectedInnerResult = CreateDummyActionResult();
            IHttpActionResult expectedResult = CreateDummyActionResult();
            Mock<IAuthenticationFilter> mock = new Mock<IAuthenticationFilter>();
            int calls = 0;
            mock.Setup(f => f.ChallengeAsync(expectedChallengeContext, expectedCancellationToken))
                .Callback(() => { calls++; })
                .Returns(() => Task.FromResult(expectedResult));
            IAuthenticationFilter filter = mock.Object;
            ITraceWriter tracer = CreateStubTracer();
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);

            // Act
            Task task = product.ChallengeAsync(expectedChallengeContext, expectedCancellationToken);

            // Assert
            Assert.NotNull(task);
            task.Wait();
            Assert.Equal(1, calls);
        }

        [Fact]
        public void ChallengeAsync_Traces()
        {
            // Arrange
            CancellationToken cancellationToken = CreateCancellationToken();
            IAuthenticationFilter filter = CreateStubFilter();
            TraceRecord record = null;
            ITraceWriter tracer = CreateTracer((r) => { record = r; });
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);
            IHttpActionResult innerResult = CreateDummyActionResult();

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(expectedRequest, innerResult);

                // Act
                Task task = product.ChallengeAsync(context, cancellationToken);

                // Assert
                Assert.NotNull(task);
                task.Wait();
                Assert.NotNull(record);
                Assert.Same(expectedRequest, record.Request);
                Assert.Same(TraceCategories.FiltersCategory, record.Category);
                Assert.Equal(TraceLevel.Info, record.Level);
                Assert.Equal(TraceKind.End, record.Kind);
                Assert.Equal(filter.GetType().Name, record.Operator);
                Assert.Equal("ChallengeAsync", record.Operation);
                Assert.Null(record.Exception);
                Assert.Null(record.Message);
            }
        }

        [Fact]
        public void ChallengeAsync_Traces_WhenContextIsNull()
        {
            // Arrange
            CancellationToken cancellationToken = CreateCancellationToken();
            IAuthenticationFilter filter = CreateStubFilter();
            TraceRecord record = null;
            ITraceWriter tracer = CreateTracer((r) => { record = r; });
            IAuthenticationFilter product = CreateProductUnderTest(filter, tracer);
            HttpAuthenticationChallengeContext context = null;

            // Act
            Task task = product.ChallengeAsync(context, cancellationToken);

            // Assert
            Assert.NotNull(task);
            task.Wait();
            Assert.NotNull(record);
            Assert.Null(record.Request);
        }

        private static HttpActionContext CreateActionContext()
        {
            return new HttpActionContext();
        }

        private static HttpActionContext CreateActionContext(HttpRequestMessage request)
        {
            HttpControllerContext controllerContext = new HttpControllerContext();
            controllerContext.Request = request;
            HttpActionContext actionContext = new HttpActionContext();
            actionContext.ControllerContext = controllerContext;
            return actionContext;
        }

        private static HttpAuthenticationContext CreateAuthenticationContext()
        {
            HttpActionContext actionContext = CreateActionContext();
            IPrincipal principal = CreateDummyPrincipal();
            return new HttpAuthenticationContext(actionContext, principal);
        }

        private static HttpAuthenticationContext CreateAuthenticationContext(HttpRequestMessage request)
        {
            HttpActionContext actionContext = CreateActionContext(request);
            IPrincipal principal = CreateDummyPrincipal();
            return new HttpAuthenticationContext(actionContext, principal);
        }

        private static CancellationToken CreateCancellationToken()
        {
            return new CancellationToken(canceled: true);
        }

        private static HttpAuthenticationChallengeContext CreateChallengeContext()
        {
            HttpActionContext actionContext = CreateActionContext();
            IHttpActionResult result = CreateDummyResult();
            return new HttpAuthenticationChallengeContext(actionContext, result);
        }

        private static HttpAuthenticationChallengeContext CreateChallengeContext(HttpRequestMessage request,
            IHttpActionResult result)
        {
            HttpActionContext actionContext = CreateActionContext(request);
            return new HttpAuthenticationChallengeContext(actionContext, result);
        }

        private IHttpActionResult CreateDummyActionResult()
        {
            return new Mock<IHttpActionResult>(MockBehavior.Strict).Object;
        }

        private static IAuthenticationFilter CreateDummyFilter()
        {
            return new Mock<IAuthenticationFilter>(MockBehavior.Strict).Object;
        }

        private static IPrincipal CreateDummyPrincipal()
        {
            return new Mock<IPrincipal>(MockBehavior.Strict).Object;
        }

        private static IHttpActionResult CreateDummyResult()
        {
            return new Mock<IHttpActionResult>(MockBehavior.Strict).Object;
        }

        private static ITraceWriter CreateDummyTracer()
        {
            return new Mock<ITraceWriter>(MockBehavior.Strict).Object;
        }

        private static AuthenticationFilterTracer CreateProductUnderTest(IAuthenticationFilter innerFilter,
            ITraceWriter traceWriter)
        {
            return new AuthenticationFilterTracer(innerFilter, traceWriter);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static IAuthenticationFilter CreateStubFilter()
        {
            Mock<IAuthenticationFilter> mock = new Mock<IAuthenticationFilter>();
            mock.Setup(f => f.AuthenticateAsync(It.IsAny<HttpAuthenticationContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null));
            mock.Setup(f => f.ChallengeAsync(It.IsAny<HttpAuthenticationChallengeContext>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null));
            return mock.Object;
        }

        private static ITraceWriter CreateStubTracer()
        {
            return new Mock<ITraceWriter>().Object;
        }

        private static ITraceWriter CreateTracer(Action<TraceRecord> trace)
        {
            Mock<ITraceWriter> mock = new Mock<ITraceWriter>(MockBehavior.Strict);
            TraceRecord record = null;
            mock.Setup(t => t.Trace(It.IsAny<HttpRequestMessage>(), It.IsAny<string>(), It.IsAny<TraceLevel>(),
                It.IsAny<Action<TraceRecord>>()))
                .Callback<HttpRequestMessage, string, TraceLevel, Action<TraceRecord>>((r, c, l, a) =>
                    {
                        record = new TraceRecord(r, c, l);
                        a.Invoke(record);
                        trace.Invoke(record);
                    });
            return mock.Object;
        }
    }
}
