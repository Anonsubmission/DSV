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


public class GlobalState
{
    public static string MerchantPaymentEmail = "cs0317b@gmail.com";
    public static orderRecord order;

    //this member is used as a get-around for reimplementing NameValueCollection in stub
    public static string CaaS_email_proxy = "";

    //this memeber is used as a get-around for doing string comparison in Boogie
    public static Decimal CaaS_gross_proxy;
}

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
    
            if (!AmazonHelper1.ValidateRequest(null, String.Format("{0}AmazonSimplePayReturn.aspx", CommonHelper.GetStoreLocation()), "GET"))
            {
                Response.Redirect(CommonHelper.GetStoreLocation());
            }
    
            // ========================= This block of code is not useful for our proof ===================/
            int orderId = Convert.ToInt32(CommonHelper.QueryStringInt("referenceId"));
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
         

            Response.Redirect("~/checkoutcompleted.aspx");
        }
    }
}

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


    public class AmazonHelper1: AmazonHelper
    {
        public static Boolean ValidateRequest(NameValueCollection parameters,
               String urlEndPoint, String httpMethod)
        {

            int orderId = Convert.ToInt32(CommonHelper.QueryStringInt("referenceId"));
            Order order = OrderManager.GetOrderById(orderId);

            //check if price is the same
            if (parameters["transactionAmount"] != "USD " + order.OrderTotal.ToString())
            {
                return false;
            }

            //The following "if statement" is used because Boogie cannot handle the string comparison in the previous condition
            if (GlobalState.CaaS_gross_proxy != GlobalState.order.gross)
            {
                Contract.Assume(false);
            }

            //check if it's paid to the right person
            if (parameters["recipientEmail"] != GlobalState.MerchantPaymentEmail)
            {
                return false;
            }

            //The following "if statement" is used so that we don't have to implement namevaluecollection in stub
            if (GlobalState.CaaS_email_proxy != GlobalState.MerchantPaymentEmail)
            {
                Contract.Assume(false);
            }

            return ValidateSignatureV2(parameters, urlEndPoint, httpMethod);
           
        }
    }


    public partial class SimplePayPaymentProcessor1 : SimplePayPaymentProcessor
    {

        public canonicalRequestResponse PostProcessPayment(Order order)
        {
            Uri gatewayUrl = new Uri(SimplePaySettings.GatewayUrl);

            RemotePostProxy post = new RemotePostProxy();

            post.FormName = "SimplePay";
            //ERIC'S CODE
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
            post.Add(AmazonHelper.SIGNATURE_KEYNAME, AmazonHelper.SignParameters(post.Params, SimplePaySettings.SecretKey, post.Method, gatewayUrl.Host, gatewayUrl.AbsolutePath));

            string tmp = String.Format("{0}AmazonSimplePayReturn.aspx", CommonHelper.GetStoreLocation(false));

            //Protocol independant code
            GlobalState.order.gross = order.OrderTotal;
            post.req.gross = order.OrderTotal;
            GlobalState.order.id = order.OrderId;
            post.req.orderID = order.OrderId;
            GlobalState.order.status = Global.OrderStatusEnum.Pending;

            //Contract.Assert(order.OrderTotal == order.OrderTotal);
            

            return post.Post();
        }
    }
}

interface Picker
{
    int NondetInt();
    canonicalRequestResponse NodetReqres();
    Order NondetOrder();
}

class PoirotMain
{
    static Picker p;

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

        // Message: merchant -> client -> CaaS
        // is the msg encrypted? if so, then:
        req_pay = res_placeorder;
        GlobalState.MerchantPaymentEmail = req_pay.payee;
        GlobalState.order.gross = req_pay.gross;
        GlobalState.order.id = req_pay.orderID;
        
        // Computation on Amazon - pay
        res_pay = amazon.pay(req_pay);

        // Message: CaaS -> client -> merchant
        // is the msg encrypted? if so, then:
        req_finish = res_pay;
        merchant_receiver.Request = req_finish;
        Contract.Assume(res_pay.orderID == req_finish.orderID);
        GlobalState.CaaS_email_proxy = res_pay.payee;
        GlobalState.CaaS_gross_proxy = res_pay.gross; //we need this since boogie can't handle string concat

        // Computation on merchant - finish order
        merchant_receiver.Page_Load(null, null);
        
        Contract.Assert(GlobalState.order.id == req_finish.orderID);
        Contract.Assert(GlobalState.MerchantPaymentEmail == req_finish.payee);
        Contract.Assert(GlobalState.order.gross == req_finish.gross);
        Contract.Assert(GlobalState.order.status == Global.OrderStatusEnum.Paid);

        
    }
}