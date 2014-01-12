namespace DotNetOpenAuth.Messaging {
	using System;
	using System.Collections.Generic;
	/*using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Diagnostics.Contracts;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Cache;*/
	using System.Net.Http;
	//using System.Net.Http.Headers;
	/*using System.Net.Mime;
	using System.Net.Sockets;
	using System.Runtime.Serialization.Json;
	using System.Text;*/
	using System.Threading;
	using System.Threading.Tasks;
	using System.Web;
	//using System.Xml;
	using DotNetOpenAuth.Messaging.Reflection;
	using Validation;

	public abstract partial class Channel : IDisposable {
		
        //ERIC'S CODE
        public Channel() { }

		public async Task<HttpResponseMessage> PrepareResponseAsync_CCP(IProtocolMessage message, CancellationToken cancellationToken = default(CancellationToken)) {
			Requires.NotNull(message, "message");

			await this.ProcessOutgoingMessageAsync_CCP(message, cancellationToken);
			HttpResponseMessage result;
			var directedMessage = message as IDirectedProtocolMessage;
			result = this.PrepareIndirectResponse(directedMessage);
            return result;
		}

        protected async Task ProcessOutgoingMessageAsync_CCP(IProtocolMessage message, CancellationToken cancellationToken)
        {
            Requires.NotNull(message, "message");

            MessageProtections appliedProtection = MessageProtections.None;
            
            foreach (IChannelBindingElement bindingElement in this.outgoingBindingElements)
            {
                Assumes.True(bindingElement.Channel != null);
                MessageProtections? elementProtection = await bindingElement.ProcessOutgoingMessageAsync(message, cancellationToken);
                if (elementProtection.HasValue)
                {
                    // Ensure that only one protection binding element applies to this message
                    // for each protection type.
                    ErrorUtilities.VerifyProtocol((appliedProtection & elementProtection.Value) == 0, MessagingStrings.TooManyBindingsOfferingSameProtection, elementProtection.Value);
                    appliedProtection |= elementProtection.Value;
                }
            }
            
            message.EnsureValidMessage();

        }


        public async Task<IDirectedProtocolMessage> ReadFromRequestAsync_ccp(IProtocolMessage message, CancellationToken cancellationToken)
        {

            IDirectedProtocolMessage requestMessage = (IDirectedProtocolMessage)message;

            if (requestMessage != null)
            {
                await this.ProcessIncomingMessageAsync_ccp(requestMessage, cancellationToken);
            }

            return requestMessage;
        }


        protected virtual async Task ProcessIncomingMessageAsync_ccp(IProtocolMessage message, CancellationToken cancellationToken)
        {
            
            MessageProtections appliedProtection = MessageProtections.None;
            foreach (IChannelBindingElement bindingElement in this.IncomingBindingElements)
            {
                Assumes.True(bindingElement.Channel != null); // CC bug: this.IncomingBindingElements ensures this... why must we assume it here?
                MessageProtections? elementProtection = await bindingElement.ProcessIncomingMessageAsync(message, cancellationToken);
                if (elementProtection.HasValue)
                {
                    appliedProtection |= elementProtection.Value;
                }
            }

            // Ensure that the message's protection requirements have been satisfied.
            if ((message.RequiredProtection & appliedProtection) != message.RequiredProtection)
            {
                throw new UnprotectedMessageException(message, appliedProtection);
            }

            // We do NOT verify that all required message parts are present here... the 
            // message deserializer did for us.  It would be too late to do it here since
            // they might look initialized by the time we have an IProtocolMessage instance.
            message.EnsureValidMessage();
        }

	}
}
