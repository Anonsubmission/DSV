
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using System.Net.Http;
using System.Web;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Messages;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OpenId.ChannelElements;
using DotNetOpenAuth.OpenId.Extensions;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId.RelyingParty.Extensions;


public class GlobalState
{
    public static MessageProtections appliedProtection;
    public static PositiveAssertionResponse is_positive_assertion;
    public static ClaimsResponse actualExt, claimedExt;
    public static IdentifierDiscoveryResult claimedEndPoint, actualEndPoint;

}


namespace DotNetOpenAuth.Messaging{
	using System;
    using System.Collections.Generic;
	using System.Net.Http;
    using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.Messaging.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
	using Validation;


	public abstract partial class Channel1: Channel {

        public HttpResponseMessage PrepareResponseAsync_CCP(PositiveAssertionResponse1 message, CancellationToken cancellationToken = default(CancellationToken))
        {

            this.ProcessOutgoingMessageAsync_CCP(message, cancellationToken);           
            HttpResponseMessage result;
            var directedMessage = message as IDirectedProtocolMessage;
            result = this.PrepareIndirectResponse(directedMessage);

            return result;
        }

        protected void ProcessOutgoingMessageAsync_CCP(PositiveAssertionResponse1 message, CancellationToken cancellationToken)
        {


            MessageProtections appliedProtection = MessageProtections.None;

            foreach (IChannelBindingElement bindingElement in this.outgoingBindingElements)
            {
                Assumes.True(bindingElement.Channel != null);
                MessageProtections? elementProtection = bindingElement.ProcessOutgoingMessage(message, cancellationToken);
                if (elementProtection.HasValue)
                {
                    // Ensure that only one protection binding element applies to this message
                    // for each protection type.

                    if ((appliedProtection & elementProtection.Value) != 0)
                        Contract.Assume(false);

                    appliedProtection |= elementProtection.Value;
                }
            }
            message.EnsureValidMessage();
        }
    }
}


namespace DotNetOpenAuth.OpenId.Messages
{
    using System;
    using DotNetOpenAuth.OpenId.ChannelElements;
    using DotNetOpenAuth.Messaging;

    public class PositiveAssertionResponse1 : PositiveAssertionResponse
    {
        public PositiveAssertionResponse1() { }

        public void EnsureValidMessage()
        {
            this.VerifyReturnToMatchesRecipient();
        }  

        private void VerifyReturnToMatchesRecipient()
        {
            if (!(this.Recipient.Scheme == this.ReturnTo.Scheme))
                Contract.Assume(false);
            if (!(this.Recipient.Authority == this.ReturnTo.Authority))
                Contract.Assume(false);
            if (!(this.Recipient.AbsolutePath == this.ReturnTo.AbsolutePath))
                Contract.Assume(false);

        }
    }
}

namespace DotNetOpenAuth.Messaging{
	using System;
    using System.Collections.Generic;
	using System.Net.Http;
    using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.Messaging.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
	using Validation;


	public abstract partial class Channel1: Channel {        
        public IDirectedProtocolMessage ReadFromRequestAsync1_ccp(IProtocolMessage message, CancellationToken cancellationToken)
        {
            IDirectedProtocolMessage requestMessage = (IDirectedProtocolMessage)message;

           
            if (requestMessage != null)
            {
                this.ProcessIncomingMessageAsync1_ccp(requestMessage, cancellationToken);
            }
            

            return requestMessage;
 
        }

        
        protected virtual void ProcessIncomingMessageAsync1_ccp(IProtocolMessage message, CancellationToken cancellationToken)
        {

            MessageProtections appliedProtection = MessageProtections.None;
            foreach (IChannelBindingElement bindingElement in this.IncomingBindingElements)
            {
                MessageProtections? elementProtection = bindingElement.ProcessIncomingMessage(message, cancellationToken);
                if (elementProtection.HasValue)
                {
                    appliedProtection |= elementProtection.Value;
                }
            }

            //write to global object
            GlobalState.appliedProtection = appliedProtection;

            // Ensure that the message's protection requirements have been satisfied.
            int a = (int)((IndirectSignedResponse)message).RequiredProtection;
            int b = (int)appliedProtection;
            
            if ((a & b) != a)
            {
                Contract.Assume(false);
            }

            PositiveAssertionResponse1 _msg = (PositiveAssertionResponse1)message;
            if (!(_msg.ReturnTo.Scheme == HttpContext.Current.Request.Url.Scheme))
                Contract.Assume(false);
            
            if (!(_msg.ReturnTo.Authority == HttpContext.Current.Request.Url.Authority))
                Contract.Assume(false);

            if (!(_msg.ReturnTo.AbsolutePath == HttpContext.Current.Request.Url.AbsolutePath))
                Contract.Assume(false);
            
            if (!(_msg.ReturnTo == HttpContext.Current.Request.Url))
                Contract.Assume(false);
        }
        
	}
}


namespace DotNetOpenAuth.OpenId.RelyingParty
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using DotNetOpenAuth.Configuration;
    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.Messaging.Bindings;
    using DotNetOpenAuth.OpenId.ChannelElements;
    using DotNetOpenAuth.OpenId.Extensions;
    using DotNetOpenAuth.OpenId.Messages;

    public class PositiveAuthenticationResponse1 : PositiveAuthenticationResponse
    {
        static Picker p2;

        public PositiveAuthenticationResponse1(PositiveAssertionResponse response, OpenIdRelyingParty relyingParty) : base(response, relyingParty) { }

        public static PositiveAuthenticationResponse1 CreateAsync_ccp(
                PositiveAssertionResponse response, OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {

            var result = new PositiveAuthenticationResponse1(response, relyingParty);

            GlobalState.claimedEndPoint = result.Endpoint;

            result.VerifyDiscoveryMatchesAssertionAsync_ccp(relyingParty, cancellationToken);

            return result;
        }

        private void VerifyDiscoveryMatchesAssertionAsync_ccp(OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {

            Identifier claimedId = this.Response.ClaimedIdentifier;
          
            var discoveryResults = relyingParty.Discover(claimedId, cancellationToken);
            if (!discoveryResults.Contains(this.Endpoint))
                Contract.Assume(false);
            else
                GlobalState.actualEndPoint = this.Endpoint;
        }
    }


    public partial class OpenIdRelyingParty1 : OpenIdRelyingParty
    {

        static Picker p;

        public IAuthenticationResponse GetResponseAsync_ccp(IProtocolMessage msg, CancellationToken cancellationToken)
        {
            
            var message = ((Channel1)this.Channel).ReadFromRequestAsync1_ccp(msg, cancellationToken);
            PositiveAssertionResponse positiveAssertion;
            NegativeAssertionResponse negativeAssertion;
            IndirectSignedResponse positiveExtensionOnly;

           

            GlobalState.is_positive_assertion = positiveAssertion = message as PositiveAssertionResponse;

            if (GlobalState.is_positive_assertion != null)
            {
                var response = PositiveAuthenticationResponse1.CreateAsync_ccp(positiveAssertion, this, cancellationToken);

                ////Check for extensions
                //ClaimsResponse sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(response, true);

                return response;
            }
            else if ((negativeAssertion = message as NegativeAssertionResponse) != null)
            {
                return new NegativeAuthenticationResponse(negativeAssertion);
            }

            return null;
            
        }

    }
}



interface Picker
{
    int NondetInt();
    Channel1 NondetChannel1();
    Uri NondetUri();
    OpenIdRelyingParty1 NondetOpenIdRelyingParty1();
    string NondetString();
    HttpRequest NondetHttpRequest();
    ClaimsResponse NondetClaimsResponse();
    IdentifierDiscoveryResult NondetIdentifierDiscoveryResult();
    Identifier NondetIdentifier();
}


enum Identity
{
    Email,
    ClaimedId
};

class PoirotMain
{

    static PositiveAssertionResponse1 Auth_resp = new PositiveAssertionResponse1();
    static PositiveAssertionResponse1 SignIn_req = new PositiveAssertionResponse1();
    static PositiveAssertionResponse1 pas_nondet = new PositiveAssertionResponse1();
    static HttpResponseMessage result;
    static Channel1 c;
    static OpenIdRelyingParty1 rp = new OpenIdRelyingParty1();
    static Picker p;
    static IAuthenticationResponse result1;
    static string auth_req_sessionID, auth_req_realm;

    //This variable should be set by the developer to determine which identity we use
    static Identity identity;


    static void init()
    {
        c = p.NondetChannel1();
        Auth_resp.SessionID= auth_req_sessionID; 
        Contract.Assume (Auth_resp.ReturnTo.Authority== auth_req_realm);  //This is an assignment, but a property cannot be assigned.
        Auth_resp.Recipient = p.NondetUri(); Auth_resp.ReturnTo = p.NondetUri(); Auth_resp.ClaimedIdentifier = p.NondetIdentifier();
        rp = p.NondetOpenIdRelyingParty1();
        SignIn_req.Recipient = p.NondetUri(); SignIn_req.ReturnTo = p.NondetUri(); SignIn_req.ClaimedIdentifier = p.NondetIdentifier();
        pas_nondet.Recipient = p.NondetUri(); pas_nondet.ReturnTo = p.NondetUri(); pas_nondet.ClaimedIdentifier = p.NondetIdentifier();
        

        GlobalState.actualExt = p.NondetClaimsResponse();
        GlobalState.claimedExt = p.NondetClaimsResponse();

        GlobalState.actualEndPoint = p.NondetIdentifierDiscoveryResult();
        GlobalState.claimedEndPoint = p.NondetIdentifierDiscoveryResult();

        //Assume we are using email to authenticate
        identity = Identity.Email;
    }

    static PositiveAssertionResponse1 Get_ID_Assertion(string sessionID, string realm)
    {
        if (sessionID == Auth_resp.SessionID && realm == Auth_resp.ReturnTo.Authority)
            return Auth_resp;
        else
            return pas_nondet;
    }

    static void Main()
    {

        init();

        //initialization
        GlobalState.is_positive_assertion = new PositiveAssertionResponse();

        result = c.PrepareResponseAsync_CCP(Auth_resp);
        //Check for extensions
        ClaimsResponse sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg((IAuthenticationResponse)result, true);
        GlobalState.actualExt = sreg;

        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(Auth_resp.ClaimedIdentifier == SignIn_req.ClaimedIdentifier);

        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(SignIn_req.ReturnTo == Auth_resp.ReturnTo);

        CancellationToken x = default(CancellationToken);
        result1 = rp.GetResponseAsync_ccp(SignIn_req, x);
        sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(result1, true);
        GlobalState.claimedExt = sreg;
        //signature coverage
        if (identity == Identity.Email)
            Contract.Assume(GlobalState.actualExt.Email != null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);
        //RP check: Does the final returnTo field match our origin?
        //This is supplied by the developer of the convincee
        Contract.Assert(Auth_resp.ReturnTo == HttpContext.Current.Request.Url);

        //check for extension
        if (identity == Identity.Email)
            Contract.Assert(GlobalState.actualExt.Email != null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);
        else if (identity == Identity.ClaimedId)
            Contract.Assert(Auth_resp.ClaimedIdentifier == SignIn_req.ClaimedIdentifier);

        //RP check: verify claimed id resolves to the correct endpoint
        Contract.Assert(GlobalState.is_positive_assertion == null || GlobalState.claimedEndPoint == GlobalState.actualEndPoint);

        //shuo's assertion
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).ClaimedIdentifier == SignIn_req.ClaimedIdentifier);
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).ReturnTo == HttpContext.Current.Request.Url);
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).Recipient.Authority == HttpContext.Current.Request.Url.Authority);
    }
}