//-----------------------------------------------------------------------
// <copyright file="FacebookClient.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.AspNet.Clients {
	using System;
    using System.Collections;
    using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Net;
	using System.Web;
	using DotNetOpenAuth.Messaging;
    using System.Text;
    using System.IO;
    using System.Diagnostics;

    using NopSolutions.NopCommerce.Common.Utils;
    using NopSolutions.NopCommerce.Payment.Methods.Amazon;

	/// <summary>
	/// The facebook client.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Facebook", Justification = "Brand name")]
	public sealed class FacebookClient : OAuth2Client {


        //ERIC'S CODE - begin
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "[dehaship]"; 
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";
        static string[] whitelist = new string[2] { "RP", "FB" };
        static string root = "C:\\CCP\\teamproject\\FacebookProofCode\\OpenAuth";

        protected static string HttpReq(string url, string post, string method, string refer = "")
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = false;
            request.Method = method;
            request.Referer = refer;

            if (method == "POST")
            {
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

            if (resp.IndexOf("Error") != -1)
            {
                Console.WriteLine(resp);
            }
            else
            {
                string[] split = resp.Split(new char[] { '|' });
                int i = resp.IndexOf('|');
                code = resp.Substring(i + 1); ;
            }

            return code;
        }

        //this function converts a piece of code to a hash
        public static string code_to_hash(string code)
        {

            foreach (KeyValuePair<string, string> entry in codeHashMap)
            {
                if (entry.Value == code)
                {
                    return entry.Key;
                }
            }

            //resp is in the format of OK|HASH or Error: ERROR MESSAGE
            string resp = HttpReq(dehash_server_host + upload_path, code, "POST");
            string hash = "";

            if (resp.IndexOf("Error") != -1)
            {
                Console.WriteLine(resp);
            }
            else
            {
                string[] split = resp.Split(new char[] { '|' });
                hash = split[1];
            }

            return hash;
        }



        public class transaction
        {
            public string party;
            public string function;
            public bool isProtected = true;
        }



        //this function parses the symval string and returns an array of functions called in reverse-chronological order
        // A[HASH1(B[[HASH2()]])]
        protected static Stack<transaction> parse_digest(string symval)
        {
            Stack<transaction> callStack = new Stack<transaction>();

            int step = 0, start = 0, end = symval.Length;
            int c = 0, s = 0;
            bool isSigned = false, isOuterLayerProtected = true;
            transaction cur = new transaction();

            while (start < end)
            {

                step = 0;
                c = (symval.IndexOf('(', start) == -1) ? int.MaxValue : symval.IndexOf('(', start);
                s = (symval.IndexOf('[', start) == -1) ? int.MaxValue : symval.IndexOf('[', start);

                if (c < s)
                { //if ( is before [
                    if (symval[c + 1] == '(')
                    {
                        step = 2; // double '('
                    }
                    else
                    {
                        step = 1;
                        if (!isSigned) isOuterLayerProtected = false; //A[[hash(protected)]], A[hash((protected))], A[hash(not protected)]
                    }

                    start += step;
                    end -= step;

                    if (!isOuterLayerProtected) cur.isProtected = false;

                    callStack.Push(cur);
                    isSigned = false;


                }
                else if (s < c)
                { //if [ is before (
                    cur = new transaction();
                    if (symval[s + 1] == '[') step = 2; // double '['
                    else step = 1;
                    if (c < int.MaxValue)
                    { // pattern: A[HASH(
                        isSigned = (step == 1) ? false : true; //A[[hash(signed)]], A[hash((insigned))]
                        cur.party = symval.Substring(start, s - start); //A[[HASH()]], a is party
                        cur.function = symval.Substring(s + step, c - s - step); //A[[HASH()]] HASH is function

                        //if one of the parties involved in the transaction is not known
                        if (!Array.Exists(whitelist, element => element == cur.party))
                        {
                            break;
                        }

                    }
                    start = c;
                    end -= step;

                }
                else
                { // the only case for this would be an invalid string
                    break;
                }
            }

            return callStack;
        }

        protected static string assemble_code(string path_digest)
        {

            Stack <transaction> callstack = parse_digest(path_digest);
            string code="";

            foreach (transaction trans in callstack)
            {
                if (trans.isProtected) code += hash_to_code(trans.function);
            }

            //assemble main
            code += @"
class PoirotMain
{
    static Picker p;

    static void Main()
    {
        FacebookClient1 oauth_client = new FacebookClient1();
        FacebookServer oauth_server = new FacebookServer();
        canonicalRequestResponse req1 = p.NondetRequestResponse();
        canonicalRequestResponse res1 = p.NondetRequestResponse();
        canonicalRequestResponse req2 = p.NondetRequestResponse();
        canonicalRequestResponse res2 = p.NondetRequestResponse();
";

            //TODO: dynamically generate this part using entry point functions
            code += @"
        req2 = oauth_client.QueryAccessToken_entry(res1);

        //we are not havocing the second request because it's not a redirection
        res2 = oauth_server.oauth_token_req(req2);

        oauth_client.GetUserData_entry(res2);
";

            code += @"
        //proto agnostic check
        Contract.Assert(OPAssertion.isSuccess);
        Contract.Assert(OPAssertion.rpid == RPStates.rpid);
        Contract.Assert(OPAssertion.return_url == RPStates.domain);
        Contract.Assert(OPAssertion.uid == RPStates.uid);

    }
}
";

            return code;

        }
        public static void generate_cs_file_from_symval(string path_digest)
        {

            TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));


            string content = assemble_code(path_digest);


            TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);


            using (StreamWriter outfile = new StreamWriter(root + "\\Program.cs"))
            {
                outfile.Write(content);
            }

        }

        
        public static bool checkLogicProperty()
        {
            TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));


            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = root + "\\run.bat";
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            //p.WaitForExit();

            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();


            TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);


            if (output.IndexOf("Program has no bugs") > 0)
                return true;
            else
                return false;
        }

        const string SourceCode_req1 = @"
//=============================== rp ================================================

//first RP request for code
namespace DotNetOpenAuth.AspNet.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Web;
    using DotNetOpenAuth.Messaging;
    using System.Text;
    using System.IO;

    /// <summary>
    /// The facebook client.
    /// </summary>
    public partial class FacebookClient1
    {

        public FacebookClient1()
        {
            this.appId = p.NondetString();
            this.appSecret = p.NondetString();
            RPGlobalState.appId = this.appId;
            RPGlobalState.appSecret = this.appSecret;
            RPGlobalState.return_uri = p.NondetString();

            //conversion to proto-agnostic data structures
            RPStates.domain = RPGlobalState.return_uri;
            RPStates.rpid = RPGlobalState.appId;

        }

        //entry point for the first request
        public canonicalRequestResponse GetServiceLoginUrl_entry()
        {
            //we are redefining the Uri objectin stub.cs
            Uri url= new Uri(RPGlobalState.return_uri);
            canonicalRequestResponse res = GetServiceLoginUrl(url);
            return res;
        }

        public canonicalRequestResponse GetServiceLoginUrl(Uri returnUrl)
        {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(AuthorizationEndpoint);
            builder.AppendQueryArgs(
                new Dictionary<string, string> {
					{ ""client_id"", this.appId },
					{ ""redirect_uri"", returnUrl.AbsoluteUri },
                    { ""path_digest"", ""RP[HASH()]""}
				});


            //[NON rp related code]
            canonicalRequestResponse res = new canonicalRequestResponse();
            res.client_id = this.appId;
            res.redirect_url = returnUrl.AbsoluteUri;

            //Contract.Assert(res.redirect_url == retu

            return res;
        }
    }

}
";

        const string SourceCode_req2 = @"

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
using Global;
using DotNetOpenAuth.AspNet.Clients;
using Facebook;

namespace Global
{
    public enum HTTPStatus : int
    {
        Success,
        Failure
    }

    public class canonicalRequestResponse
    {
        public string client_id;
        public string redirect_url;

        public string client_secret;
        public string code;
        public string token;

        public HTTPStatus status;

    }


    public static class MessagingUtilities1
    {
        //dummy function
        internal static void AppendQueryArgs(this UriBuilder builder, IEnumerable<KeyValuePair<string, string>> args)
        {
        }
    }

}

public class RPGlobalState
{
    //RP data
    public static string return_uri;
    public static string appId;
    public static string appSecret;
}


//second RP request for token
namespace DotNetOpenAuth.AspNet.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Web;
    using DotNetOpenAuth.Messaging;
    using System.Text;
    using System.IO;

    /// <summary>
    /// The facebook client.
    /// </summary>
    public partial class FacebookClient1
    {
        //[NON rp related code] initialization code
        static Picker p;
        private const string AuthorizationEndpoint = ""http://localhost:38623/oauth_req.aspx"";
        private const string TokenEndpoint = ""http://localhost:38623/accessToken_req.aspx"";
        private string appId;
        private string appSecret;

        // entry point for the second request
        public canonicalRequestResponse QueryAccessToken_entry(canonicalRequestResponse req)
        {
            //boogie hack -- uninitializes our vars
            this.appId = p.NondetString();
            this.appSecret = p.NondetString();
            RPGlobalState.appId = this.appId;
            RPGlobalState.appSecret = this.appSecret;
            RPGlobalState.return_uri = p.NondetString();

            //conversion to proto-agnostic data structures
            RPStates.domain = RPGlobalState.return_uri;
            RPStates.rpid = RPGlobalState.appId;
            // end of boogie hack

            //we are redefining the Uri objectin stub.cs
            Uri url = new Uri(RPGlobalState.return_uri);
            canonicalRequestResponse res = QueryAccessToken(url, req.code);
            return res;
        }

        public canonicalRequestResponse QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var builder = new UriBuilder(TokenEndpoint);
            builder.AppendQueryArgs(
                new Dictionary<string, string> {
					{ ""client_id"", this.appId },
					{ ""redirect_uri"", returnUrl.AbsoluteUri},
					{ ""client_secret"", this.appSecret },
					{ ""code"", authorizationCode }
				});

            //[NON rp related code]
            canonicalRequestResponse res = new canonicalRequestResponse();
            res.client_id = this.appId;
            res.redirect_url = returnUrl.AbsoluteUri;
            res.code = authorizationCode;

            return res;
        }

        public string getAccessToken(Uri url){
            using (WebClient client = new WebClient())
            {
                string data = client.DownloadString(url);

                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                var parsedQueryString = HttpUtility.ParseQueryString(data);
                return parsedQueryString[""access_token""];
            }
        }
    }

}
";
        const string SourceCode_req3 = @"
//final rp check
namespace DotNetOpenAuth.AspNet.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Web;
    using DotNetOpenAuth.Messaging;
    using System.Text;
    using System.IO;

    /// <summary>
    /// The facebook client.
    /// </summary>
    public partial class FacebookClient1
    {

        public void GetUserData_entry(canonicalRequestResponse req)
        {
            if (req.status == HTTPStatus.Failure) Contract.Assume(false);
            RPStates.uid = req.token;
        }
    }

}

";


		#region Constants and Fields

		/// <summary>
		/// The authorization endpoint.
		/// </summary>
        /// ERIC'S CODE - BEGIN
		//private const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";
        private const string AuthorizationEndpoint = "http://localhost:38623/oauth_req.aspx";
    

		/// <summary>
		/// The token endpoint.
		/// </summary>
		//private const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";
        private const string TokenEndpoint = "http://localhost:38623/accessToken_req.aspx";

		/// <summary>
		/// The _app id.
		/// </summary>
		private readonly string appId;

		/// <summary>
		/// The _app secret.
		/// </summary>
		private readonly string appSecret;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FacebookClient"/> class.
		/// </summary>
		/// <param name="appId">
		/// The app id.
		/// </param>
		/// <param name="appSecret">
		/// The app secret.
		/// </param>
		public FacebookClient(string appId, string appSecret)
			: base("facebook") {
			Requires.NotNullOrEmpty(appId, "appId");
			Requires.NotNullOrEmpty(appSecret, "appSecret");

			this.appId = appId;
			this.appSecret = appSecret;
		}

		#endregion

		#region Methods


		/// <summary>
		/// The get service login url.
		/// </summary>
		/// <param name="returnUrl">
		/// The return url.
		/// </param>
		/// <returns>An absolute URI.</returns>
		protected override Uri GetServiceLoginUrl(Uri returnUrl) {
			// Note: Facebook doesn't like us to url-encode the redirect_uri value
			var builder = new UriBuilder(AuthorizationEndpoint);
			builder.AppendQueryArgs(
				new Dictionary<string, string> {
					{ "client_id", this.appId },
					{ "redirect_uri", returnUrl.AbsoluteUri },
                    { "path_digest", "RP["+code_to_hash(SourceCode_req1)+"()]"}
				});
			return builder.Uri;
		}

		/// <summary>
		/// The get user data.
		/// </summary>
		/// <param name="accessToken">
		/// The access token.
		/// </param>
		/// <returns>A dictionary of profile data.</returns>
		protected override IDictionary<string, string> GetUserData(string accessToken) {
			FacebookGraphData graphData;
			var request =
				WebRequest.Create(
					"https://graph.facebook.com/me?access_token=" + MessagingUtilities.EscapeUriDataStringRfc3986(accessToken));
			using (var response = request.GetResponse()) {
				using (var responseStream = response.GetResponseStream()) {
					graphData = JsonHelper.Deserialize<FacebookGraphData>(responseStream);
				}
			}

			// this dictionary must contains 
			var userData = new Dictionary<string, string>();
			userData.AddItemIfNotEmpty("id", graphData.Id);
			userData.AddItemIfNotEmpty("username", graphData.Email);
			userData.AddItemIfNotEmpty("name", graphData.Name);
			userData.AddItemIfNotEmpty("link", graphData.Link == null ? null : graphData.Link.AbsoluteUri);
			userData.AddItemIfNotEmpty("gender", graphData.Gender);
			userData.AddItemIfNotEmpty("birthday", graphData.Birthday);
			return userData;
		}

		/// <summary>
		/// Obtains an access token given an authorization code and callback URL.
		/// </summary>
		/// <param name="returnUrl">
		/// The return url.
		/// </param>
		/// <param name="authorizationCode">
		/// The authorization code.
		/// </param>
		/// <returns>
		/// The access token.
		/// </returns>
		protected override string QueryAccessToken(Uri returnUrl, string authorizationCode) {
			// Note: Facebook doesn't like us to url-encode the redirect_uri value
			var builder = new UriBuilder(TokenEndpoint);
			builder.AppendQueryArgs(
				new Dictionary<string, string> {
					{ "client_id", this.appId },
					{ "redirect_uri", NormalizeHexEncoding(returnUrl.AbsoluteUri) },
					{ "client_secret", this.appSecret },
					{ "code", authorizationCode },
				});

			using (WebClient client = new WebClient()) {
				string data = client.DownloadString(builder.Uri);
                //string data = client.DownloadString("http://localhost:38623/accessToken_req.aspx?client_id=568762523166117&redirect_uri=http%3A%2F%2Flocalhost%3A30260%2FAccount%2FExternalLoginCallback%3F__provider__%3Dfacebook%26__sid__%3Da67ace0b82e94c9ab6863ab8f86aa19f&client_secret=26c4c0c3bdd21469328fd01fdb6512a4&code=AQBO0X8XRQ6a8u2K7QNTnlbnUU9m4lbj_XT8Ia35U9TNng3dahGTAiVG0oUVBCAS5sP0dDTs2G7kxa3mEduLlAEzL_b0_lXI0KBDktX__yBtFUrkkUPLQa4Y8uL_zDPfMHtefCGfxf4A0skZHhz5ecvSrLRHlwxnajDZDM9fIRaGkeAvhy0wgh93JERgwXQsg4zDoVSxdI8b6H4RpQ13vsBWzYJT9MkxkXYt_tubnZVXhK2O9N4-DZGwIyJfh-J993AlRo7Y-ocP-52hFczXFJJyXx0cHmv2poqeV5UKxVolPR-bpInGU9BLwg1VZbObHm5mu2HSclQwWtH0nCICiN8Q");
                if (string.IsNullOrEmpty(data)) {
					return null;
				}

				var parsedQueryString = HttpUtility.ParseQueryString(data);
				return parsedQueryString["access_token"];
			}
		}

        protected override string QueryAccessToken_CCP(Uri returnUrl, string authorizationCode, string path_digest)
        {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(TokenEndpoint);

            string new_hash = code_to_hash(SourceCode_req2);
            path_digest = "RP[" + new_hash + "((" + path_digest + "))]";

            builder.AppendQueryArgs(
                new Dictionary<string, string> {
					{ "client_id", this.appId },
					{ "redirect_uri", NormalizeHexEncoding(returnUrl.AbsoluteUri) },
					{ "client_secret", this.appSecret },
					{ "code", authorizationCode },
                    { "path_digest", path_digest}
				});

            using (WebClient client = new WebClient())
            {
                string data = client.DownloadString(builder.Uri);
                
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                var parsedQueryString = HttpUtility.ParseQueryString(data);

                //check path digest

                string digest = parsedQueryString["path_digest"];
                new_hash = code_to_hash(SourceCode_req3);
                digest = "RP[" + new_hash +"(("+digest +"))]";
                generate_cs_file_from_symval(digest);

                if (!checkLogicProperty()) return null;

                return parsedQueryString["access_token"];
            }
        }
        /// ERIC'S CODE - END

		/// <summary>
		/// Converts any % encoded values in the URL to uppercase.
		/// </summary>
		/// <param name="url">The URL string to normalize</param>
		/// <returns>The normalized url</returns>
		/// <example>NormalizeHexEncoding("Login.aspx?ReturnUrl=%2fAccount%2fManage.aspx") returns "Login.aspx?ReturnUrl=%2FAccount%2FManage.aspx"</example>
		/// <remarks>
		/// There is an issue in Facebook whereby it will rejects the redirect_uri value if
		/// the url contains lowercase % encoded values.
		/// </remarks>
		private static string NormalizeHexEncoding(string url) {
			var chars = url.ToCharArray();
			for (int i = 0; i < chars.Length - 2; i++) {
				if (chars[i] == '%') {
					chars[i + 1] = char.ToUpperInvariant(chars[i + 1]);
					chars[i + 2] = char.ToUpperInvariant(chars[i + 2]);
					i += 2;
				}
			}
			return new string(chars);
		}

		#endregion
	}
}
