using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;

using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Amazon;

namespace caas
{
    public partial class oauth_req : System.Web.UI.Page
    {

        //ERIC'S CODE
        string key_root = "C:\\CCP";
        GlobalState globalState;
        canonicalPayRequest req = new canonicalPayRequest();
        canonicalPayResponse res = new canonicalPayResponse();

        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://ericchen.me:81/"; //ERIC'S IP
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";
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

        public string SourceCode_oauth = @"
//=============================== op - FB ================================================

namespace Facebook
{
    public partial class FacebookServer
    {

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

            canonicalRequestResponse res = new canonicalRequestResponse();
            res.code = FBGlobalState.code;
            
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

            //get the incoming request
            var context = HttpContext.Current;
            HttpRequest req = context.Request;

            NameValueCollection parameters = new NameValueCollection(Request.QueryString);
            NameValueCollection new_parameters = new NameValueCollection();
            string url = "https://www.facebook.com/dialog/oauth";

            string redir_url = "", symT = "";
            var items = parameters.AllKeys.SelectMany(parameters.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items)
            {
                if (item.key == "redirect_uri")
                    redir_url = item.value;
                else if (item.key == "symT")
                    symT = item.value;
                else
                    new_parameters.Add(item.key, item.value);
            }

            string new_hash = code_to_hash(SourceCode_oauth);
            symT = "FB[" + new_hash + "(" + symT + ")]";
            redir_url += "&symT=" + symT;

            new_parameters.Add("redirect_uri", redir_url);

            string redirectLocation = url + "?" + ToQueryString(new_parameters);

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Redirect(redirectLocation);

            Response.End();

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect("/pay.aspx?orderID=1&gross=1&returnURL=http://isrc99b080:7000/finishOrder.aspx&symVal=TStore[[eeca0149e5fc0ef4cfc679951c072c0949fd1a1e()]]&signature=TStore-CD-ED-7E-14-C9-F7-D5-86-09-3A-75-1F-B0-83-9A-F5-38-35-EC-4B-15-DF-DF-9E-99-88-44-D5-7A-41-37-3E-A0-4C-E5-3A-16-7D-9C-D1-2E-3B-57-21-5D-7D-61-89");
        }
    }
}
