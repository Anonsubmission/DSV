
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


public class GlobalState
{
    public static string MerchantPaymentEmail = "cs0317b@gmail.com";
    public static orderRecord order;
    public static orderRecord payment;

    //this member is used as a get-around for reimplementing NameValueCollection in stub
    public static string CaaS_email_proxy = "";

    //this memeber is used as a get-around for doing string comparison in Boogie
    public static Decimal CaaS_gross_proxy;

    public static CaaS paypal = new CaaS();
}


namespace NopSolutions.NopCommerce.Payment.Methods.PayPal
{
    using NopSolutions.NopCommerce.Web;

    public class CaaS
    {
        orderRecord payment47 = new orderRecord();
        public bool getPDTDetails(int tx,out orderRecord values)
        {
            canonicalRequestResponse res = new canonicalRequestResponse();

            if (payment47 != null && payment47.id == tx)
            {
                values = payment47;
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        internal bool getPDTDetails(int tx, orderRecord values)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NopSolutions.NopCommerce.Web
{
    using NopSolutions.NopCommerce.Payment.Methods.PayPal;

    public partial class PayPalStandardReturn1
    {
        public canonicalRequestResponse Response;
        public Page Page;
        public canonicalRequestResponse Request;

        public PayPalStandardReturn1()
        {
            Request = new canonicalRequestResponse();
            Response = new canonicalRequestResponse();
            Page = new Page();
        }

        public void Page_Load(object sender, EventArgs e)
        {
            int tx = Convert.ToInt32(CommonHelper.QueryStringInt("tx"));
            Dictionary<string, string> values = null;
            string response;
            string digest_paypal;
            if (PayPalStandardPaymentProcessor1.getPDTDetails(tx, out GlobalState.payment))
            {
                string orderNumber = string.Empty;
                values.TryGetValue("custom", out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = OrderManager.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    decimal total = decimal.Zero;
                    try
                    {
                        total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                    }
                    catch (Exception exc)
                    {

                    }
                    if (order.OrderTotal != total)
                    {
                        Contract.Assume(false);
                    }

                    string payer_status = string.Empty;
                    values.TryGetValue("payer_status", out payer_status);
                    string payment_status = string.Empty;
                    values.TryGetValue("payment_status", out payment_status);
                    string pending_reason = string.Empty;
                    values.TryGetValue("pending_reason", out pending_reason);
                    string mc_currency = string.Empty;
                    values.TryGetValue("mc_currency", out mc_currency);
                    string txn_id = string.Empty;
                    values.TryGetValue("txn_id", out txn_id);
                    string payment_type = string.Empty;
                    values.TryGetValue("payment_type", out payment_type);
                    string payer_id = string.Empty;
                    values.TryGetValue("payer_id", out payer_id);
                    string receiver_id = string.Empty;
                    values.TryGetValue("receiver_id", out receiver_id);
                    string invoice = string.Empty;
                    values.TryGetValue("invoice", out invoice);
                    string payment_fee = string.Empty;
                    values.TryGetValue("payment_fee", out payment_fee);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Paypal PDT:");
                    sb.AppendLine("total: " + total);
                    sb.AppendLine("Payer status: " + payer_status);
                    sb.AppendLine("Payment status: " + payment_status);
                    sb.AppendLine("Pending reason: " + pending_reason);
                    sb.AppendLine("mc_currency: " + mc_currency);
                    sb.AppendLine("txn_id: " + txn_id);
                    sb.AppendLine("payment_type: " + payment_type);
                    sb.AppendLine("payer_id: " + payer_id);
                    sb.AppendLine("receiver_id: " + receiver_id);
                    sb.AppendLine("invoice: " + invoice);
                    sb.AppendLine("payment_fee: " + payment_fee);

                    OrderManager.InsertOrderNote(order.OrderId, sb.ToString(), false, DateTime.UtcNow);
                }

                if (GlobalState.payment != null)
                {
                    if (GlobalState.order.gross != GlobalState.payment.gross)
                    {
                        Contract.Assume(false);
                    }
                    if (GlobalState.MerchantPaymentEmail != GlobalState.payment.payee)
                    {
                        Contract.Assume(false);
                    }
                    if (GlobalState.order.id == GlobalState.payment.id)
                    {
                        GlobalState.order.status = Global.OrderStatusEnum.Paid;
                    }
                    else
                    {
                        Contract.Assume(false);
                    }
                }
            }
            else
            {
                Contract.Assume(false);
            }
        }
    }
}

namespace NopSolutions.NopCommerce.Payment.Methods.PayPal
{
    using NopSolutions.NopCommerce.Web;
    public class PayPalStandardPaymentProcessor1
    {
	    public static bool getPDTDetails(int tx, out orderRecord values) {
            values = null;
		    return GlobalState.paypal.getPDTDetails(tx,values);
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
        PayPalStandardPaymentProcessor1 merchant_sender = new PayPalStandardPaymentProcessor1();
        
        PayPalStandardReturn1 merchant_receiver = new PayPalStandardReturn1();
        Order init_order = p.NondetOrder();
        GlobalState.order = new orderRecord();
        GlobalState.payment = new orderRecord();
        canonicalRequestResponse res_placeorder = p.NodetReqres();
        canonicalRequestResponse req_pay = p.NodetReqres();
        canonicalRequestResponse res_pay = p.NodetReqres();
        canonicalRequestResponse req_finish = p.NodetReqres();

        // Message: CaaS -> client -> merchant
        // Computation on merchant - finish order
        merchant_receiver.Page_Load(null, null);

        Contract.Assert(GlobalState.order.id == GlobalState.payment.id);
        Contract.Assert(GlobalState.MerchantPaymentEmail == GlobalState.payment.payee);
        Contract.Assert(GlobalState.order.gross == GlobalState.payment.gross);
        Contract.Assert(GlobalState.order.status == Global.OrderStatusEnum.Paid);
    }
}