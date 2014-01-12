//-----------------------------------------------------------------------
// <copyright file="Channel.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Messaging {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Diagnostics.Contracts;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Cache;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Net.Mime;
	using System.Net.Sockets;
	using System.Runtime.Serialization.Json;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Web;
	using System.Xml;
	using DotNetOpenAuth.Messaging.Reflection;
	using Validation;

	/// <summary>
	/// Manages sending direct messages to a remote party and receiving responses.
	/// </summary>
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Unavoidable.")]
    //shuo:begin
    // public abstract class Channel : IDisposable {
	public abstract partial class Channel : IDisposable {
    //shuo:end
        protected virtual HttpResponseMessage PrepareIndirectResponse(IDirectedProtocolMessage message)
        {
            Requires.NotNull(message, "message");
            Requires.That(message.Recipient != null, "message", MessagingStrings.DirectedMessageMissingRecipient);
            Requires.That((message.HttpMethods & (HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.PostRequest)) != 0, "message", "GET or POST expected.");

            Assumes.True(message != null && message.Recipient != null);
            var messageAccessor = this.MessageDescriptions.GetAccessor(message);
            Assumes.True(message != null && message.Recipient != null);
            var fields = messageAccessor.Serialize();

            string methodName = messageAccessor.Description.MessageType.Name; //method = CheckIdRequest


            HttpResponseMessage response = null;
            bool tooLargeForGet = false;
            if ((message.HttpMethods & HttpDeliveryMethods.GetRequest) == HttpDeliveryMethods.GetRequest)
            {
                bool payloadInFragment = false;
                var httpIndirect = message as IHttpIndirectResponse;
                if (httpIndirect != null)
                {
                    payloadInFragment = httpIndirect.Include301RedirectPayloadInFragment;
                }

                // First try creating a 301 redirect, and fallback to a form POST
                // if the message is too big.
                response = this.Create301RedirectResponse(message, fields, payloadInFragment);
                tooLargeForGet = response.Headers.Location.PathAndQuery.Length > this.MaximumIndirectMessageUrlLength;
            }

            // Make sure that if the message is too large for GET that POST is allowed.
            if (tooLargeForGet)
            {
                ErrorUtilities.VerifyProtocol(
                    (message.HttpMethods & HttpDeliveryMethods.PostRequest) == HttpDeliveryMethods.PostRequest,
                    MessagingStrings.MessageExceedsGetSizePostNotAllowed);
            }

            // If GET didn't work out, for whatever reason...
            if (response == null || tooLargeForGet)
            {
                response = this.CreateFormPostResponse(message, fields);
            }

            return response;
        }
	}
}
