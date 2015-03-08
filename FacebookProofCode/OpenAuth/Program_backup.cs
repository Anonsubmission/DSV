
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

//=============================== rp ================================================
public class RPGlobalState
{
    //RP data
    public static string return_uri;
    public static string appId;
    public static string appSecret;
}

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
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Facebook", Justification = "Brand name")]
    public partial class FacebookClient1
    {
        

        //[NON rp related code] initialization code
        static Picker p;
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
					{ "client_id", this.appId },
					{ "redirect_uri", returnUrl.AbsoluteUri },
                    { "path_digest", "RP[HASH()]"}
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
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Facebook", Justification = "Brand name")]
    public partial class FacebookClient1
    {

        private const string AuthorizationEndpoint = "http://localhost:38623/oauth_req.aspx";
        private const string TokenEndpoint = "http://localhost:38623/accessToken_req.aspx";
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
					{ "client_id", this.appId },
					{ "redirect_uri", returnUrl.AbsoluteUri},
					{ "client_secret", this.appSecret },
					{ "code", authorizationCode }
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
                return parsedQueryString["access_token"];
            }
        }
    }

}

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
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Facebook", Justification = "Brand name")]
    public partial class FacebookClient1
    {

        public void GetUserData_entry(canonicalRequestResponse req)
        {
            if (req.status == HTTPStatus.Failure) Contract.Assume(false);
            RPStates.uid = req.token;
        }
    }

}


//=============================== op - FB ================================================
public class FBGlobalState
{
    //facebook data
    public static string code;
    public static string access_token;
    public static int user_id;

    //data recieved from RP
    public static string return_url;
    public static string client_id;
}


interface Picker
{
    int NondetInt();
    string NondetString();
    canonicalRequestResponse NondetRequestResponse();
    Boolean NondetBool();
}

namespace Facebook
{
    public class FacebookServer
    {

        static Picker p;
        
        public canonicalRequestResponse oauth_code_req(canonicalRequestResponse req)
        {
            FBGlobalState.code = p.NondetString();
            FBGlobalState.access_token = p.NondetString();
            FBGlobalState.user_id = p.NondetInt();
            FBGlobalState.return_url = req.redirect_url;
            FBGlobalState.client_id = req.client_id;

            //conversion to proto-agnostic data structures
            OPAssertion.uid = FBGlobalState.access_token;
            OPAssertion.rpid = req.client_id;
            OPAssertion.return_url = req.redirect_url;
            OPAssertion.isSuccess = true;

            Contract.Assert(OPAssertion.rpid == RPStates.rpid);
            Contract.Assert(OPAssertion.return_url == RPStates.domain);

            canonicalRequestResponse res = new canonicalRequestResponse();
            res.code = FBGlobalState.code;
            
            return res;
        }

        public canonicalRequestResponse oauth_token_req(canonicalRequestResponse req)
        {

            canonicalRequestResponse res = new canonicalRequestResponse();

            //ideally we should leave them uninitialized but it doesnt work for boogie check
            FBGlobalState.code = p.NondetString();
            FBGlobalState.access_token = p.NondetString();
            FBGlobalState.user_id = p.NondetInt();
            FBGlobalState.return_url = p.NondetString();
            FBGlobalState.client_id = p.NondetString();

            //conversion to proto-agnostic data structures
            OPAssertion.uid = FBGlobalState.access_token;
            OPAssertion.rpid = FBGlobalState.client_id;
            OPAssertion.return_url = FBGlobalState.return_url;
            OPAssertion.isSuccess = p.NondetBool();



            if (req.code != FBGlobalState.code)
                res.status = HTTPStatus.Failure;
            else if (req.redirect_url != FBGlobalState.return_url)
                res.status = HTTPStatus.Failure;
            else if (req.client_id != FBGlobalState.client_id)
                res.status = HTTPStatus.Failure;
            else if (!OPAssertion.isSuccess)
                res.status = HTTPStatus.Failure;
            else{
                res.status = HTTPStatus.Success;
                res.token = FBGlobalState.access_token;
            }

            return res;
        }

    }

}



class PoirotMain
{
    static Picker p;

    static void Main()
    {
        FacebookClient1 oauth_client = new FacebookClient1();
        FacebookServer oauth_server = new FacebookServer(); 

        canonicalRequestResponse req1 = oauth_client.GetServiceLoginUrl_entry();

        //I'm not entirely sure if this is correct, but I think we don't have to havoc the firest req?
        canonicalRequestResponse res1 = oauth_server.oauth_code_req(req1);

        //we are havocing the response because it is not encrypted
        res1 = p.NondetRequestResponse();
        canonicalRequestResponse req2 = oauth_client.QueryAccessToken_entry(res1);

        //we are not havocing the second request because it's not a redirection
        canonicalRequestResponse res2 = oauth_server.oauth_token_req(req2);

        //oauth_client.GetUserData_entry(res2);

        //proto agnostic check
        Contract.Assert(OPAssertion.isSuccess);
        Contract.Assert(OPAssertion.rpid == RPStates.rpid);
        Contract.Assert(OPAssertion.return_url == RPStates.domain);
        Contract.Assert(OPAssertion.uid == RPStates.uid);

       

    }
}