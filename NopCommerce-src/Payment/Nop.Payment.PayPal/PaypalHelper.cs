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
// Contributor(s): _______. 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Directory;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.Payment.Methods.PayPal.PayPalSvc;


//Rui:begin
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Collections;
//rui:end

namespace NopSolutions.NopCommerce.Payment.Methods.PayPal
{
    /// <summary>
    /// Represents paypal helper
    /// </summary>
    public class PaypalHelper
    {
        /// <summary>
        /// Get Paypal currency code
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <returns>Paypal currency code</returns>
        public static CurrencyCodeType GetPaypalCurrency(Currency currency)
        {
            CurrencyCodeType currencyCodeType = CurrencyCodeType.USD;
            try
            {
                currencyCodeType = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), currency.CurrencyCode, true);
            }
            catch 
            {
            }
            return currencyCodeType;
        }

        /// <summary>
        /// Checks response
        /// </summary>
        /// <param name="abstractResponse">response</param>
        /// <param name="errorMsg">Error message if exists</param>
        /// <returns>True - response OK; otherwise, false</returns>
        public static bool CheckSuccess(AbstractResponseType abstractResponse, out string errorMsg)
        {
            bool success = false;
            StringBuilder sb = new StringBuilder();
            switch (abstractResponse.Ack)
            {
                case AckCodeType.Success:
                case AckCodeType.SuccessWithWarning:
                    success = true;
                    break;
                default:
                    break;
            }
            if (null != abstractResponse.Errors)
            {
                foreach (ErrorType errorType in abstractResponse.Errors)
                {
                    if (sb.Length <= 0)
                    {
                        sb.Append(Environment.NewLine);
                    }
                    sb.Append("LongMessage: ").Append(errorType.LongMessage).Append(Environment.NewLine);
                    sb.Append("ShortMessage: ").Append(errorType.ShortMessage).Append(Environment.NewLine);
                    sb.Append("ErrorCode: ").Append(errorType.ErrorCode).Append(Environment.NewLine);
                }
            }
            errorMsg = sb.ToString();
            return success;
        }

        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="PaymentStatus">PayPal payment status</param>
        /// <param name="PendingReason">PayPal pending reason</param>
        /// <returns>Payment status</returns>
        public static PaymentStatusEnum GetPaymentStatus(string PaymentStatus, string PendingReason)
        {
            PaymentStatusEnum result = PaymentStatusEnum.Pending;

            if (PaymentStatus == null)
                PaymentStatus = string.Empty;

            if (PendingReason == null)
                PendingReason = string.Empty;

            switch (PaymentStatus.ToLowerInvariant())
            {
                case "pending":
                    switch (PendingReason.ToLowerInvariant())
                    {
                        case "authorization":
                            result = PaymentStatusEnum.Authorized;
                            break;
                        default:
                            result = PaymentStatusEnum.Pending;
                            break;
                    }
                    break;
                case "processed":
                case "completed":
                case "canceled_reversal":
                    result = PaymentStatusEnum.Paid;
                    break;
                case "denied":
                case "expired":
                case "failed":
                case "voided":
                    result = PaymentStatusEnum.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = PaymentStatusEnum.Refunded;
                    break;
                default:
                    break;
            }
            return result;
        }

        //RUI - begin
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://ericchen.me:81/"; //ERIC'S IP
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";
        static string[] whitelist = new string[2] { "Merchant", "CaaS" };
        static string root = "C:\\CCP\\teamprojects\\NopCommerce-PayPal-MTP\\NopCommerce";
        //static string payee_email = "cs0317b@gmail.com";

        /// <summary>
        /// send a http request
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="post">post</param>
        /// <param name="method">method</param>
        /// <param name="refer">refer</param>
        /// <returns>response</returns>
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


        /// <summary>
        /// converts a code hash to its corresponding code
        /// </summary>
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

            Debug.WriteLine("message (hash=" + hash + ") has " + code.Length + " bytes\n");

            return code;
        }

        /// <summary>
        /// converts a piece of code to a hash
        /// </summary>
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


        /// <summary>
        /// transaction
        /// </summary>
        public class transaction
        {
            /// <summary>
            /// party
            /// </summary>
            public string party;
            /// <summary>
            /// function
            /// </summary>
            public string function;
            /// <summary>
            /// isEncrypted
            /// </summary>
            public bool isEncrypted, isTrustedEnclosing;
        }


        // A[HASH1(B[[HASH2()]])]
        /// <summary>
        /// parses the symval string and returns an array of functions called in reverse-chronological order
        /// </summary>
        protected static Stack parse_digest(string symval)
        {
            Stack callStack = new Stack();

            int step = 0, start = 0, end = symval.Length;
            int c = 0, s = 0;
            transaction cur = new transaction();
            while (start < end)
            {
                step = 0;
                c = (symval.IndexOf('(', start) == -1) ? int.MaxValue : symval.IndexOf('(', start);
                s = (symval.IndexOf('[', start) == -1) ? int.MaxValue : symval.IndexOf('[', start);

                if (c < s)
                { //if ( is before [
                    if (symval[c + 1] == '(') step = 2; // double '('
                    else step = 1;
                    cur.isTrustedEnclosing = (step == 1) ? false : true;
                    start += step;
                    end -= step;
                }
                else if (s < c)
                { //if [ is before (
                    cur = new transaction();
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

        /// <summary>
        /// assemble code
        /// </summary>
        protected static string assemble_code(string path_digest)
        {

            Stack callstack = parse_digest(path_digest);

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
using NopSolutions.NopCommerce.Payment.Methods.PayPal;
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
  //  public static string MerchantPaymentEmail ;
    public static merchant_state tstore = new merchant_state(100, ""TStore.com"");
    public static CaaS paypal = new CaaS();
}
//shuo:begin
public class merchant_state
{
    public orderRecord[] orders;
    public string myAccount;
    //public int count;
    public merchant_state(int m, string account)
    {
        //count = m;
        orders = new orderRecord[m];
        myAccount = account;
    }
}

";
            int n = callstack.Count;
            transaction[] steps = new transaction[callstack.Count];
            while (true)
            {
                if (callstack.Count == 0) break;
                int i = callstack.Count - 1;
                steps[i] = (transaction)callstack.Pop();
                if (steps[i].isTrustedEnclosing) code += hash_to_code(steps[i+1].function);
                if (steps[i].isEncrypted || i==0) code += hash_to_code(steps[i].function);
            }

            code += @"
class PoirotMain
{
    static Picker p;
    static void foo(out int x)
    {
        x = 100;
    }
    static void Main()
    {
        int y;
        foo(out y);
        Contract.Assert(y != 10);
        
        int witness;
        CaaS paypal = GlobalState.paypal;
        PayPalStandardPaymentProcessor1 merchant_sender = new PayPalStandardPaymentProcessor1();
        
        PayPalStandardReturn1 merchant_receiver = new PayPalStandardReturn1();
        Order init_order = p.NondetOrder();
        canonicalRequestResponse res_placeorder = p.NodetReqres();
        canonicalRequestResponse req_pay = p.NodetReqres();
        canonicalRequestResponse res_pay = p.NodetReqres();
        canonicalRequestResponse req_finish = p.NodetReqres();
";
            if (n == 4) code += @"
        // Computation on merchant - place order
        res_placeorder = merchant_sender.PostProcessPayment(init_order);";

            if (n >= 3) code += @"
        // Message: merchant -> client -> CaaS
        // is the msg encrypted? if so, then:
        req_pay = res_placeorder;
        GlobalState.MerchantPaymentEmail = req_pay.payee;
        GlobalState.order.gross = req_pay.gross;
        GlobalState.order.id = req_pay.orderID;

        GlobalState.paypal.pay(req_pay); //Rui: there is no return for this call, because anything returned through the client should be havoced
";
            if (n >= 2) code += @"
         req_finish.orderID = p.NondetInt();
         witness = merchant_receiver.Page_Load(null, null, req_finish.orderID);
";

            //assert
            code += @"
        Contract.Assert(GlobalState.paypal.caas.payments[witness].orderID == req_finish.orderID);
        Contract.Assert(
                        paypal.caas.payments[witness].orderID == req_finish.orderID &&
                        paypal.caas.payments[witness].gross == GlobalState.tstore.orders[req_finish.orderID].gross &&
                        paypal.caas.payments[witness].payee == GlobalState.tstore.myAccount &&
                        paypal.caas.payments[witness].status == CaasReturnStatus.Sucess
                        );
        Contract.Assert(0 <= witness && witness < paypal.caas.payments.Length);
        
        Contract.Assert(Contract.Exists(0, paypal.caas.payments.Length, i =>
                        paypal.caas.payments[i].orderID == req_finish.orderID &&
                        paypal.caas.payments[i].payee == GlobalState.tstore.myAccount &&
                        paypal.caas.payments[i].status == CaasReturnStatus.Sucess &&
                        paypal.caas.payments[i].gross == GlobalState.tstore.orders[req_finish.orderID].gross 
            ));
    }
}";

            return code;

        }

        //Rui: measurement
        public static long getCurrentTimeInMS()
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            return milliseconds;
        }

        /// <summary>
        /// generate cs file from symval
        /// </summary>
        public static void generate_cs_file_from_symval(string path_digest)
        {

            long t1 = getCurrentTimeInMS();
            string content = assemble_code(path_digest);
            long t2 = getCurrentTimeInMS();

            using (StreamWriter outfile = new StreamWriter(root + "\\Program.cs"))
            {
                outfile.Write(content);
            }

            long t3 = getCurrentTimeInMS();

            Debug.WriteLine("t1=" + t1 + ", t2=" + t2 + ", t3 =" + t3 + "\n");
        }

        public static bool checkLogicProperty()
        {
            long t4 = getCurrentTimeInMS();

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

            long t5 = getCurrentTimeInMS();
            Debug.WriteLine("t4=" + t4 + ", t5=" + t5 + "\n");

            if (output.IndexOf("Program has no bugs") > 0)
                return true;
            else
                return false;
        }
        //RUI end


    }
}

