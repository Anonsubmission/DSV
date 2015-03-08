//------------------------------------------------------------------------------
// The contents of this file are subject to the nopCommerce Public License Version 1.0 ("License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  http://www.nopCommerce.com/License.aspx. 
// 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
// See the License for the specific language governing rights and limitations under the License.
// 
// The Original Code is nopCommerce.
// The Initial Developer of the Original Code is NopSolutions.
// All Rights Reserved.
// 
// Contributor(s): 
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using NopSolutions.NopCommerce.Common;
using System.Web;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Specialized;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.SEO;
using NopSolutions.NopCommerce.Common.Utils;
using System.Diagnostics;


namespace NopSolutions.NopCommerce.Payment.Methods.Amazon
{
    public class AmazonHelper
    {
        public static readonly String SIGNATURE_KEYNAME = "signature";
        public static readonly String SIGNATURE_METHOD_KEYNAME = "signatureMethod";
        public static readonly String SIGNATURE_VERSION_KEYNAME = "signatureVersion";
        public static readonly String SIGNATURE_VERSION_1 = "1";
        public static readonly String SIGNATURE_VERSION_2 = "2";
        public static readonly String HMAC_SHA1_ALGORITHM = "HmacSHA1";
        public static readonly String HMAC_SHA256_ALGORITHM = "HmacSHA256";
        public static readonly String RSA_SHA1_ALGORITHM = "SHA1withRSA";
        public static readonly String CERTIFICATE_URL_KEYNAME = "certificateUrl";
        public static readonly String EMPTY_STRING = String.Empty;

        // Constants used when constructing the string to sign for v2
        public static readonly String AppName = "ASP";
        public static readonly String NewLine = "\n";
        public static readonly String EmptyUriPath = "/";
        public static String equals = "=";
        public static readonly String And = "&";
        public static readonly String UTF_8_Encoding = "UTF-8";

        /**
        * Calculate String to Sign for SignatureVersion 1
        * @param parameters request parameters
        * @return String to Sign
        */
        public static string SignParameters(NameValueCollection parameters, String key,
            String HttpMethod, String Host, String RequestURI) //throws Exception
        {
            String signatureVersion = parameters[SIGNATURE_VERSION_KEYNAME];
            String algorithm = HMAC_SHA1_ALGORITHM;
            String stringToSign = null;
            if (signatureVersion == null && String.Compare(AppName, "FPS", true) != 0)
            {
                stringToSign = CalculateSignV1(parameters);
            }
            else if (String.Compare("1", signatureVersion, true) == 0)
            {
                stringToSign = CalculateSignV1(parameters);
            }
            else if (String.Compare("2", signatureVersion, true) == 0)
            {
                algorithm = parameters[SIGNATURE_METHOD_KEYNAME];
                stringToSign = CalculateSignV2(parameters, HttpMethod, Host, RequestURI);
            }
            else
            {
                throw new NopException("Invalid Signature Version specified");
            }
            return Sign(stringToSign, key, algorithm);
        }

        /**
	    * Calculate String to Sign for SignatureVersion 1
	    * @param parameters request parameters
	    * @return String to Sign
	    */
        private static string CalculateSignV1(NameValueCollection parameters)
        {
            StringBuilder data = new StringBuilder();
            IDictionary<String, String> sorted = new SortedDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (string paramKey in parameters.AllKeys)
            {
                sorted.Add(paramKey, parameters[paramKey]);
            }
            foreach (KeyValuePair<String, String> pair in sorted)
            {
                if (pair.Value != null)
                {
                    if (String.Compare(pair.Key, SIGNATURE_KEYNAME, true) == 0) continue;
                    data.Append(pair.Key);
                    data.Append(pair.Value);
                }
            }
            return data.ToString();
        }

        /**
    	 * Calculate String to Sign for SignatureVersion 2
	     * @param parameters
    	 * @param httpMethod - POST or GET
	     * @param hostHeader - Service end point
    	 * @param requestURI - Path
	     * @return
    	 */
        public static string CalculateSignV2(NameValueCollection parameters, String httpMethod, String hostHeader, String requestURI)// throws SignatureException
        {
            StringBuilder stringToSign = new StringBuilder();
            if (httpMethod == null) throw new Exception("HttpMethod cannot be null");
            stringToSign.Append(httpMethod);
            stringToSign.Append(NewLine);

            // The host header - must eventually convert to lower case
            // Host header should not be null, but in Http 1.0, it can be, in that
            // case just append empty string ""
            if (hostHeader == null)
                stringToSign.Append("");
            else
                stringToSign.Append(hostHeader.ToLower());
            stringToSign.Append(NewLine);

            if (requestURI == null || requestURI.Length == 0)
                stringToSign.Append(EmptyUriPath);
            else
                stringToSign.Append(UrlEncode(requestURI, true));
            stringToSign.Append(NewLine);

            IDictionary<String, String> sortedParamMap = new SortedDictionary<String, String>(StringComparer.Ordinal);
            foreach (string paramKey in parameters.AllKeys)
            {
                sortedParamMap.Add(paramKey, parameters[paramKey]);
            }
            foreach (String key in sortedParamMap.Keys)
            {
                if (String.Compare(key, SIGNATURE_KEYNAME, true) == 0) continue;
                stringToSign.Append(UrlEncode(key, false));
                stringToSign.Append(equals);
                stringToSign.Append(UrlEncode(sortedParamMap[key], false));
                stringToSign.Append(And);
            }

            String result = stringToSign.ToString();
            return result.Remove(result.Length - 1);
        }

        public static String UrlEncode(String data, bool path)
        {
            StringBuilder encoded = new StringBuilder();
            String unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~" + (path ? "/" : "");

            foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                {
                    encoded.Append(symbol);
                }
                else
                {
                    encoded.Append("%" + String.Format("{0:X2}", (int)symbol));
                }
            }

            return encoded.ToString();

        }

        /**
         * Computes RFC 2104-compliant HMAC signature.
         */
        public static String Sign(String data, String key, String signatureMethod)// throws SignatureException
        {
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                HMAC Hmac = HMAC.Create(signatureMethod);
                Hmac.Key = encoding.GetBytes(key);
                Hmac.Initialize();
                CryptoStream cs = new CryptoStream(Stream.Null, Hmac, CryptoStreamMode.Write);
                cs.Write(encoding.GetBytes(data), 0, encoding.GetBytes(data).Length);
                cs.Close();
                byte[] rawResult = Hmac.Hash;
                String sig = Convert.ToBase64String(rawResult, 0, rawResult.Length);
                return sig;
            }
            catch (Exception e)
            {
                throw new NopException("Failed to generate signature: " + e.Message);
            }
        }

        /**
     * Validates the request by checking the integrity of its parameters.
     * @param parameters - all the http parameters sent in IPNs or return urls. 
     * @param urlEndPoint should be the url which recieved this request. 
     * @param httpMethod should be either POST (IPNs) or GET (returnUrl redirections)
     */

        //ERIC'S CODE - begin
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://ericchen.me:81/"; //ERIC'S IP
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";
        static string[] whitelist = new string[2] { "Merchant", "CaaS" };
        static string root = "C:\\CCP\\teamproject\\NopCommerce\\NopCommerce";
        static string payee_email = "cs0317b@gmail.com";

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
            public bool isEncrypted;
        }

        

        //this function parses the symval string and returns an array of functions called in reverse-chronological order
        // A[HASH1(B[[HASH2()]])]
        protected static Stack parse_digest(string symval)
        {
            Stack callStack = new Stack();

            int step = 0, start = 0, end = symval.Length;
            int c = 0, s = 0;

            while (start < end)
            {
                step = 0;
                c = (symval.IndexOf('(', start) == -1) ? int.MaxValue : symval.IndexOf('(', start);
                s = (symval.IndexOf('[', start) == -1) ? int.MaxValue : symval.IndexOf('[', start);

                if (c < s)
                { //if ( is before [
                    if (symval[c + 1] == '(') step = 2; // double '('
                    else step = 1;
                    start += step;
                    end -= step;
                }
                else if (s < c)
                { //if [ is before (
                    transaction cur = new transaction();
                    if (symval[s + 1] == '[') step = 2; // double '['
                    else step = 1;
                    if (c < int.MaxValue)
                    { // pattern: A[HASH(
                        cur.isEncrypted = (step == 1) ? false : true; //[[encrypted]] [not encrypted]
                        cur.party = symval.Substring(start, s - start); //A[[HASH()]], a is party
                        cur.function = symval.Substring(s + step, c - s - step); //A[[HASH()]] HASH is function

                        //if one of the parties involved in the transaction is not known
                        if (!Array.Exists(whitelist, element => element == cur.party))
                        {
                            break;
                        }

                        callStack.Push(cur);
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

        protected static string assemble_code(string symT)
        {

            Stack callstack = parse_digest(symT);

            string code = @"
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Threading.Tasks;
using NopSolutions.NopCommerce;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.SEO;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.Controls;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Amazon;
using NopSolutions.NopCommerce.Web;
using Global;
using System.Diagnostics.Contracts;

interface Picker
{
    int NondetInt(); 
    canonicalRequestResponse NodetReqres();
    Order NondetOrder();
    orderRecord NondetorderRecord();
}


public class GlobalState
{
    public static string MerchantPaymentEmail = """;

            code += payee_email;

            code += @""";
    public static orderRecord order;

    public static merchant_state tstore = new merchant_state(100, ""TStore.com"");


    //this member is used as a get-around for reimplementing NameValueCollection in stub
    public static string CaaS_email_proxy = """";

    //this memeber is used as a get-around for doing string comparison in Boogie
    public static Decimal CaaS_gross_proxy;

    public static int witness;

}

";
            transaction step1 = (transaction)callstack.Pop();
            transaction step2 = (transaction)callstack.Pop();
            transaction step3 = (transaction)callstack.Pop();

            
            code += hash_to_code(step1.function);
            code += hash_to_code(step2.function);
            code += hash_to_code(step3.function);
            

            code += @"
class PoirotMain
{
    static Picker p;
    static void assumeOrderProperties(canonicalRequestResponse res)
    {
        int orderID=res.orderID;
        GlobalState.tstore.orders[orderID] = p.NondetorderRecord();
        Contract.Assume(GlobalState.tstore.orders[orderID].id==orderID);
        Contract.Assume(GlobalState.tstore.orders[orderID].gross == res.gross);
        Contract.Assume(GlobalState.tstore.orders[orderID].status == Global.OrderStatusEnum.Pending);
    }
    static void Main()
    {
        SimplePayPaymentProcessor1 merchant_sender = new SimplePayPaymentProcessor1();
        CaaS amazon = new CaaS();
        
        AmazonSimplePayReturn1 merchant_receiver = new AmazonSimplePayReturn1();
        Order init_order = p.NondetOrder();
        GlobalState.order = new orderRecord();
        canonicalRequestResponse res_placeorder = p.NodetReqres();
        canonicalRequestResponse req_pay = p.NodetReqres();
        canonicalRequestResponse res_pay = p.NodetReqres();
        canonicalRequestResponse req_finish = p.NodetReqres();

        // Computation on merchant - place order
        res_placeorder = merchant_sender.PostProcessPayment(init_order);
        assumeOrderProperties(res_placeorder);";

            if (step1.isEncrypted)
            {
                code += @"
        // Message: merchant -> client -> CaaS
        // is the msg encrypted? if so, then:
        req_pay = res_placeorder;
        GlobalState.MerchantPaymentEmail = req_pay.payee;
        GlobalState.order.gross = req_pay.gross;
        GlobalState.order.id = req_pay.orderID;
";
            }

            code += @"
        // Computation on Amazon - pay
        res_pay = amazon.pay(req_pay);
";

            if (step2.isEncrypted)
            {
                code += @"
        // Message: CaaS -> client -> merchant
        // is the msg encrypted? if so, then:
        req_finish = res_pay;
        merchant_receiver.Request = req_finish;
        Contract.Assume(res_pay.orderID == req_finish.orderID);
        GlobalState.CaaS_email_proxy = res_pay.payee;
        GlobalState.CaaS_gross_proxy = res_pay.gross; //we need this since boogie can't handle string concat";
            }

            code += @"
        // Computation on merchant - finish order
        merchant_receiver.Page_Load(null, null);";

            //assert
            code += @"
        res_pay.witness = GlobalState.witness;
        Contract.Assert(amazon.caas.payments[res_pay.witness].orderID == req_finish.orderID &&
                        amazon.caas.payments[res_pay.witness].gross == GlobalState.tstore.orders[req_finish.orderID].gross &&
                        amazon.caas.payments[res_pay.witness].payee == GlobalState.tstore.myDomain &&
                        amazon.caas.payments[res_pay.witness].status == CaasReturnStatus.Sucess);
        Contract.Assert(0 <= res_pay.witness && res_pay.witness < amazon.caas.payments.Length);

        Contract.Assert(Contract.Exists(0, amazon.caas.payments.Length, i =>
                        amazon.caas.payments[i].orderID == req_finish.orderID &&
                        amazon.caas.payments[i].gross == GlobalState.tstore.orders[req_finish.orderID].gross &&
                        amazon.caas.payments[i].payee == GlobalState.tstore.myDomain &&
                        amazon.caas.payments[i].status == CaasReturnStatus.Sucess
            ));
    }
}";

            return code;

        }
        public static void generate_cs_file_from_symval(string symT)
        {
            TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));


            string content = assemble_code(symT);


            TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);
            
            using (StreamWriter outfile = new StreamWriter(root + "\\Program.cs"))
            {
               outfile.Write(content);
            }
            
        }

        static string SourceCode_FinishOrder = @"

namespace NopSolutions.NopCommerce.Web
{
    using NopSolutions.NopCommerce.Payment.Methods.Amazon;

    public partial class AmazonSimplePayReturn1
    {
        public canonicalRequestResponse Response;
        public Page Page;
        public canonicalRequestResponse Request;

        public AmazonSimplePayReturn1()
        {
            Request = new canonicalRequestResponse();
            Response = new canonicalRequestResponse();
            Page = new Page();
        }

        public void Page_Load(object sender, EventArgs e)
        {

            if (!AmazonHelper1.ValidateRequest(null, String.Format(""{0}AmazonSimplePayReturn.aspx"", CommonHelper.GetStoreLocation()), ""GET""))
            {
                Response.Redirect(CommonHelper.GetStoreLocation());
            }

            // ========================= This block of code is not useful for our proof ===================/
            int orderId = Convert.ToInt32(CommonHelper.QueryStringInt(""referenceId""));
            Order order = OrderManager.GetOrderById(orderId);
            if (order == null)
            {
                Response.Redirect(CommonHelper.GetStoreLocation());
            }
            if (NopContext.Current.User.CustomerId != order.CustomerId)
            {
                Response.Redirect(CommonHelper.GetStoreLocation());
            }
            // ========================= end block ===================/

            //if it reaches here, then we mark order as paid
            if (OrderManager.CanMarkOrderAsPaid(order))
            {
                OrderManager.MarkOrderAsPaid(order.OrderId);

                if (GlobalState.order.id == Request.orderID)
                {
                    GlobalState.order.status = Global.OrderStatusEnum.Paid;
                }
            }
            else
            {
                Contract.Assume(false);
            }


            Response.Redirect(""~/checkoutcompleted.aspx"");
        }
    }
}

namespace NopSolutions.NopCommerce.Payment.Methods.Amazon
{
    using NopSolutions.NopCommerce.Web;
    public class AmazonHelper1 : AmazonHelper
    {
        public static Boolean ValidateRequest(NameValueCollection parameters,
               String urlEndPoint, String httpMethod)
        {

            int orderId = Convert.ToInt32(CommonHelper.QueryStringInt(""referenceId""));
            Order order = OrderManager.GetOrderById(orderId);

            //check if price is the same
            if (parameters[""transactionAmount""] != ""USD "" + order.OrderTotal.ToString())
            {
                return false;
            }

            //The following if statement is used because Boogie cannot handle the string comparison in the previous condition
            if (GlobalState.CaaS_gross_proxy != GlobalState.order.gross)
            {
                Contract.Assume(false);
            }

            //check if it's paid to the right person
            if (parameters[""recipientEmail""] != GlobalState.MerchantPaymentEmail)
            {
                return false;
            }

            //The following if statement is used so that we don't have to implement namevaluecollection in stub
            if (GlobalState.CaaS_email_proxy != GlobalState.MerchantPaymentEmail)
            {
                Contract.Assume(false);
            }

            return ValidateSignatureV2(parameters, urlEndPoint, httpMethod);

        }
    }
}

";
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

        public static Boolean ValidateRequest(NameValueCollection parameters,
               String urlEndPoint, String httpMethod)
        {
            String signatureVersion = null;
            //This is present only in case of signature version 2. If this is not present we assume this is signature version 1.
            try
            {
                signatureVersion = parameters[SIGNATURE_VERSION_KEYNAME];
            }
            catch (KeyNotFoundException)
            {
                signatureVersion = null;
            }

            // Boogie check
            string old_hash = parameters["symT"];
            string new_hash = code_to_hash(SourceCode_FinishOrder);
            string symT = "Merchant[[" + new_hash + "(" + old_hash + ")]]";

            generate_cs_file_from_symval(symT);

            if (!checkLogicProperty()) return false;

            // check for Amazon's payment status code
            if (parameters["status"] != "PS" && parameters["status"] != "PR") return false;

            int orderId = Convert.ToInt32(CommonHelper.QueryStringInt("referenceId"));
            Order order = OrderManager.GetOrderById(orderId);

            //check if price is the same
            string trans_total = parameters["transactionAmount"];
            string order_total = Math.Round(order.OrderTotal, 2).ToString();
            if (trans_total != "USD " + order_total &&
                trans_total != order_total) return false;

            //check if it's paid to the right person
            if (parameters["recipientEmail"] != "cs0317b@gmail.com") return false;

            bool result;
            if (SIGNATURE_VERSION_2.Equals(signatureVersion))
                result =  ValidateSignatureV2(parameters, urlEndPoint, httpMethod);
            else
                result = ValidateSignatureV1(parameters);

            return result;
        }

        /**
          * Verifies the signature using PKI.
          */
        public static Boolean ValidateSignatureV2(NameValueCollection parameters,
            String urlEndPoint, String httpMethod)
        {
            //1. input validation.
            String signature = parameters[SIGNATURE_KEYNAME];
            if (signature == null)
            {
                throw new Exception("'signature' is missing from the parameters.");
            }

            String signatureMethod = parameters[SIGNATURE_METHOD_KEYNAME];
            if (signatureMethod == null)
            {
                throw new Exception("'signatureMethod' is missing from the parameters.");
            }

            String signatureAlgorithm = GetSignatureAlgorithm(signatureMethod);
            if (signatureAlgorithm == null)
            {
                throw new Exception("'signatureMethod' present in parameters is invalid. " +
                        "Valid signatureMethods are : 'RSA-SHA1'");
            }

         
            
            CspParameters cspParams = null;
            RSACryptoServiceProvider rsaProvider = null;
            StreamReader publicKeyFile = null;
            string publicKeyText = "";
            byte[] plainBytes = null;
            string key_root = "C:\\CCP";

            cspParams = new CspParameters();
            cspParams.ProviderType = 1;// PROV_RSA_FULL;
            rsaProvider = new RSACryptoServiceProvider(384, cspParams);

            // Read public key from file
            publicKeyFile = File.OpenText(key_root + "\\RSAKeys\\pubkey_CaaS.xml");
            publicKeyText = publicKeyFile.ReadToEnd();

            // Import public key
            rsaProvider.FromXmlString(publicKeyText);

            

            // ERIC'S CODE - end

            //2. calculating the string to sign
            String stringToSign = EMPTY_STRING;
            try
            {
                Uri uri = new Uri(urlEndPoint);
                String hostHeader = getHostHeader(uri);
                String requestURI = GetRequestURI(uri);
                stringToSign = CalculateSignV2(parameters, httpMethod, hostHeader, requestURI);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            //3. verify signature 
            try
            {
                byte[] signatureBytes = Base64DecodeToBytes(signature);
                // ERIC'S CODE - begin
                /*
                X509Certificate2 x509Cert = new X509Certificate2(StrToByteArray(certificate));
                RSACryptoServiceProvider RSA = (RSACryptoServiceProvider)x509Cert.PublicKey.Key;
                return RSA.VerifyData(StrToByteArray(stringToSign), new SHA1Managed(), signatureBytes);
                */
                return rsaProvider.VerifyData(Encoding.Unicode.GetBytes(stringToSign), new SHA1CryptoServiceProvider(), signatureBytes);

                // ERIC'S CODE - end
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static bool ValidateSignatureV1(NameValueCollection parameters)
        {
            String stringToSign = CalculateSignV1(parameters);
            String signature = parameters[SIGNATURE_KEYNAME];
            String sig;
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                HMAC Hmac = HMAC.Create("HmacSHA1");
                Hmac.Key = encoding.GetBytes(SimplePaySettings.SecretKey);
                Hmac.Initialize();
                CryptoStream cs = new CryptoStream(Stream.Null, Hmac, CryptoStreamMode.Write);
                cs.Write(encoding.GetBytes(stringToSign), 0, encoding.GetBytes(stringToSign).Length);
                cs.Close();
                byte[] rawResult = Hmac.Hash;
                sig = Convert.ToBase64String(rawResult, 0, rawResult.Length);

            }
            catch (Exception e)
            {
                throw new Exception("Failed to generate HMAC : " + e.Message);
            }
            return sig.Equals(signature);
        }


        public static string V2UrlEncode(String data, bool path)
        {
            StringBuilder encoded = new StringBuilder();
            String unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~" + (path ? "/" : "");

            foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                {
                    encoded.Append(symbol);
                }
                else
                {
                    encoded.Append("%" + String.Format("{0:X2}", (int)symbol));
                }
            }
            return encoded.ToString();
        }



        public static string UrlDecode(String value)
        {
            return HttpUtility.UrlDecode(value, Encoding.UTF8);
        }

        private static string getHostHeader(Uri uri)
        {
            int port = uri.Port;
            if (port != -1)
            {
                if (uri.Scheme.Equals(Uri.UriSchemeHttps) && port == 443
                    || uri.Scheme.Equals(Uri.UriSchemeHttp) && port == 80)
                    port = -1;
            }
            return uri.Host.ToLower() + (port != -1 ? ":" + port : "");
        }

        private static string GetRequestURI(Uri uri)
        {
            String requestURI = uri.LocalPath;
            if (requestURI == null || requestURI.Equals(EMPTY_STRING))
            {
                requestURI = "/";
            }
            else
            {
                requestURI = UrlDecode(requestURI);
            }
            return requestURI;
        }

        private static string GetSignatureAlgorithm(string signatureMethod)
        {
            if ("RSA-SHA1".Equals(signatureMethod))
            {
                return RSA_SHA1_ALGORITHM;
            }
            return null;
        }

        private static string GetPublicKeyCertificateAsString(string certificateUrl)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(certificateUrl);
            request.AllowAutoRedirect = false;
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader read = new StreamReader(stream);
            String data = read.ReadToEnd();
            return data;
        }

        /// <summary>
        /// Base64 decode a string
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>Data</returns>
        public static byte[] Base64DecodeToBytes(string data)
        {
            return Convert.FromBase64String(data);
        }

        /// <summary>
        /// Convert a string to a byte array
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>Byte array</returns>
        public static byte[] StrToByteArray(string str)
        {
            return new UTF8Encoding().GetBytes(str);
        }

    }
}
