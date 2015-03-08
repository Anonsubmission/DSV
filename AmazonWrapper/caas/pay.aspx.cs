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

    public partial class pay : System.Web.UI.Page
    {
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

        public string SourceCode_Pay = @"

namespace NopSolutions.NopCommerce.Payment.Methods.Amazon
{
    using NopSolutions.NopCommerce.Web;
//shuo:begin
    public class payment_record
    {
        public int gross;
        public int orderID;
        public CaasReturnStatus status;
        public string payee;
    }
    public class caas_state
    {
        public payment_record[] payments;
        //public int count;
        public caas_state(int m)
        {
            //count = m;
            payments = new payment_record[m];
        }
    }
//shuo:end
    public class CaaS
    {
       //shuo:begin
        static Picker p;
        public caas_state caas = new caas_state(100);

        public  void SQLStub_insertPayment(int gross, int orderID, CaasReturnStatus status, string payee)
        {
            
            int i;
            i = p.NondetInt();
            Contract.Assume(0 <= i && i < caas.payments.Length);
            GlobalState.witness = i;

           // i = 88;
            caas.payments[i] = new payment_record();
            Contract.Assume(caas.payments[i].gross==gross);   //this is because of the decimal complication
            caas.payments[i].orderID=orderID;
            caas.payments[i].status = status;
            caas.payments[i].payee = payee;
        }
        public canonicalRequestResponse pay(canonicalRequestResponse req)
        {
              Contract.Assert(req.gross == GlobalState.tstore.orders[req.orderID].gross);
              
            canonicalRequestResponse res = new canonicalRequestResponse();
           
            res.gross = req.gross;
            res.orderID = req.orderID;

            res.status = CaasReturnStatus.Sucess;

            // if the request is signed then the previous res.payee will be assigned to this req.payee in Main()
            // this line of code simply propagates the payee info to the response
            res.payee = req.signer;

            //not sure whether this field is important
            res.MerchantReturnURL = req.MerchantReturnURL;

            //shuo:begin
            SQLStub_insertPayment(res.gross, res.orderID, res.status, res.payee);

            //shuo:end

            return res;
        }
    }
}


";

        protected void Page_Load(object sender, EventArgs e)
        {
            String stringToSign = "";

            Page.EnableViewState = false;
            UrlLabel.Text = Server.UrlDecode(Request.Path + "?" + Request.QueryString);

            // Code that runs when a new session is started
            string returnUrl = "http://localhost:8242/AmazonSimplePayReturn.aspx";

            NameValueCollection parameters = new NameValueCollection(Request.QueryString);

            string old_hash = parameters["symT"];
            string new_hash = code_to_hash(SourceCode_Pay);
            string symT = "CaaS[[" + new_hash + "("+old_hash+")]]";
            parameters["symT"]= symT;

            //prepare the sig
            CspParameters cspParams = null;
            RSACryptoServiceProvider rsaProvider = null;
            StreamReader privateKeyFile = null;
            string privateKeyText = "";
            byte[] signatureBytes = null;
            byte[] plainBytes = null;
            string sig = "";

            
            cspParams = new CspParameters();
            cspParams.ProviderType = 1;// PROV_RSA_FULL;
            rsaProvider = new RSACryptoServiceProvider(384, cspParams);

            // Read public key from file
            privateKeyFile = File.OpenText(key_root + "\\RSAKeys\\prikey_CaaS.xml");
            privateKeyText = privateKeyFile.ReadToEnd();

            // Import public key
            rsaProvider.FromXmlString(privateKeyText);
            parameters.Remove("signature");
            //parameters["status"] = "PF";
            stringToSign = AmazonHelper.CalculateSignV2(parameters, "GET", "localhost:8242", "/AmazonSimplePayReturn.aspx");

            try
            {
                // Encrypt plain text
                plainBytes = Encoding.Unicode.GetBytes(stringToSign);
                signatureBytes = rsaProvider.SignData(plainBytes, new SHA1CryptoServiceProvider());

                sig = Convert.ToBase64String(signatureBytes, 0, signatureBytes.Length);
            }
            catch (Exception ex)
            {
                Response.Write("Fail to sign.");
            }

            //Note that localhost:8243 and AmazonSimplePayReturn.aspx are hardcoded right now, idealy we want them to be dynamically inserted
            Response.StatusCode = 302;
            Response.Status = "302 Moved Temporarily";
            Response.RedirectLocation = returnUrl + "?";// +stringToSign + "&signature=" + parameters["signature"];

            var items = parameters.AllKeys.SelectMany(parameters.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items)
            {
                Response.RedirectLocation += HttpUtility.UrlEncode(item.key) + "=" + HttpUtility.UrlEncode(item.value) + "&";
                
            }
            Response.RedirectLocation += "signature=" + HttpUtility.UrlEncode(sig);
            
            Response.End();
            
        }

        protected void parse(NameValueCollection pColl, string signer)
        {
            req.orderID = Convert.ToInt32(pColl["orderID"]);
            req.gross = Convert.ToInt32(pColl["gross"]);
            req.returnURL = pColl["returnURL"];
            req.signer = signer;
        }

 

        protected void composeAndSignPayResponse(string symVal)
        {
            Response.StatusCode = 302;
            Response.Status = "302 Moved Temporarily";
            Response.RedirectLocation = res.redirectionURL;
            Response.RedirectLocation += "?orderID=" + res.orderID;
            Response.RedirectLocation += "&status=" + globalState.status_names[(int)res.status];
            Response.RedirectLocation += "&gross=" + res.gross;
            Response.RedirectLocation += "&symVal=" + symVal;
            Sign(Response.RedirectLocation);
            Response.End();
        }

        protected void Sign(string plainText)
        {
            byte[] plainBytes = null;
            byte[] signatureBytes = null;
            try
            {
                // Encrypt plain text
                plainBytes = Encoding.Unicode.GetBytes(plainText);
                signatureBytes = globalState.rsaProvider.SignData(plainBytes, new SHA1CryptoServiceProvider());
                Response.RedirectLocation += "&signature=CaaS-" + BitConverter.ToString(signatureBytes);
            }
            catch (Exception ex)
            {
                Response.Write("Fail to sign.");
            }
        } // sign
    }
}