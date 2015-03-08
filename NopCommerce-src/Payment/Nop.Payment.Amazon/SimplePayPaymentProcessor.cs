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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.BusinessLogic.Products;
using NopSolutions.NopCommerce.BusinessLogic.Directory;
using System.Globalization;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;

using System.Net;
using System.Web;
using System.IO;
using System.Text;

namespace NopSolutions.NopCommerce.Payment.Methods.Amazon
{


    /// <summary>
    /// Represents an SimplePay payment gateway
    /// </summary>
    public partial class SimplePayPaymentProcessor : IPaymentMethod
    {
        //ERIC'S CODE
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

        public string SourceCode_PlaceOrder = @"

public class merchant_state
{
    public orderRecord[] orders;
    public string myDomain;
    public int count;
    public merchant_state(int m, string domain)
    {
        count = m;
        orders = new orderRecord[m];
        myDomain = domain;
    }
}

public partial class SimplePayPaymentProcessor1 : SimplePayPaymentProcessor
    {

        public canonicalRequestResponse PostProcessPayment(Order order)
        {
            Uri gatewayUrl = new Uri(SimplePaySettings.GatewayUrl);

            RemotePostProxy post = new RemotePostProxy();

            post.FormName = ""SimplePay"";
            //ERIC'S CODE
            //post.Url = gatewayUrl.ToString();
            post.Url = ""http://localhost:38623/Default.aspx"";
            post.Method = ""POST"";

            post.Add(""immediateReturn"", ""1"");
            post.Add(AmazonHelper.SIGNATURE_VERSION_KEYNAME, AmazonHelper.SIGNATURE_VERSION_2);
            post.Add(AmazonHelper.SIGNATURE_METHOD_KEYNAME, AmazonHelper.HMAC_SHA256_ALGORITHM);
            post.Add(""accessKey"", SimplePaySettings.AccessKey);
            post.Add(""amount"", String.Format(CultureInfo.InvariantCulture, ""USD {0:0.00}"", order.OrderTotal));
            post.Add(""description"", string.Format(""{0}, {1}"", SettingManager.StoreName, order.OrderId));
            post.Add(""amazonPaymentsAccountId"", SimplePaySettings.AccountId);
            post.Add(""returnUrl"", String.Format(""{0}AmazonSimplePayReturn.aspx"", CommonHelper.GetStoreLocation(false)));
            post.Add(""processImmediate"", (SimplePaySettings.SettleImmediately ? ""1"" : ""0""));
            post.Add(""referenceId"", order.OrderId.ToString());
            post.Add(AmazonHelper.SIGNATURE_KEYNAME, AmazonHelper.SignParameters(post.Params, SimplePaySettings.SecretKey, post.Method, gatewayUrl.Host, gatewayUrl.AbsolutePath));

            string tmp = String.Format(""{0}AmazonSimplePayReturn.aspx"", CommonHelper.GetStoreLocation(false));

            //Protocol independant code
            GlobalState.order.gross = Decimal.ToInt32(order.OrderTotal);
            post.req.gross = Decimal.ToInt32(order.OrderTotal);
            GlobalState.order.id = order.OrderId;
            post.req.orderID = order.OrderId;
            GlobalState.order.status = Global.OrderStatusEnum.Pending;

            //Contract.Assert(order.OrderTotal == order.OrderTotal);
            

            return post.Post();
        }
    }
";

        #region Methods
        /// <summary>
        /// Process payment
        /// </summary>
        /// <param name="paymentInfo">Payment info required for an order processing</param>
        /// <param name="customer">Customer</param>
        /// <param name="orderGuid">Unique order identifier</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void ProcessPayment(PaymentInfo paymentInfo, Customer customer, Guid orderGuid, ref ProcessPaymentResult processPaymentResult)
        {
            processPaymentResult.PaymentStatus = PaymentStatusEnum.Pending;
        }

        /// <summary>
        /// Post process payment (payment gateways that require redirecting)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>The error status, or String.Empty if no errors</returns>
        public string PostProcessPayment(Order order)
        {
            Uri gatewayUrl = new Uri(SimplePaySettings.GatewayUrl);

            RemotePost post = new RemotePost();

            post.FormName = "SimplePay";
            string hash = code_to_hash(SourceCode_PlaceOrder);
           
            //construct path digest
            string symT = "Merchant[[" + hash + "()]]";
            post.Add("symT", symT);

            //post.Url = gatewayUrl.ToString();
            post.Url = "http://localhost:38623/Default.aspx";
            post.Method = "POST";

            post.Add("immediateReturn", "1");
            post.Add(AmazonHelper.SIGNATURE_VERSION_KEYNAME, AmazonHelper.SIGNATURE_VERSION_2);
            post.Add(AmazonHelper.SIGNATURE_METHOD_KEYNAME, AmazonHelper.HMAC_SHA256_ALGORITHM);
            post.Add("accessKey", SimplePaySettings.AccessKey);
            post.Add("amount", String.Format(CultureInfo.InvariantCulture, "USD {0:0.00}", order.OrderTotal));
            post.Add("description", string.Format("{0}, {1}", SettingManager.StoreName, order.OrderId));
            post.Add("amazonPaymentsAccountId", SimplePaySettings.AccountId);
            post.Add("returnUrl", String.Format("{0}AmazonSimplePayReturn.aspx", CommonHelper.GetStoreLocation(false)));
            post.Add("processImmediate", (SimplePaySettings.SettleImmediately ? "1" : "0"));
            post.Add("referenceId", order.OrderId.ToString());
            //the entire msg is signed using the pre-decided simplepay secret key
            post.Add(AmazonHelper.SIGNATURE_KEYNAME, AmazonHelper.SignParameters(post.Params, SimplePaySettings.SecretKey, post.Method, gatewayUrl.Host, gatewayUrl.AbsolutePath));

            string tmp = String.Format("{0}AmazonSimplePayReturn.aspx", CommonHelper.GetStoreLocation(false));

            post.Post();

            return String.Empty;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee()
        {
            return SimplePaySettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void Capture(Order order, ref ProcessPaymentResult processPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Refunds payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void Refund(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Voids paymen
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void Void(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="paymentInfo">Payment info required for an order processing</param>
        /// <param name="customer">Customer</param>
        /// <param name="orderGuid">Unique order identifier</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void ProcessRecurringPayment(PaymentInfo paymentInfo, Customer customer, Guid orderGuid, ref ProcessPaymentResult processPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cancels recurring payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void CancelRecurringPayment(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool CanCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool CanPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool CanRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool CanVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        /// <returns>A recurring payment type of payment method</returns>
        public RecurringPaymentTypeEnum SupportRecurringPayments
        {
            get
            {
                return RecurringPaymentTypeEnum.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        /// <returns>A payment method type</returns>
        public PaymentMethodTypeEnum PaymentMethodType
        {
            get
            {
                return PaymentMethodTypeEnum.Standard;
            }
        }
        #endregion
    }
}
