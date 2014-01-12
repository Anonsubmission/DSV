using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace caas
{
    public partial class pay : System.Web.UI.Page
    {
        protected void payComputation()
        {
            int payment_count = globalState.payment_count;
            globalState.payments[payment_count].validity = true;
            globalState.payments[payment_count].payee = (PARTY_ID) Enum.Parse(typeof(PARTY_ID), req.signer);
            globalState.payments[payment_count].gross = req.gross;
            globalState.payments[payment_count].orderID = req.orderID;
            globalState.payment_count++;

            res.redirectionURL = req.returnURL;
            res.gross = req.gross;
            res.orderID = req.orderID;
            res.status = Status.Paid;
            composeAndSignPayResponse("CaaS[[" + globalState.hashvalue_pay + "(" + Request.Params["symVal"] + ")]]");
        }

       
    }
}