
//ERIC'S CODE - begin
namespace DotNetOpenAuth.OpenId.RelyingParty
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using DotNetOpenAuth.Configuration;
    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.Messaging.Bindings;
    using DotNetOpenAuth.OpenId.ChannelElements;
    using DotNetOpenAuth.OpenId.Extensions;
    using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
    using DotNetOpenAuth.OpenId.Messages;
    //using DotNetOpenAuth.Test.OpenId.Extensions;
    using DotNetOpenAuth.OpenId.RelyingParty.Extensions;
    using Validation;

    //ERIC'S CODE
    //internal partial class PositiveAuthenticationResponse
    public partial class PositiveAuthenticationResponse
    {
        public static string SourceCode_RP = @"
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

";



        public static async Task<PositiveAuthenticationResponse> CreateAsync_ccp(
                PositiveAssertionResponse response, OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {
            var result = new PositiveAuthenticationResponse(response, relyingParty);
            await result.VerifyDiscoveryMatchesAssertionAsync_ccp(relyingParty, cancellationToken);
            return result;
        }

        //ERIC'S CODE
        //private async Task VerifyDiscoveryMatchesAssertionAsync_ccp(OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        public async Task VerifyDiscoveryMatchesAssertionAsync_ccp(OpenIdRelyingParty relyingParty, CancellationToken cancellationToken)
        {
            Logger.OpenId.Debug("Verifying assertion matches identifier discovery results...");

            // Ensure that we abide by the RP's rules regarding RequireSsl for this discovery step.
            Identifier claimedId = this.Response.ClaimedIdentifier;
            /*
            if (relyingParty.SecuritySettings.RequireSsl)
            {
                if (!claimedId.TryRequireSsl(out claimedId))
                {
                    Logger.OpenId.ErrorFormat("This site is configured to accept only SSL-protected OpenIDs, but {0} was asserted and must be rejected.", this.Response.ClaimedIdentifier);
                    ErrorUtilities.ThrowProtocol(OpenIdStrings.RequireSslNotSatisfiedByAssertedClaimedId, this.Response.ClaimedIdentifier);
                }
            }
            */

            // Check whether this particular identifier presents a problem with HTTP discovery
            // due to limitations in the .NET Uri class.
            UriIdentifier claimedIdUri = claimedId as UriIdentifier;

            /*
            if (claimedIdUri != null && claimedIdUri.ProblematicNormalization)
            {
                ErrorUtilities.VerifyProtocol(relyingParty.SecuritySettings.AllowApproximateIdentifierDiscovery, OpenIdStrings.ClaimedIdentifierDefiesDotNetNormalization);
                Logger.OpenId.WarnFormat("Positive assertion for claimed identifier {0} cannot be precisely verified under partial trust hosting due to .NET limitation.  An approximate verification will be attempted.", claimedId);
            }
            */
            var discoveryResults = await relyingParty.DiscoverAsync(claimedId, cancellationToken);
            ErrorUtilities.VerifyProtocol(
                discoveryResults.Contains(this.Endpoint),
                OpenIdStrings.IssuedAssertionFailsIdentifierDiscovery,
                this.Endpoint,
                discoveryResults.ToStringDeferred(true));
        }
    }


    public partial class OpenIdRelyingParty
    {
        /// <summary>
        /// Gets an authentication response from a Provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The processed authentication response if there is any; <c>null</c> otherwise.
        /// </returns>
        /// <remarks>
        /// Requires an <see cref="HttpContext.Current">HttpContext.Current</see> context.
        /// </remarks>
        public async Task<IAuthenticationResponse> GetResponseAsync_ccp(IProtocolMessage msg,  CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var message = await this.Channel.ReadFromRequestAsync_ccp(msg, cancellationToken);
                PositiveAssertionResponse positiveAssertion;
                NegativeAssertionResponse negativeAssertion;
                IndirectSignedResponse positiveExtensionOnly;
                if ((positiveAssertion = message as PositiveAssertionResponse) != null)
                {
                    // We need to make sure that this assertion is coming from an endpoint
                    // that the host deems acceptable.
                    var providerEndpoint = new SimpleXrdsProviderEndpoint(positiveAssertion);
                    ErrorUtilities.VerifyProtocol(
                        this.FilterEndpoint(providerEndpoint),
                        OpenIdStrings.PositiveAssertionFromNonQualifiedProvider,
                        providerEndpoint.Uri);

                    var response = await PositiveAuthenticationResponse.CreateAsync_ccp(positiveAssertion, this, cancellationToken);
                    foreach (var behavior in this.Behaviors)
                    {
                        behavior.OnIncomingPositiveAssertion(response);
                    }

                    var sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(response, true);
                    Debug.Assert(sreg.Email != null, "No email field!");

                    return response;
                }
                else if ((positiveExtensionOnly = message as IndirectSignedResponse) != null)
                {
                    return new PositiveAnonymousResponse(positiveExtensionOnly);
                }
                else if ((negativeAssertion = message as NegativeAssertionResponse) != null)
                {
                    return new NegativeAuthenticationResponse(negativeAssertion);
                }

                return null;
            }
            catch (ProtocolException ex)
            {
                return new FailedAuthenticationResponse(ex);
            }
        }

        

    }
}

//ERIC'S CODE - end
