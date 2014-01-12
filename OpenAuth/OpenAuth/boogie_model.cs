
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
            GloabalState.appliedProtection = appliedProtection;

            // Ensure that the message's protection requirements have been satisfied.
            int a = (int)((IndirectSignedResponse)message).RequiredProtection;
            int b = (int)appliedProtection;
            
            if ((a & b) != a)
            {
                Contract.Assume(false);
            }

            PositiveAssertionResponse1 _msg = (PositiveAssertionResponse1)message;
            _msg.EnsureValidMessage();
            
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
        public PositiveAuthenticationResponse1(PositiveAssertionResponse response, OpenIdRelyingParty relyingParty) : base(response, relyingParty) { }

        public static PositiveAuthenticationResponse1 CreateAsync_ccp(
                PositiveAssertionResponse response, OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {

            var result = new PositiveAuthenticationResponse1(response, relyingParty);
            result.VerifyDiscoveryMatchesAssertionAsync_ccp(relyingParty, cancellationToken);

            //Contract.Assert(false);

            return result;
        }

        private void VerifyDiscoveryMatchesAssertionAsync_ccp(OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {

            Identifier claimedId = this.Response.ClaimedIdentifier;
          
            var discoveryResults = relyingParty.Discover(claimedId, cancellationToken);
            if (!discoveryResults.Contains(this.Endpoint))
                Contract.Assume(false);

            GloabalState.is_endpoint_discovered = true;
        }
    }


    public partial class OpenIdRelyingParty1 : OpenIdRelyingParty
    {
        
        public IAuthenticationResponse GetResponseAsync_ccp(IProtocolMessage msg, CancellationToken cancellationToken)
        {
            
            var message = ((Channel1)this.Channel).ReadFromRequestAsync1_ccp(msg, cancellationToken);
            PositiveAssertionResponse positiveAssertion;
            NegativeAssertionResponse negativeAssertion;
            IndirectSignedResponse positiveExtensionOnly;

            GloabalState.is_positive_assertion = positiveAssertion = message as PositiveAssertionResponse;

            if (GloabalState.is_positive_assertion != null)
            {
                var response = PositiveAuthenticationResponse1.CreateAsync_ccp(positiveAssertion, this, cancellationToken);
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

    static PositiveAssertionResponse1 pas = new PositiveAssertionResponse1();
    static PositiveAssertionResponse1 pas1 = new PositiveAssertionResponse1();
    static HttpResponseMessage result;
    static Channel1 c;
    static OpenIdRelyingParty1 rp = new OpenIdRelyingParty1();
    static Picker p;
    static IAuthenticationResponse result1;

    //This variable should be set by the developer to determine which identity we use
    static Identity identity;


    static void init()
    {
        c = p.NondetChannel1();
        pas.Recipient = p.NondetUri(); pas.ReturnTo = p.NondetUri(); pas.ClaimedIdentifier = p.NondetIdentifier();
        rp = p.NondetOpenIdRelyingParty1();
        pas1.Recipient = p.NondetUri(); pas1.ReturnTo = p.NondetUri(); pas1.ClaimedIdentifier = p.NondetIdentifier();

        GlobalState.actualExt = p.NondetClaimsResponse();
        GlobalState.claimedExt = p.NondetClaimsResponse();

        GlobalState.actualEndPoint = p.NondetIdentifierDiscoveryResult();
        GlobalState.claimedEndPoint = p.NondetIdentifierDiscoveryResult();

        //Assume we are using email to authenticate
        identity = Identity.Email;
    }

     
    static void Main()
    {
  
        init();

        //initialization
        GlobalState.is_positive_assertion = new PositiveAssertionResponse();

        result = c.PrepareResponseAsync_CCP(pas);
        //Check for extensions
        ClaimsResponse sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg((IAuthenticationResponse)result, true);
        GlobalState.actualExt = sreg;

        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(pas.ClaimedIdentifier == pas1.ClaimedIdentifier);

        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(pas1.ReturnTo == pas.ReturnTo);

        result1= rp.GetResponseAsync_ccp(pas1, x);
        sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(result1, true);
        GlobalState.claimedExt = sreg;
        //signature coverage
        if (identity == Identity.Email)
            Contract.Assume(GlobalState.actualExt.Email!=null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);
        //RP check: Does the final returnTo field match our origin?
        //This is supplied by the developer of the convincee
        Contract.Assert(pas.ReturnTo == HttpContext.Current.Request.Url);

        //check for extension
        if (identity == Identity.Email)
            Contract.Assert(GlobalState.actualExt.Email!=null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);
        else if(identity == Identity.ClaimedId)
            Contract.Assert(pas.ClaimedIdentifier == pas1.ClaimedIdentifier);

        //RP check: verify claimed id resolves to the correct endpoint
        Contract.Assert(GlobalState.is_positive_assertion == null || GlobalState.claimedEndPoint == GlobalState.actualEndPoint);
    }
}