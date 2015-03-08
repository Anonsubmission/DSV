﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using Microsoft.TestCommon;

namespace System.Web.Http.Owin
{
    public class OwinBufferPolicySelectorTest
    {
        [Fact]
        public void UseBufferedInputStream_ReturnsFalse()
        {
            Assert.False(new OwinBufferPolicySelector().UseBufferedInputStream(null));
        }

        [Fact]
        public void UseBufferedOutputStream_ReturnsTrue_ForObjectContent()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ObjectContent<string>("blue", new JsonMediaTypeFormatter());

            Assert.True(new OwinBufferPolicySelector().UseBufferedOutputStream(response));
        }

        [Fact]
        public void UseBufferedOutputStream_ReturnsFalse_ForSpecifiedContentLength()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ObjectContent<string>("blue", new JsonMediaTypeFormatter());
            response.Content.Headers.ContentLength = 5;

            Assert.False(new OwinBufferPolicySelector().UseBufferedOutputStream(response));
        }

        [Fact]
        public void UseBufferedOutputStream_ReturnsFalse_ForChunkedTransferEncoding()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Headers.TransferEncodingChunked = true;
            response.Content = new ObjectContent<string>("blue", new JsonMediaTypeFormatter());

            Assert.False(new OwinBufferPolicySelector().UseBufferedOutputStream(response));
        }

        [Fact]
        public void UseBufferedOutputStream_ReturnsFalse_ForStreamContent()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StreamContent(new MemoryStream());

            Assert.False(new OwinBufferPolicySelector().UseBufferedOutputStream(response));
        }

        [Fact]
        public void UseBufferedOutputStream_ReturnsFalse_ForPushStreamContent()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new PushStreamContent((s, c, tc) => { return; });

            Assert.False(new OwinBufferPolicySelector().UseBufferedOutputStream(response));
        }
    }
}
