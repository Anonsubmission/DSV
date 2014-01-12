﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Mocks
{
    internal class MockDelegatingHandler : DelegatingHandler
    {
        private bool _throwInSendAsync;

        public MockDelegatingHandler(bool throwInSendAsync = false)
        {
            _throwInSendAsync = throwInSendAsync;
        }

        public MockDelegatingHandler(HttpMessageHandler innerHandler, bool throwInSendAsync = false)
            : base(innerHandler)
        {
            _throwInSendAsync = throwInSendAsync;
        }

        public bool WasInvoked { get; private set; }

        public HttpRequestMessage Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Exception SendAsyncException { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            Request = request;
            CancellationToken = cancellationToken;

            if (_throwInSendAsync)
            {
                SendAsyncException = new Exception("SendAsync exception");
                throw SendAsyncException;
            }
#if !NETFX_CORE // Extension method only available on non portable library
            return TaskHelpers.FromResult(request.CreateResponse());
#else
            return TaskHelpers.FromResult(new HttpResponseMessage { RequestMessage = request });
#endif
        }
    }
}
