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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.PayPal;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.Audit;

//Rui
using System.Diagnostics;

namespace NopSolutions.NopCommerce.Web
{
    public partial class PaypalPDTHandlerPage : BaseNopPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CommonHelper.SetResponseNoCache(Response);

            if (!Page.IsPostBack)
            {
                string tx = CommonHelper.QueryString("tx");
                Dictionary<string, string> values;
                string response;

                //Rui
                string digest_paypal;

                PayPalStandardPaymentProcessor processor = new PayPalStandardPaymentProcessor();
                if (processor.GetPDTDetails(tx, out values, out response, out digest_paypal))   //Rui
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
                            LogManager.InsertLog(LogTypeEnum.OrderError, "PayPal IPN. Error getting orderGUID", exc);
                        }

    //Rui:begin
                        if (order.OrderTotal != total) return;
    //Rui:end

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

 //RUI begin
                        string SourceCode_FinishOrder = @"
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
	        int tx = Convert.ToInt32(CommonHelper.QueryStringInt(""tx""));
            int witness;
            payment_record payment;
            
            orderRecord order;
            
            Dictionary<string, string> values = null;
            if ((witness=PayPalStandardPaymentProcessor1.getPDTDetails(tx, out payment))>=0)
            {
                string orderNumber = string.Empty;
                values.TryGetValue(""custom"", out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                
                try
                    {
                        orderNumberGuid = new Guid(orderNumber);
                    }
                    catch { }
                    //Order order = OrderManager.GetOrderByGuid(orderNumberGuid);

                    //payment.orderID is essentially the custom field in PDT
                    Contract.Assume(checkedOut_orderID == payment.orderID);  //this should be an assignment  ""checkedOut_orderID = payment.orderID"", but the support for ""out parameter"" is wierd 
                    order = GlobalState.tstore.orders[checkedOut_orderID];
                    Contract.Assume(order.id == checkedOut_orderID);

                    if (order != null)
                    {
                        decimal total = decimal.Zero;
                        try
                        {
                            total = decimal.Parse(values[""mc_gross""], new CultureInfo(""en-US""));
                        }
                        catch (Exception exc)
                        {
                            
                        }
                        if (order.gross != total) {
                            Contract.Assume(false);
                        }

                        string payer_status = string.Empty;
                        values.TryGetValue(""payer_status"", out payer_status);
                        string payment_status = string.Empty;
                        values.TryGetValue(""payment_status"", out payment_status);
                        string pending_reason = string.Empty;
                        values.TryGetValue(""pending_reason"", out pending_reason);
                        string mc_currency = string.Empty;
                        values.TryGetValue(""mc_currency"", out mc_currency);
                        string txn_id = string.Empty;
                        values.TryGetValue(""txn_id"", out txn_id);
                        string payment_type = string.Empty;
                        values.TryGetValue(""payment_type"", out payment_type);
                        string payer_id = string.Empty;
                        values.TryGetValue(""ayer_id"", out payer_id);
                        string receiver_id = string.Empty;
                        values.TryGetValue(""receiver_id"", out receiver_id);
                        string invoice = string.Empty;
                        values.TryGetValue(""invoice"", out invoice);
                        string payment_fee = string.Empty;
                        values.TryGetValue(""payment_fee"", out payment_fee);

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(""Paypal PDT:"");
                        sb.AppendLine(""total: "" + total);
                        sb.AppendLine(""Payer status: "" + payer_status);
                        sb.AppendLine(""Payment status: "" + payment_status);
                        sb.AppendLine(""Pending reason: "" + pending_reason);
                        sb.AppendLine(""mc_currency: "" + mc_currency);
                        sb.AppendLine(""txn_id: "" + txn_id);
                        sb.AppendLine(""payment_type: "" + payment_type);
                        sb.AppendLine(""payer_id: "" + payer_id);
                        sb.AppendLine(""receiver_id: "" + receiver_id);
                        sb.AppendLine(""invoice: "" + invoice);
                        sb.AppendLine(""payment_fee: "" + payment_fee);

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
";
                        Debug.WriteLine("PDTDigest=" + digest_paypal + "\n");
                        // Boogie check
                        string old_hash = CommonHelper.QueryString("path_digest"); //get digest from query string
                        string new_hash = string.Empty;
                        new_hash = PaypalHelper.code_to_hash(SourceCode_FinishOrder);
                        string path_digest = "Merchant[" + new_hash + "((CaaS[" + digest_paypal + "(" + old_hash + ")]))]";
                        Debug.WriteLine("path_digest=" + path_digest + "\n");

                        PaypalHelper.generate_cs_file_from_symval(path_digest);

                        if (!PaypalHelper.checkLogicProperty()) return;
  //RUI end


                        if (OrderManager.CanMarkOrderAsPaid(order))
                        {
                            OrderManager.MarkOrderAsPaid(order.OrderId);
                        }
                    
                    }
                    Response.Redirect("~/checkoutcompleted.aspx");
                }
                else
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
                        OrderManager.InsertOrderNote(order.OrderId, "PayPal PDT failed. " + response, false, DateTime.UtcNow);
                    }
                    Response.Redirect(CommonHelper.GetStoreLocation());
                }
            }
        }

        public override bool AllowGuestNavigation
        {
            get
            {
                return true;
            }
        }
    }
}