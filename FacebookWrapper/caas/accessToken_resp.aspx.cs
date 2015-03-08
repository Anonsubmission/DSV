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
    public partial class accessToken_resp : System.Web.UI.Page
    {
        

        public string SourceCode_Pay = @"
namespace NopSolutions.NopCommerce.Payment.Methods.Amazon
{
    using NopSolutions.NopCommerce.Web;
    
    public class CaaS
    {
        public canonicalRequestResponse pay(canonicalRequestResponse req)
        {
            canonicalRequestResponse res = new canonicalRequestResponse();
            res.gross = req.gross;
            res.orderID = req.orderID;

            res.status = CaasReturnStatus.Sucess;

            // if the request is signed then the previous res.payee will be assigned to this req.payee in Main()
            // this line of code simply propagates the payee info to the response
            res.payee = req.payee;

            //not sure whether this field is important
            res.MerchantReturnURL = req.MerchantReturnURL;

            return res;
        }
    }
}
";

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}