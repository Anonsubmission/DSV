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
        {
        }

        protected void Button1_Click(object sender, EventArgs e)
        {  
        }     
    }
}