//-----------------------------------------------------------------------
// <copyright file="IDirectedProtocolMessage.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Messaging {
	using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    //ERIC'S CODE
    public class Uri1 : Uri
    {
        string _Scheme, _AbsolutePath, _Authority;

        public Uri1(string uriString):base(uriString){}
        protected Uri1(SerializationInfo serializationInfo, StreamingContext streamingContext):base(serializationInfo, streamingContext){}
        public Uri1(string uriString, UriKind uriKind):base(uriString,uriKind){}
        public Uri1(Uri baseUri, string relativeUri):base(baseUri,relativeUri){}
        public Uri1(Uri baseUri, Uri relativeUri) : base(baseUri, relativeUri) {}
        
        public string AbsolutePath { get { return _Scheme; } }
        public string Authority { get { return _Authority; } }
        public string Scheme { get { return _AbsolutePath; } }

    }

	/// <summary>
	/// Implemented by messages that have explicit recipients
	/// (direct requests and all indirect messages).
	/// </summary>
	public interface IDirectedProtocolMessage : IProtocolMessage {
		/// <summary>
		/// Gets the preferred method of transport for the message.
		/// </summary>
		/// <remarks>
		/// For indirect messages this will likely be GET+POST, which both can be simulated in the user agent:
		/// the GET with a simple 301 Redirect, and the POST with an HTML form in the response with javascript
		/// to automate submission.
		/// </remarks>
		HttpDeliveryMethods HttpMethods { get; }

		/// <summary>
		/// Gets the URL of the intended receiver of this message.
		/// </summary>
        /// ERIC'S CODE
        Uri Recipient { get; }
	}
}
