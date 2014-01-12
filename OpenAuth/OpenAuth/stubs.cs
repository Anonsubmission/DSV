

using System.ComponentModel;
using System.Runtime.Serialization;
using DotNetOpenAuth.OpenId;

namespace System{	
	public class  Uri: ISerializable{
        public string _Scheme, _AbsolutePath, _Authority, _AbsoluteUrl;
        public string AbsolutePath {
            get { return _AbsolutePath; }
        }
        public string Scheme {
            get { return _Scheme; }
        }
        public string Authority {
            get { return _Authority; }
        }
		public string AbsoluteUri {
			get {return _AbsoluteUrl; }
		}
		public static bool operator ==(Uri uri1,Uri uri2) {
            if (uri1._Scheme!=uri2._Scheme) 
                return false;
            if (uri1._AbsolutePath!=uri2._AbsolutePath) 
                return false;
            if (uri1._Authority!=uri2._Authority) 
                return false;
			if (uri1._AbsoluteUrl!=uri2._AbsoluteUrl) 
                return false;
            return true;
        }
        public static bool operator !=(Uri uri1, Uri uri2)
        {
            if (uri1._Scheme != uri2._Scheme)
                return true;
            if (uri1._AbsolutePath != uri2._AbsolutePath)
                return true;
            if (uri1._Authority != uri2._Authority)
                return true;
			if (uri1._AbsoluteUrl != uri2._AbsoluteUrl)
                return true;
            return false;
        }
		public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext){}
	}
}

interface Picker1
{
    System.Uri NondetUri();
    IdentifierDiscoveryResult NondetIdentifierDiscoveryResult();
}

namespace System.Web {

    public sealed class HttpRequest
    {
        Picker1 p;
        private Uri _Url;
        public HttpRequest() { _Url = p.NondetUri(); }
        public Uri Url
        {
            get { return _Url; }
        }
    }

    public sealed class HttpContext 
    {
        HttpRequest _httpRequest;
        static HttpContext _Current = new HttpContext(new HttpRequest() );
        HttpContext(HttpRequest input)
        {
            _httpRequest = input;
        }
        public static HttpContext Current
        {
            get { return _Current; }
        }
        public HttpRequest Request
        {
            get { return _httpRequest; }
            set
            {
                this._httpRequest = value;
            }
        }
    }
}

namespace DotNetOpenAuth.OpenId
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    //using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.OpenId.Messages;
    using DotNetOpenAuth.OpenId.RelyingParty;
    //using Validation;

    
	
    public class IdentifierDiscoveryResult
    {
        Picker1 p;

        public string ClaimedIdentifier, ProviderEndpoint, ProviderLocalIdentifier, Protocol;

        public IdentifierDiscoveryResult(string a, string b, string c, string d, string e, string f)
        {

        }

        public static bool operator ==(IdentifierDiscoveryResult a, IdentifierDiscoveryResult b)
        {
            if (a.ClaimedIdentifier != b.ClaimedIdentifier)
                return false;
            if (a.ProviderEndpoint != b.ProviderEndpoint)
                return false;
            if (a.ProviderLocalIdentifier != b.ProviderLocalIdentifier)
                return false;
            if (a.Protocol != b.Protocol)
                return false;
            return true;
        }

        public static bool operator !=(IdentifierDiscoveryResult a, IdentifierDiscoveryResult b)
        {
            if (a.ClaimedIdentifier != b.ClaimedIdentifier)
                return true;
            if (a.ProviderEndpoint != b.ProviderEndpoint)
                return true;
            if (a.ProviderLocalIdentifier != b.ProviderLocalIdentifier)
                return true;
            if (a.Protocol != b.Protocol)
                return true;
            return false;
        }
    }

}


namespace DotNetOpenAuth.OpenId.RelyingParty {
  
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Web;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId.Messages;
	
    
	public partial class PositiveAuthenticationResponse : PositiveAnonymousResponse
    {
        Picker1 p;

        /*
		protected new PositiveAssertionResponse Response
        {
			get { return (PositiveAssertionResponse)base.Response; }
		}
        */
        //public IdentifierDiscoveryResult _Endpoint = new IdentifierDiscoveryResult(null, null, null, null, null, null);

        public IdentifierDiscoveryResult _Endpoint;

        
        public PositiveAuthenticationResponse()
        {
            _Endpoint = p.NondetIdentifierDiscoveryResult();
        }

        public PositiveAuthenticationResponse(PositiveAssertionResponse r, OpenIdRelyingParty re) {
            _Endpoint = p.NondetIdentifierDiscoveryResult(); 
        }

		public IdentifierDiscoveryResult Endpoint {
            get { return _Endpoint; }
            set { }
        }
	}
 
}

namespace DotNetOpenAuth.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The interface that classes must implement to be serialized/deserialized
    /// as protocol messages.
    /// </summary>
    public interface IProtocolMessage : IMessage
    {
        /// <summary>
        /// Gets the level of protection this message requires.
        /// </summary>
        MessageProtections RequiredProtection { 
			get; set;
		}
		
        /// <summary>
        /// Gets a value indicating whether this is a direct or indirect message.
        /// </summary>
        MessageTransport Transport { get; }

    }
	
	public class uri1{
		public string AbsolutePath;
		public string Scheme;
		public string Authority;
	}
	
	// Summary:
    //     Implemented by messages that have explicit recipients (direct requests and
    //     all indirect messages).
    public interface IDirectedProtocolMessage : IProtocolMessage, IMessage
    {
        // Summary:
        //     Gets the preferred method of transport for the message.
        //
        // Remarks:
        //     For indirect messages this will likely be GET+POST, which both can be simulated
        //     in the user agent: the GET with a simple 301 Redirect, and the POST with
        //     an HTML form in the response with javascript to automate submission.
        HttpDeliveryMethods HttpMethods { get; }
		
		MessageProtections RequiredProtection { get; set;}
        //
        // Summary:
        //     Gets the URL of the intended receiver of this message.
        Uri Recipient { get; set;}
    }
}


namespace DotNetOpenAuth.OpenId.Extensions.SimpleRegistration
{
    public class ClaimsResponse
    {
        public string _BirthDate, 
            _Country, _Email, 
            _FullName, _Nickname;

        public string BirthDate{
            get{
                return _BirthDate;
            }
            set{
                _BirthDate = value;
            }
        }

        public string Country{
            get{
                return _Country;
            }
            set{
                _Country = value;
            }
        }

        public string Email{
            get{
                return _Email;
            }
            set{
                _Email = value;
            }
        }

        public string FullName{
            get{
                return _FullName;
            }
            set{
                _FullName = value;
            }
        }

        public string Nickname{
            get{
                return _Nickname;
            }
            set{
                _Nickname = value;
            }
        }


    }
}


namespace DotNetOpenAuth.OpenId.Messages
{
    using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Net.Security;
	using System.Web;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.Messaging.Bindings;
	using DotNetOpenAuth.Messaging.Reflection;
	using DotNetOpenAuth.OpenId.ChannelElements;


	public class RequestBase : IDirectedProtocolMessage
    {
	
		private readonly Dictionary<string, string> extraData = new Dictionary<string, string>();
		
		HttpDeliveryMethods IDirectedProtocolMessage.HttpMethods
        {
            get
            {
                // OpenID 2.0 section 5.1.1
                HttpDeliveryMethods methods = HttpDeliveryMethods.PostRequest;
                if (this.Transport == MessageTransport.Indirect)
                {
                    methods |= HttpDeliveryMethods.GetRequest;
                }
                return methods;
            }
        }
		
		public virtual MessageProtections RequiredProtection{
            get { return MessageProtections.All; }
			set {}
        }
		
		public MessageTransport Transport { get; private set; }
		
		public virtual void EnsureValidMessage()
        {
        }
		
		public IDictionary<string, string> ExtraData
        {
            get { return this.extraData; }
        }
		
		public Version Version { get; private set; }
        public Uri _Recipient;
        public Uri Recipient
        {
            get { return this._Recipient; }
            set
            {
                this._Recipient = value;
            }
        }
		
	}
	
	public partial class IndirectSignedResponse : IndirectResponseBase, ITamperResistantOpenIdMessage    {
	
		private DateTime creationDateUtc = DateTime.UtcNow;

			
		public override MessageProtections RequiredProtection {
			get { return MessageProtections.All; }
		}
		
		string ITamperResistantProtocolMessage.Signature { get; set; }
	
		string ITamperResistantOpenIdMessage.SignedParameterOrder { get; set; }
		
		string ITamperResistantOpenIdMessage.AssociationHandle { get; set; }
		
		string IReplayProtectedProtocolMessage.Nonce { get; set; }
		
		string IReplayProtectedProtocolMessage.NonceContext {
			get {
				if (this.ProviderEndpoint != null) {
					return this.ProviderEndpoint.AbsolutePath;
				} else {
					// This is the Provider, on an OpenID 1.x check_authentication message.
					// We don't need any special nonce context because the Provider
					// generated and consumed the nonce.
					return string.Empty;
				}
			}
		}
		
		string ITamperResistantOpenIdMessage.InvalidateHandle { get; set; }
				
		internal Uri ProviderEndpoint { get; set; }
				
		DateTime IExpiringProtocolMessage.UtcCreationDate {
			get { return this.creationDateUtc; }
			set { this.creationDateUtc = value.ToUniversalTime(); }
		}
        public Uri _ReturnTo;
        public Uri ReturnTo
        {
            get { return this._ReturnTo; }
            set
            {
                this._ReturnTo = value;
            }
        }
		//public uri1 ReturnTo {get; set;}
	}


 
    public class PositiveAssertionResponse : IndirectSignedResponse
    {
        public Identifier _ClaimedIdentifier;
        public PositiveAssertionResponse() { }
        public Identifier ClaimedIdentifier {
            get { return this._ClaimedIdentifier; }
            set
            {
                this._ClaimedIdentifier = value;
            } 
        }
    }
}


namespace DotNetOpenAuth.OpenId
{
    public abstract class Identifier
    {
        public string _OriginalString; 
		
		public static implicit operator string(Identifier identifier){
			return identifier._OriginalString;
		}
		
        public static bool operator ==(Identifier id1, Identifier id2)
        {
            return id1._OriginalString == id2._OriginalString;
        }

        public static bool operator !=(Identifier id1, Identifier id2)
        {
            return id1._OriginalString != id2._OriginalString;
        }
    }
}

//csc.exe /t:library /r:D:\codeplex\boogie_repo\bin\v4.5\Debug\DotNetOpenAuth.Core.dll;D:\codeplex\boogie_repo\bin\v4.5\Debug\DotNetOpenAuth.OpenId.dll;D:\codeplex\boogie_repo\bin\v4.5\Debug\DotNetOpenAuth.OpenId.RelyingParty.dll /debug /D:DEBUG Stubs.cs