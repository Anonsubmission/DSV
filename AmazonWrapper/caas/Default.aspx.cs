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
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Code that runs when a new session is started

            //get the incoming request
            var context = HttpContext.Current;
            HttpRequest req = context.Request;

            RemotePost post = new RemotePost();
            post.FormName = "SimplePay";
            post.Url = "https://authorize.payments-sandbox.amazon.com/pba/paypipeline";
            post.Method = "POST";

            //get info from nopcommerce submission form
            NameValueCollection form = Request.Form;
            var items = form.AllKeys.SelectMany(form.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items)
            {
                if (item.key == "returnUrl" && form["path_digest"] == null)
                    post.Add(item.key, "http://localhost:38623/pay.aspx");
                else if (item.key == "returnUrl" && form["path_digest"] != null)
                    post.Add(item.key, "http://localhost:38623/pay.aspx?path_digest=" + form["path_digest"]);
                else if (item.key == "signature")
                    post.Add(item.key, AmazonHelper.SignParameters(post.Params,
                        "[secret key]", //simplePay secret key
                        post.Method,
                        "authorize.payments-sandbox.amazon.com",
                        "/pba/paypipeline"));
                else if (item.key == "path_digest") continue;
                else
                    post.Add(item.key, item.value);
            }

            /*
            post.Add("immediateReturn", "1");
            post.Add("signatureVersion", "2");
            post.Add("signatureMethod", "HmacSHA256");
            post.Add("accessKey", "[AWS access key]");
            post.Add("amount", "USD 35.12");
            post.Add("description", "Your store name, 9");
            post.Add("amazonPaymentsAccountId", "IGFCUTPWGXVM311K1E6QTXIQ1RPEIUG5PTIMUZ");
            post.Add("returnUrl", "http://localhost:8242/AmazonSimplePayReturn.aspx");
            post.Add("processImmediate", "0");
            post.Add("referenceId", "9");
            post.Add("signature", "e6nHOQqZCC3BMXZ1veEVIzWfu5SQxUIJ7O6v2wjdpQw=");
            */

            post.Post();

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect("/pay.aspx?orderID=1&gross=1&returnURL=http://isrc99b080:7000/finishOrder.aspx&symVal=TStore[[eeca0149e5fc0ef4cfc679951c072c0949fd1a1e()]]&signature=TStore-CD-ED-7E-14-C9-F7-D5-86-09-3A-75-1F-B0-83-9A-F5-38-35-EC-4B-15-DF-DF-9E-99-88-44-D5-7A-41-37-3E-A0-4C-E5-3A-16-7D-9C-D1-2E-3B-57-21-5D-7D-61-89");
        }
    }
}
