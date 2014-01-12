//-----------------------------------------------------------------------
// <copyright file="PositiveAssertionResponse.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId.Messages {
	using System;
    using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
    using System.Net;
	using System.Net.Security;
	using System.Text;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.Messaging.Bindings;
	using DotNetOpenAuth.OpenId.ChannelElements;
    using System.Web;
    using System.IO;


	/// <summary>
	/// An identity assertion from a Provider to a Relying Party, stating that the
	/// user operating the user agent is in fact some specific user known to the Provider.
	/// </summary>
	[DebuggerDisplay("OpenID {Version} {Mode} {LocalIdentifier}")]
	[Serializable]
    //ERIC'S CODE
	//internal class PositiveAssertionResponse : IndirectSignedResponse {
    public class PositiveAssertionResponse : IndirectSignedResponse
    {

        //ERIC'S CODE - begin
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://ericchen.me:81/"; //ERIC'S IP
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";

        protected static string HttpReq(string url, string post, string method, string refer = ""){
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = false;
            request.Method = method;
            request.Referer = refer;

            if (method == "POST"){
                byte[] postBytes = Encoding.ASCII.GetBytes(post);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postBytes.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();
            }


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());

            return sr.ReadToEnd();
        }


        //this function converts a code hash to its corresponding code
        public static string hash_to_code(string hash)
        {
            if (codeHashMap.ContainsKey(hash)) return codeHashMap[hash];

            //TODO: ask dehash server
            string resp = HttpReq(dehash_server_host + dehash_path + "?hash=" + hash, "", "GET");
            string code = "";

            if (resp.IndexOf("Error") != -1){
                Console.WriteLine(resp);
            }else{
                string[] split = resp.Split(new char[] { '|' });
                int i = resp.IndexOf('|');
                code = resp.Substring(i + 1); ;
            }

            return code;
        }

        //this function converts a piece of code to a hash
        public static string code_to_hash(string code){

            foreach (KeyValuePair<string, string> entry in codeHashMap){
                if (entry.Value == code){
                    return entry.Key;
                }
            }

            //resp is in the format of OK|HASH or Error: ERROR MESSAGE
            string resp = HttpReq(dehash_server_host + upload_path, code, "POST");
            string hash = "";

            if (resp.IndexOf("Error") != -1){
                Console.WriteLine(resp);
            }else{
                string[] split = resp.Split(new char[] { '|' });
                hash = split[1];
            }

            return hash;
        }

        public static string SourceCode_OP = @"
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
";


        //ERIC'S CODE - end

        //shuo:begin
        public PositiveAssertionResponse() { }
        //shuo:end

        public static string hashvalue_op;

		/// <summary>
		/// Initializes a new instance of the <see cref="PositiveAssertionResponse"/> class.
		/// </summary>
		/// <param name="request">
		/// The authentication request that caused this assertion to be generated.
		/// </param>
        /// ERIC'S CODE
		//internal PositiveAssertionResponse(CheckIdRequest request)
        public PositiveAssertionResponse(CheckIdRequest request)
			: base(request) {

            //ERIC'S CODE - This is where we hash the code
            hashvalue_op = code_to_hash(SourceCode_OP);

			this.ClaimedIdentifier = request.ClaimedIdentifier;
			this.LocalIdentifier = request.LocalIdentifier;
		}



		/// <summary>
		/// Initializes a new instance of the <see cref="PositiveAssertionResponse"/> class
		/// for unsolicited assertions.
		/// </summary>
		/// <param name="version">The OpenID version to use.</param>
		/// <param name="relyingPartyReturnTo">The return_to URL of the Relying Party.
		/// This value will commonly be from <see cref="SignedResponseRequest.ReturnTo"/>,
		/// but for unsolicited assertions may come from the Provider performing RP discovery
		/// to find the appropriate return_to URL to use.</param>
        /// ERIC'S CODE
		//internal PositiveAssertionResponse(Version version, Uri relyingPartyReturnTo)
        public PositiveAssertionResponse(Version version, Uri relyingPartyReturnTo)
			: base(version, relyingPartyReturnTo) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PositiveAssertionResponse"/> class.
		/// </summary>
		/// <param name="relyingParty">The relying party return_to endpoint that will receive this positive assertion.</param>
        /// ERIC'S CODE
		//internal PositiveAssertionResponse(RelyingPartyEndpointDescription relyingParty)
        public PositiveAssertionResponse(RelyingPartyEndpointDescription relyingParty)
			: this(relyingParty.Protocol.Version, relyingParty.ReturnToEndpoint) {
		}

		/// <summary>
		/// Gets or sets the Claimed Identifier.
		/// </summary>
		/// <remarks>
		/// <para>"openid.claimed_id" and "openid.identity" SHALL be either both present or both absent. 
		/// If neither value is present, the assertion is not about an identifier, 
		/// and will contain other information in its payload, using extensions (Extensions). </para>
		/// </remarks>
		[MessagePart("openid.claimed_id", IsRequired = true, AllowEmpty = false, RequiredProtection = ProtectionLevel.Sign, MinVersion = "2.0")]
        //ERIC'S CODE
		//internal Identifier ClaimedIdentifier { get; set; }
        public Identifier ClaimedIdentifier { get; set; }

		/// <summary>
		/// Gets or sets the OP Local Identifier.
		/// </summary>
		/// <value>The OP-Local Identifier. </value>
		/// <remarks>
		/// <para>OpenID Providers MAY assist the end user in selecting the Claimed 
		/// and OP-Local Identifiers about which the assertion is made. 
		/// The openid.identity field MAY be omitted if an extension is in use that 
		/// makes the response meaningful without it (see openid.claimed_id above). </para>
		/// </remarks>
		[MessagePart("openid.identity", IsRequired = true, AllowEmpty = false, RequiredProtection = ProtectionLevel.Sign)]
		//ERIC'S CODE
        //internal Identifier LocalIdentifier { get; set; }
        public Identifier LocalIdentifier { get; set; }



        //ERIC'S CODE note the RequiredProtection field has to be signed
        [MessagePart("openid.symval", IsRequired = true, AllowEmpty = false, RequiredProtection = ProtectionLevel.Sign)]
        public string SymvalIdentifier { get; set; }
        
	}
}
