using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Collections;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.IO;

using System.Net;

using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Amazon;


namespace caas
{

    public class canonicalPayRequest
    {
        public int orderID;
        public int gross;
        public string returnURL;
        public string signer;
    }
    public class canonicalPayResponse
    {
        public string redirectionURL;
        public int orderID;
        public Status status;
        public int gross;
    }

    public partial class oauth_resp : System.Web.UI.Page
    {
        

        protected void Page_Load(object sender, EventArgs e)
        {/*
            String stringToSign = "";
            Page.EnableViewState = false;

            NameValueCollection parameters = new NameValueCollection(Request.QueryString);

            //we are not removing the redirect url because we need this for the access token msg
            string return_url = parameters["redirect_uri"];
            string old_hash = parameters["path_digest"];
            string new_hash = code_to_hash(SourceCode_Pay);
            string path_digest = "Facebook[[" + new_hash + "("+old_hash+")]]";
            parameters["path_digest"]= path_digest;
            
            Response.StatusCode = 302;
            Response.Status = "302 Moved Temporarily";
            Response.RedirectLocation = return_url;

            var items = parameters.AllKeys.SelectMany(parameters.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items)
            {
                Response.RedirectLocation += "&"+ HttpUtility.UrlEncode(item.key) + "=" + HttpUtility.UrlEncode(item.value);
                
            }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.End();
            */
        }

        protected void Button1_Click(object sender, EventArgs e)
        {

           
        }
   

     
    }
}