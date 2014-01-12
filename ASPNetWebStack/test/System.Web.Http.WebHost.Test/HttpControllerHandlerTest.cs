﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Hosting;
using System.Web.Routing;
using Microsoft.TestCommon;
using Moq;
using Newtonsoft.Json.Linq;

namespace System.Web.Http.WebHost
{
    public class HttpControllerHandlerTest
    {
        public static TheoryDataSet<HttpMethod> AllHttpMethods
        {
            get
            {
                return new TheoryDataSet<HttpMethod>
                {       
                    HttpMethod.Get,
                    HttpMethod.Post,
                    HttpMethod.Put,
                    HttpMethod.Delete,
                    HttpMethod.Head,
                    HttpMethod.Options,
                    HttpMethod.Trace
                };
            }
        }

        public static TheoryDataSet<HttpMethod> HttpMethodsWithContent
        {
            get
            {
                return new TheoryDataSet<HttpMethod>
                {       
                    HttpMethod.Post,
                    HttpMethod.Put,
                    HttpMethod.Delete,
                };
            }
        }

        [Theory]
        [PropertyData("AllHttpMethods")]
        public void ConvertRequest_Creates_HttpRequestMessage_For_All_HttpMethods(HttpMethod httpMethod)
        {
            // Arrange
            HttpContextBase contextBase = CreateStubContext(httpMethod.Method, new MemoryStream());

            // Act
            HttpRequestMessage request = HttpControllerHandler.ConvertRequest(contextBase);

            // Assert
            Assert.Equal(httpMethod, request.Method);
        }

        [Fact]
        public void ConvertRequest_Copies_Headers_And_Content_Headers()
        {
            // Arrange
            HttpContextBase contextBase = CreateStubContext("Get", new MemoryStream());
            HttpRequestBase requestBase = contextBase.Request;
            NameValueCollection nameValues = requestBase.Headers;
            nameValues["myHeader"] = "myValue";
            nameValues["Content-Type"] = "application/mine";

            // Act
            HttpRequestMessage request = HttpControllerHandler.ConvertRequest(contextBase);
            string[] headerValues = request.Headers.GetValues("myHeader").ToArray();

            // Assert
            Assert.Equal("myValue", headerValues[0]);
            Assert.Equal("application/mine", request.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [PropertyData("HttpMethodsWithContent")]
        public void ConvertRequest_Creates_Request_With_Content_For_Content_Methods(HttpMethod httpMethod)
        {
            // Arrange
            HttpContextBase contextBase = CreateStubContext(httpMethod.Method, new MemoryStream());

            // Act
            HttpRequestMessage actualRequest = HttpControllerHandler.ConvertRequest(contextBase);

            // Assert
            Assert.NotNull(actualRequest.Content);
        }

        [Fact]
        public void ConvertRequest_Uses_HostBufferPolicySelector_To_Select_Buffered_Stream()
        {
            // Arrange
            HttpContextBase contextMock = CreateStubContext("Post", new MemoryStream(new byte[] { 5 }));
            MemoryStream memoryStream = new MemoryStream();

            // Act
            HttpRequestMessage actualRequest = HttpControllerHandler.ConvertRequest(contextMock);
            actualRequest.Content.CopyToAsync(memoryStream).Wait();
            byte[] actualBuffer = memoryStream.GetBuffer();

            // Assert
            Assert.Equal(5, actualBuffer[0]);
        }

        [Fact]
        public void ConvertRequest_AddsOwinEnvironment_WhenPresentInHttpContext()
        {
            // Arrange
            using (MemoryStream ignoreStream = new MemoryStream())
            {
                HttpRequestBase stubRequest = CreateStubRequest("IgnoreMethod", ignoreStream);
                IDictionary<string, object> expectedEnvironment = new Dictionary<string, object>();
                IDictionary items = new Hashtable();
                items.Add(HttpControllerHandler.OwinEnvironmentHttpContextKey, expectedEnvironment);
                HttpContextBase context = CreateStubContext(stubRequest, items);

                // Act
                using (HttpRequestMessage actualRequest = HttpControllerHandler.ConvertRequest(context))
                {
                    IDictionary<string, object> environment = actualRequest.GetOwinEnvironment();

                    // Assert
                    Assert.Same(expectedEnvironment, environment);
                }
            }
        }

        [Fact]
        public void ConvertRequest_DoesNotAddOwinEnvironment_WhenNotPresentInHttpContext()
        {
            // Arrange
            using (MemoryStream ignoreStream = new MemoryStream())
            {
                HttpRequestBase stubRequest = CreateStubRequest("IgnoreMethod", ignoreStream);
                IDictionary items = new Hashtable();
                HttpContextBase context = CreateStubContext(stubRequest, items);

                // Act
                using (HttpRequestMessage actualRequest = HttpControllerHandler.ConvertRequest(context))
                {
                    // Assert
                    object ignore;
                    bool found = actualRequest.Properties.TryGetValue(HttpControllerHandler.OwinEnvironmentKey,
                        out ignore);
                    Assert.False(found);
                }
            }
        }

        [Fact]
        public void ConvertRequest_DoesNotAddOwinEnvironment_WhenItemsIsNull()
        {
            // Arrange
            using (MemoryStream ignoreStream = new MemoryStream())
            {
                HttpRequestBase stubRequest = CreateStubRequest("IgnoreMethod", ignoreStream);
                IDictionary items = null;
                HttpContextBase context = CreateStubContext(stubRequest, items);

                // Act
                using (HttpRequestMessage actualRequest = HttpControllerHandler.ConvertRequest(context))
                {
                    // Assert
                    object ignore;
                    bool found = actualRequest.Properties.TryGetValue(HttpControllerHandler.OwinEnvironmentKey,
                        out ignore);
                    Assert.False(found);
                }
            }
        }

        [Fact]
        public void ConvertResponse_IfResponseHasNoCacheControlDefined_SetsNoCacheCacheabilityOnAspNetResponse()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            HttpResponseMessage response = new HttpResponseMessage();
            HttpRequestMessage request = new HttpRequestMessage();

            // Act
            HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);

            // Assert
            contextMock.Verify(c => c.Response.Cache.SetCacheability(HttpCacheability.NoCache));
        }

        [Fact]
        public void ConvertResponse_IfResponseHasCacheControlDefined_DoesNotSetCacheCacheabilityOnAspNetResponse()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            HttpResponseMessage response = new HttpResponseMessage();
            HttpRequestMessage request = new HttpRequestMessage();
            response.Headers.CacheControl = new CacheControlHeaderValue { Public = true };

            // Act
            HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);

            // Assert
            contextMock.Verify(c => c.Response.Cache.SetCacheability(HttpCacheability.NoCache), Times.Never());
        }

        [Fact]
        public Task ConvertResponse_DisposesRequestAndResponse()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet((hcb) => hcb.Response.OutputStream).Returns(Stream.Null);

            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage();

            // Act
            return HttpControllerHandler.ConvertResponse(contextMock.Object, response, request).ContinueWith(
                _ =>
                {
                    // Assert
                    Assert.ThrowsObjectDisposed(() => request.Method = HttpMethod.Get, typeof(HttpRequestMessage).FullName);
                    Assert.ThrowsObjectDisposed(() => response.StatusCode = HttpStatusCode.OK, typeof(HttpResponseMessage).FullName);
                });
        }

        [Fact]
        public Task ConvertResponse_DisposesRequestAndResponseWithContent()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet((hcb) => hcb.Response.OutputStream).Returns(Stream.Null);

            HttpRequestMessage request = new HttpRequestMessage() { Content = new StringContent("request") };
            HttpResponseMessage response = new HttpResponseMessage() { Content = new StringContent("response") };

            // Act
            return HttpControllerHandler.ConvertResponse(contextMock.Object, response, request).ContinueWith(
                _ =>
                {
                    // Assert
                    Assert.ThrowsObjectDisposed(() => request.Method = HttpMethod.Get, typeof(HttpRequestMessage).FullName);
                    Assert.ThrowsObjectDisposed(() => response.StatusCode = HttpStatusCode.OK, typeof(HttpResponseMessage).FullName);
                });
        }

        [Fact]
        public void ProcessRequestAsync_Flows_ClientDisconnectedCancellationToken()
        {
            // Arrange
            ShortCircuitHandler spy = new ShortCircuitHandler();
            CancellationToken clientDisconnectedToken = new CancellationToken(true);
            HttpRequestBase requestBase = CreateStubRequest("Get", Stream.Null);
            HttpResponseBase responseBase = CreateStubResponse(clientDisconnectedToken);
            HttpContextBase contextBase = CreateStubContext(requestBase, responseBase);
            HttpControllerHandler handler = new HttpControllerHandler(new RouteData(), spy);

            // Act
            var taskResult = handler.ProcessRequestAsyncCore(contextBase);

            // Assert
            taskResult.ThrowIfFaulted();
            Assert.Equal(clientDisconnectedToken, spy.CancelToken);
        }

        private class ShortCircuitHandler : HttpMessageHandler
        {
            public CancellationToken CancelToken { get; private set; }
          
            protected override Task<HttpResponseMessage> SendAsync(
                    HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CancelToken = cancellationToken;
                return Task.FromResult<HttpResponseMessage>(null);
            }
        }

        [Fact]
        public void SuppressFormsAuthenticationRedirect_DoesntRequireSuppressRedirect()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            IDictionary contextItems = new Hashtable();
            contextMock.SetupGet(hcb => hcb.Response.StatusCode).Returns(200);
            contextMock.SetupGet(hcb => hcb.Items).Returns(contextItems);

            // Act
            HttpControllerHandler.EnsureSuppressFormsAuthenticationRedirect(contextMock.Object);

            // Assert
            Assert.False(contextMock.Object.Response.SuppressFormsAuthenticationRedirect);
        }

        [Fact]
        public void SuppressFormsAuthenticationRedirect_RequireSuppressRedirect()
        {
            // Arrange
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            IDictionary contextItems = new Hashtable();
            contextMock.SetupGet(hcb => hcb.Response.StatusCode).Returns(401);
            contextMock.SetupGet(hcb => hcb.Items).Returns(contextItems);

            // Act
            HttpControllerHandler.EnsureSuppressFormsAuthenticationRedirect(contextMock.Object);

            // Assert
            Assert.True(contextMock.Object.Response.SuppressFormsAuthenticationRedirect);
        }

        [Fact]
        public void ConvertResponse_Creates_Correct_HttpResponseBase()
        {
            // Arrange
            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", new JsonMediaTypeFormatter());

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert preparation -- deserialize the response
            memoryStream.Seek(0L, SeekOrigin.Begin);
            string responseString = null;
            using (var streamReader = new StreamReader(memoryStream))
            {
                responseString = streamReader.ReadToEnd();
            }

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.OK, responseBase.StatusCode);
            Assert.True(responseBase.Headers["Content-Type"].StartsWith(JsonMediaTypeFormatter.DefaultMediaType.MediaType));
            Assert.Equal("\"hello\"", responseString);
        }

        [Fact]
        public void ConvertResponse_Returns_Error_Response_When_Formatter_Write_Task_Faults()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            try
            {
                // to capture stack trace inside this method
                throw new NotSupportedException("Expected error");
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Returns(tcs.Task);

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert preparation -- deserialize the HttpError response
            HttpError httpError = null;
            memoryStream.Seek(0L, SeekOrigin.Begin);
            using (StreamContent content = new StreamContent(memoryStream))
            {
                content.Headers.ContentType = JsonMediaTypeFormatter.DefaultMediaType;
                httpError = content.ReadAsAsync<HttpError>().Result;
            }

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.True(responseBase.Headers["Content-Type"].StartsWith(JsonMediaTypeFormatter.DefaultMediaType.MediaType));
            Assert.Equal("An error has occurred.", httpError["Message"]);
            Assert.Equal("The 'ObjectContent`1' type failed to serialize the response body for content type 'application/json; charset=utf-8'.", httpError["ExceptionMessage"]);
            Assert.Equal(typeof(InvalidOperationException).FullName, httpError["ExceptionType"]);
            Assert.True(httpError.ContainsKey("StackTrace"));

            HttpError innerError = (httpError["InnerException"] as JObject).ToObject<HttpError>();
            Assert.NotNull(innerError);
            Assert.Equal(typeof(NotSupportedException).FullName, innerError["ExceptionType"].ToString());
            Assert.Equal("Expected error", innerError["ExceptionMessage"]);
            Assert.Contains(MethodInfo.GetCurrentMethod().Name, innerError["StackTrace"].ToString());
        }

        [Fact]
        public void ConvertResponse_Returns_Error_Response_When_Formatter_Write_Throws_Immediately()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Throws(new NotSupportedException("Expected error"));

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert preparation -- deserialize the HttpError response
            HttpError httpError = null;
            memoryStream.Seek(0L, SeekOrigin.Begin);
            using (StreamContent content = new StreamContent(memoryStream))
            {
                content.Headers.ContentType = JsonMediaTypeFormatter.DefaultMediaType;
                httpError = content.ReadAsAsync<HttpError>().Result;
            }

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.True(responseBase.Headers["Content-Type"].StartsWith(JsonMediaTypeFormatter.DefaultMediaType.MediaType));
            Assert.Equal("An error has occurred.", httpError["Message"]);
            Assert.Equal("The 'ObjectContent`1' type failed to serialize the response body for content type 'application/json; charset=utf-8'.", httpError["ExceptionMessage"]);
            Assert.Equal(typeof(InvalidOperationException).FullName, httpError["ExceptionType"]);
            Assert.True(httpError.ContainsKey("StackTrace"));

            HttpError innerError = (httpError["InnerException"] as JObject).ToObject<HttpError>();
            Assert.NotNull(innerError);
            Assert.Equal(typeof(NotSupportedException).FullName, innerError["ExceptionType"].ToString());
            Assert.Equal("Expected error", innerError["ExceptionMessage"]);
            Assert.Contains("System.Net.Http.HttpContent.CopyToAsync", innerError["StackTrace"].ToString());
        }

        [Fact]
        public void ConvertResponse_Returns_User_Response_When_Formatter_Write_Throws_HttpResponseException_With_No_Content()
        {
            // Arrange
            HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            errorResponse.Headers.Add("myHeader", "myValue");

            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Throws(new HttpResponseException(errorResponse));

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();
            memoryStream.Seek(0L, SeekOrigin.Begin);

            // Assert
            Assert.Equal<int>((int)errorResponse.StatusCode, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Equal("myValue", responseBase.Headers["myHeader"]);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void ConvertResponse_Returns_User_Response_When_Formatter_Write_Throws_HttpResponseException_With_Content()
        {
            // Arrange
            HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            errorResponse.Headers.Add("myHeader", "myValue");
            errorResponse.Content = new StringContent("user message", Encoding.UTF8, "application/fake");

            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Throws(new HttpResponseException(errorResponse));

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert preparation -- deserialize the response

            memoryStream.Seek(0L, SeekOrigin.Begin);
            string responseContent = null;
            using (var streamReader = new StreamReader(memoryStream))
            {
                responseContent = streamReader.ReadToEnd();
            }

            // Assert
            Assert.Equal<int>((int)errorResponse.StatusCode, responseBase.StatusCode);
            Assert.True(responseBase.Headers["Content-Type"].StartsWith("application/fake"));
            Assert.Equal("user message", responseContent);
            Assert.Equal("myValue", responseBase.Headers["myHeader"]);
        }

        [Fact]
        public void ConvertResponse_Returns_InternalServerError_And_No_Content_When_Formatter_Write_Task_Faults_During_Error_Response()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.SetException(new NotSupportedException("Expected error"));

            // This formatter throws on any write attempt
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                            It.IsAny<object>(),
                                                            It.IsAny<Stream>(),
                                                            It.IsAny<HttpContent>(),
                                                            It.IsAny<TransportContext>())).Returns(tcs.Task);

            // Create a local config to hook to the request to condition
            // the formatter selection for the error response
            HttpConfiguration config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(formatterMock.Object);

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void ConvertResponse_Returns_InternalServerError_And_No_Content_When_Formatter_Write_Throws_Immediately_During_Error_Response()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };

            // This formatter throws on any write attempt
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                            It.IsAny<object>(),
                                                            It.IsAny<Stream>(),
                                                            It.IsAny<HttpContent>(),
                                                            It.IsAny<TransportContext>())).Throws(new NotSupportedException("Expected error"));

            // Create a local config to hook to the request to condition
            // the formatter selection for the error response
            HttpConfiguration config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(formatterMock.Object);

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void ConvertResponse_Returns_InternalServerError_And_No_Content_When_Content_Negotiation_Cannot_Find_Formatter_For_Error_Response()
        {
            // Create a content negotiator that works attempting a normal response but fails when creating the error response.
            Mock<IContentNegotiator> negotiatorMock = new Mock<IContentNegotiator>() { CallBase = true };
            negotiatorMock.Setup(m => m.Negotiate(It.IsAny<Type>(), It.IsAny<HttpRequestMessage>(), It.IsAny<IEnumerable<MediaTypeFormatter>>()))
                .Returns((Type t, HttpRequestMessage r, IEnumerable<MediaTypeFormatter> f) =>
                    {
                        ContentNegotiationResult result = t == typeof(HttpError)
                            ? null
                            : new ContentNegotiationResult(f.First(), JsonMediaTypeFormatter.DefaultMediaType);
                        return result;
                    });

            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };

            // This formatter throws on any write attempt
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                            It.IsAny<object>(),
                                                            It.IsAny<Stream>(),
                                                            It.IsAny<HttpContent>(),
                                                            It.IsAny<TransportContext>())).Throws(new NotSupportedException("Expected error"));

            // Create a local config to hook to the request to condition
            // the formatter selection for the error response
            HttpConfiguration config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(formatterMock.Object);
            config.Services.Replace(typeof(IContentNegotiator), negotiatorMock.Object);

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void ConvertResponse_Returns_InternalServerError_And_No_Content_When_No_Content_Negotiator_For_Error_Response()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };

            // This formatter throws on any write attempt
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                            It.IsAny<object>(),
                                                            It.IsAny<Stream>(),
                                                            It.IsAny<HttpContent>(),
                                                            It.IsAny<TransportContext>())).Throws(new NotSupportedException("Expected error"));

            // Create a local config to hook to the request to condition
            // the formatter selection for the error response
            HttpConfiguration config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(formatterMock.Object);
            config.Services.Replace(typeof(IContentNegotiator), null /*negotiatorMock.Object*/);

            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response, request);
            task.Wait();

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void ConvertResponse_Returns_InternalServerError_And_No_Content_For_Null_HttpResponseMessage()
        {
            // Arrange
            MemoryStream memoryStream = new MemoryStream();
            Mock<HttpContextBase> contextMock = CreateMockHttpContextBaseForResponse(memoryStream);
            HttpResponseBase responseBase = contextMock.Object.Response;
            HttpRequestMessage request = new HttpRequestMessage();

            // Act
            Task task = HttpControllerHandler.ConvertResponse(contextMock.Object, response: null, request: new HttpRequestMessage());
            task.Wait();

            // Assert
            Assert.Equal<int>((int)HttpStatusCode.InternalServerError, responseBase.StatusCode);
            Assert.Equal(0, memoryStream.Length);
            Assert.Null(responseBase.Headers["Content-Type"]);
        }

        [Fact]
        public void WriteStreamedErrorResponseAsync_Aborts_When_Formatter_Write_Throws_Immediately()
        {
            // Arrange
            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Throws(new NotSupportedException("Expected error"));

            MemoryStream memoryStream = new MemoryStream();

            Mock<HttpRequestBase> requestBaseMock = new Mock<HttpRequestBase>();
            requestBaseMock.Setup(m => m.Abort()).Verifiable();
            HttpRequestBase requestBase = requestBaseMock.Object;
            HttpResponseBase responseBase = CreateMockHttpResponseBaseForResponse(memoryStream).Object;
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet(m => m.Response).Returns(responseBase);
            HttpContextBase contextBase = CreateStubContext(requestBase, responseBase);

            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.WriteStreamedResponseContentAsync(contextBase, response.Content);
            task.Wait();

            // Assert
            requestBaseMock.Verify();
        }

        [Fact]
        public void WriteStreamedErrorResponseAsync_Aborts_When_Formatter_Write_Faults()
        {
            // Arrange
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(new NotSupportedException("Expected error"));

            Mock<JsonMediaTypeFormatter> formatterMock = new Mock<JsonMediaTypeFormatter>() { CallBase = true };
            formatterMock.Setup(m => m.WriteToStreamAsync(It.IsAny<Type>(),
                                                          It.IsAny<object>(),
                                                          It.IsAny<Stream>(),
                                                          It.IsAny<HttpContent>(),
                                                          It.IsAny<TransportContext>())).Returns(tcs.Task);

            MemoryStream memoryStream = new MemoryStream();

            Mock<HttpRequestBase> requestBaseMock = new Mock<HttpRequestBase>();
            requestBaseMock.Setup(m => m.Abort()).Verifiable();
            HttpRequestBase requestBase = requestBaseMock.Object;
            HttpResponseBase responseBase = CreateMockHttpResponseBaseForResponse(memoryStream).Object;
            HttpContextBase contextBase = CreateStubContext(requestBase, responseBase);

            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.IsLocalKey, new Lazy<bool>(() => true));
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new ObjectContent<string>("hello", formatterMock.Object);

            // Act
            Task task = HttpControllerHandler.WriteStreamedResponseContentAsync(contextBase, response.Content);
            task.Wait();

            // Assert
            requestBaseMock.Verify();
        }

        private static HttpRequestBase CreateStubRequest(string httpMethod, Stream bufferedStream)
        {
            Mock<HttpRequestBase> requestBaseMock = new Mock<HttpRequestBase>() { CallBase = true };
            requestBaseMock.SetupGet(m => m.HttpMethod).Returns(httpMethod);
            requestBaseMock.SetupGet(m => m.Url).Returns(new Uri("Http://localhost"));
            requestBaseMock.SetupGet(m => m.Headers).Returns(new NameValueCollection());
            requestBaseMock.Setup(m => m.InputStream).Returns(bufferedStream);
            return requestBaseMock.Object;
        }

        private static HttpContextBase CreateStubContext(string httpMethod, Stream bufferedStream)
        {
            HttpRequestBase request = CreateStubRequest(httpMethod, bufferedStream);
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet(m => m.Request).Returns(request);
            return contextMock.Object;
        }

        private static HttpContextBase CreateStubContext(HttpRequestBase request, IDictionary items)
        {
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet(m => m.Request).Returns(request);
            contextMock.SetupGet(m => m.Items).Returns(items);
            return contextMock.Object;
        }

        private static HttpContextBase CreateStubContext(HttpRequestBase request, HttpResponseBase response)
        {
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>();
            contextMock.SetupGet(m => m.Request).Returns(request);
            contextMock.SetupGet(m => m.Response).Returns(response);
            return contextMock.Object;
        }

        private static HttpResponseBase CreateStubResponse(CancellationToken clientDisconnectedToken)
        {
            Mock<HttpResponseBase> mock = new Mock<HttpResponseBase>();
            mock.Setup(r => r.ClientDisconnectedToken).Returns(clientDisconnectedToken);
            return mock.Object;
        }

        private static Mock<HttpResponseBase> CreateMockHttpResponseBaseForResponse(Stream outputStream)
        {
            NameValueCollection testHeaders = new NameValueCollection();

            Mock<HttpResponseBase> responseBaseMock = new Mock<HttpResponseBase>() { DefaultValue = DefaultValue.Mock };
            responseBaseMock.Setup(m => m.OutputStream).Returns(outputStream);
            responseBaseMock.Setup(m => m.Headers).Returns(testHeaders);
            responseBaseMock.Setup(m => m.AppendHeader(It.IsAny<string>(), It.IsAny<string>())).Callback<string, string>((s, v) => testHeaders.Add(s, v));
            responseBaseMock.Setup(m => m.ClearHeaders()).Callback(() => testHeaders.Clear());
            responseBaseMock.Setup(m => m.Clear()).Callback(() => testHeaders.Clear());
            responseBaseMock.SetupProperty(m => m.StatusCode);

            return responseBaseMock;
        }

        private static Mock<HttpContextBase> CreateMockHttpContextBaseForResponse(Stream outputStream)
        {
            NameValueCollection testHeaders = new NameValueCollection();

            Mock<HttpResponseBase> responseBaseMock = CreateMockHttpResponseBaseForResponse(outputStream);
            HttpResponseBase responseBase = responseBaseMock.Object;
            Mock<HttpContextBase> contextMock = new Mock<HttpContextBase>() { DefaultValue = DefaultValue.Mock };
            contextMock.SetupGet(m => m.Response).Returns(responseBase);

            return contextMock;
        }

    }
}
