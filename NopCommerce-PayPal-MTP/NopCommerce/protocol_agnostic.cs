using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;




namespace Global
{

    public enum OrderStatusEnum : int
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 10,
        /// <summary>
        /// Processing
        /// </summary>
        Processing = 20,
        /// <summary>
        /// Complete
        /// </summary>
        Paid = 30,
        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled = 40
    }

    public enum CaasReturnStatus : int
    {
        Sucess,
        Failure
    }

    public class orderRecord
    {
        public int id;
        public int gross;
        public OrderStatusEnum status;
	public string payee; // payee's email
    }

    public class canonicalRequestResponse
    {
        public string CaaSUrl; // Url to send the user to when they place their order (useless for us?)
        public int orderID;
        public int gross;
        public string MerchantReturnURL; //returnURL is set by merchant
        public CaasReturnStatus status;
        public string payee; // payee's email
        public string signature;
        public string signer;

        //public NameValueCollection QueryString;

        public void Redirect(string tmp) { 
            //dummy function 
        }
    }

    public partial class RemotePostProxy
    {
        public canonicalRequestResponse req;
        public string FormName, Method;
        public NameValueCollection Params;

        public RemotePostProxy()
        {
            this.req = new canonicalRequestResponse();
        }

        public void Add(string key, string value)
        {
            if (key == "referenceId")
                this.req.orderID = Convert.ToInt32(value);
            else if (key == "amount")
                this.req.gross = Convert.ToInt32(value);
            else if (key == "returnUrl")
                this.req.MerchantReturnURL = value;
            else if (key == "signature")
                this.req.signature = value;
           
        }

        public string Url
        {
            get { return this.req.CaaSUrl; }
            set { this.req.CaaSUrl = value; }
        }

        public canonicalRequestResponse Post()
        {
            return this.req;
        }
    }
   
}

