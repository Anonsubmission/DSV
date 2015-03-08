using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;

using System.Collections;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace caas
{
    public partial class accessToken_req : System.Web.UI.Page
    {

        public string SourceCode_token = @"
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
    public partial class FacebookServer
    {

        static Picker p;
        
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
";

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            return string.Join("&", array);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Code that runs when a new session is started

            string url = "https://graph.facebook.com/oauth/access_token";

            NameValueCollection query = new NameValueCollection(Request.QueryString);
            string symT = query["symT"];
            query.Remove("symT");
            string hashed_code = oauth_req.code_to_hash(SourceCode_token);
            symT = "FB[" + hashed_code + "((" + symT + "))]";

            string full_uri = url+"?"+ToQueryString(query);

            using (WebClient client = new WebClient())
            {
                string data = client.DownloadString(full_uri);
                if (string.IsNullOrEmpty(data))
                {
                    Response.Write(null);
                }

                var parsedQueryString = HttpUtility.ParseQueryString(data);
                parsedQueryString.Add("symT", symT);

                Response.Write(ToQueryString(parsedQueryString));

            }

            Response.End();
        }
    }
}