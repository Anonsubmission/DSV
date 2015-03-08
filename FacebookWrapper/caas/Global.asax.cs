using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;

using NopSolutions.NopCommerce.Common.Utils;


namespace caas
{
    public enum PARTY_ID
    {
        CaaS,
        TStore,
        Other
    };


    public enum API_ID
    {
        API_CaaS_pay,
        API_TStore_placeOrder,
        API_TStore_finishOrder
    };

    public enum Status
    {
        Invalid,
        Open,
        Pending,
        Paid
    };

    public enum PaymentStatus
    {
        Invalid,
        Open,
        Paid
    };

    public class paymentRecord
    {
        public int gross;
        public PARTY_ID payee;
        public int orderID;
        public bool validity;
    }

    public class orderRecord
    {
        public int gross;
        public Status status;
    }

    public class GlobalState
    {

        public int CallCount;
        public string[] API_names;
        public string[] payment_status_names;
        public orderRecord[] orders;
        public int getGrossOfOrder(int orderID)
        {
            return orders[orderID].gross;
        }

        public paymentRecord[] payments;
        public int payment_count;
        public string[] status_names;
        public SHA1 sha;
        public string hashvalue_pay;
        public RSACryptoServiceProvider rsaProvider;
    }

    public class Global : System.Web.HttpApplication
    {
        /*
        //public string[] id_map = new string[] { "CaaS", "TStore", "Other" };
        public static Dictionary<string, int> id_map = new Dictionary<string, int>()
        {
            {"CaaS",0},
            {"TStore",1},
            {"Other",2}
        };
        */

        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string root = "D:\\codeplex\\eric";
        static string dehash_server_host = "http://10.81.125.170/";
        static string upload_path = "research/verification/upload.php";
        static string dehash_path = "research/verification/dehash.php";

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
        protected static string hash_to_code(string hash)
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
                code = split[1];
            }

            return code;
        }

        //this function converts a piece of code to a hash
        protected static string code_to_hash(string code)
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


        void Application_Start(object sender, EventArgs e)
        {

     
          
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }

        void Session_Start(object sender, EventArgs e)
        {
            

        }

        void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.

        }

        public static byte[] convertFromStringToBytes(string str)
        {
            String[] arr = str.Split('-');
            byte[] array = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++) array[i] = Convert.ToByte(arr[i], 16);
            return array;
        }

        public static bool verifySignature(string url)
        {
            string signer,signature;
 
            int i = url.IndexOf("&signature=") ;
            if (i == -1)
            {
                return false;
            }
            string plainText = url.Substring(0,i);
            i += "&signature=".Length;
            signature = url.Substring(i);
            if (signature.IndexOf('&')!=-1) {
                return false;
            }
            i = signature.IndexOf('-');
            signer = signature.Substring(0,i);
            signature = signature.Substring(i + 1);

            CspParameters cspParams = null;
            RSACryptoServiceProvider rsaProvider = null;
            StreamReader publicKeyFile = null;
            string publicKeyText = "";
            byte[] signatureBytes = null;
            try
            {
                cspParams = new CspParameters();
                cspParams.ProviderType = 1;// PROV_RSA_FULL;
                rsaProvider = new RSACryptoServiceProvider(384, cspParams);

                // Read public key from file
                publicKeyFile = File.OpenText(root+"\\RSAKeys\\pubkey_"+signer+".xml");
                publicKeyText = publicKeyFile.ReadToEnd();

                // Import public key
                rsaProvider.FromXmlString(publicKeyText);

                signatureBytes = convertFromStringToBytes(signature);
                bool result= rsaProvider.VerifyData(Encoding.Unicode.GetBytes(plainText),new SHA1CryptoServiceProvider(),
                    signatureBytes);
                return result;
            }
            catch (Exception ex)
            {
                Console.Write("Fail to initialize RSA provider.");
            }
            
            return true;
        }


    }
}
