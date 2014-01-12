namespace DotNetOpenAuth.OpenId.Messages
{
    using System;
    using DotNetOpenAuth.OpenId.ChannelElements;
    using DotNetOpenAuth.Messaging;
  
    public partial class IndirectSignedResponse : IndirectResponseBase, ITamperResistantOpenIdMessage
    {
		public override void EnsureValidMessage() {
			this.VerifyReturnToMatchesRecipient();
		} 

        private void VerifyReturnToMatchesRecipient()
        {
            ErrorUtilities.VerifyProtocol(
                string.Equals(this.Recipient.Scheme, this.ReturnTo.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Recipient.Authority, this.ReturnTo.Authority, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Recipient.AbsolutePath, this.ReturnTo.AbsolutePath, StringComparison.Ordinal),
                OpenIdStrings.ReturnToParamDoesNotMatchRequestUrl,
                Protocol.openid.return_to,
                this.ReturnTo,
                this.Recipient);
        }
    }
}
