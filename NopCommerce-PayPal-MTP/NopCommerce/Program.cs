
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
    public static merchant_state tstore = new merchant_state(100, "TStore.com");
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


namespace NopSolutions.NopCommerce.Payment.Methods.PayPal
{
    public class payment_record
    {
        public int gross;
        public int orderID;
        public CaasReturnStatus status;
        public string payee;
        public int tx;
    }
    public class caas_state
    {
        public payment_record[] payments;
        public caas_state(int m)
        {
            payments = new payment_record[m];
        }
    }
    
    public partial class CaaS
    {
        Picker p;
        public caas_state caas = new caas_state(100);
        public int getPDTDetails(string identity, int tx, out payment_record values)
        {
            int i;
            i = p.NondetInt();
            if (identity != caas.payments[i].payee)
                Contract.Assume(false);
            Contract.Assume(0 <= i && i < caas.payments.Length && caas.payments[i].tx == tx);
            values = caas.payments[i];
           
            return i;
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
        Picker p;
        public PayPalStandardReturn1()
        {
            Request = new canonicalRequestResponse();
            Response = new canonicalRequestResponse();
            Page = new Page();
        }

        public int Page_Load(object sender, EventArgs e, int checkedOut_orderID)
        {
	        int tx = Convert.ToInt32(CommonHelper.QueryStringInt("tx"));
            int witness;
            payment_record payment;
            
            orderRecord order;
            
            Dictionary<string, string> values = null;
            if ((witness=PayPalStandardPaymentProcessor1.getPDTDetails(tx, out payment))>=0)
            {
                string orderNumber = string.Empty;
                values.TryGetValue("custom", out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                
                try
                    {
                        orderNumberGuid = new Guid(orderNumber);
                    }
                    catch { }
                    //Order order = OrderManager.GetOrderByGuid(orderNumberGuid);

                    //payment.orderID is essentially the custom field in PDT
                    Contract.Assume(checkedOut_orderID == payment.orderID);  //this should be an assignment  "checkedOut_orderID = payment.orderID", but the support for "out parameter" is wierd 
                    order = GlobalState.tstore.orders[checkedOut_orderID];
                    Contract.Assume(order.id == checkedOut_orderID);

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
                        if (order.gross != total) {
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
                        values.TryGetValue("ayer_id", out payer_id);
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

                        OrderManager.InsertOrderNote(order.id, sb.ToString(), false, DateTime.UtcNow);
                    }

	                if (order.gross != payment.gross)
                    {
                        Contract.Assume(false);
                    }
 
                   if (payment.status!=CaasReturnStatus.Sucess)
                        Contract.Assume(false);
                   order.status = Global.OrderStatusEnum.Paid;
            } else {
                Contract.Assume(false);
            }
            return witness;
        }
    }
}

namespace NopSolutions.NopCommerce.Payment.Methods.PayPal
{
    using NopSolutions.NopCommerce.Web;
    public class PayPalStandardPaymentProcessor1
    {
        public static int getPDTDetails(int tx, out payment_record values)
        {
            values = null;
            return GlobalState.paypal.getPDTDetails(GlobalState.tstore.myAccount,tx, out values);
	    }
    }
}

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

        // Computation on merchant - place order
        res_placeorder = merchant_sender.PostProcessPayment(init_order);
        // Message: merchant -> client -> CaaS
        // is the msg encrypted? if so, then:
        req_pay = res_placeorder;
        GlobalState.MerchantPaymentEmail = req_pay.payee;
        GlobalState.order.gross = req_pay.gross;
        GlobalState.order.id = req_pay.orderID;

        GlobalState.paypal.pay(req_pay); //Rui: there is no return for this call, because anything returned through the client should be havoced

         req_finish.orderID = p.NondetInt();
         witness = merchant_receiver.Page_Load(null, null, req_finish.orderID);

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
}