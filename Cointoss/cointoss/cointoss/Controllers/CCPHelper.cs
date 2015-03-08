using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace cointoss.Controllers
{
    public static class CCPHelper
    {
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://ericchen.me:81/"; //ERIC'S IP
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";
        static string[] whitelist = new string[4] { "GamblingSite", "CaaS", "OAuth", "CoinTosser" };
        static string root = "C:\\CCP\\teamproject\\Cointoss-verify\\NopCommerce";
        
        
        public static string HttpReq(string url, string post, string method, string refer = "")
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


        //this function parses the symval string and returns an array of functions called in reverse-chronological order
        // A[HASH1(B[[HASH2()]])]
        public class transaction
        {
            public string party;
            public string function;
            public bool isProtected = true;
        }

        public static Stack<transaction> parse_digest(string symval)
        {
            Stack<transaction> callStack = new Stack<transaction>();

            int step = 0, start = 0, end = symval.Length;
            int c = 0, s = 0;
            bool isSigned = false, isOuterLayerProtected = true;
            transaction cur = new transaction();

            while (start < end)
            {

                step = 0;
                c = (symval.IndexOf('(', start) == -1) ? int.MaxValue : symval.IndexOf('(', start);
                s = (symval.IndexOf('[', start) == -1) ? int.MaxValue : symval.IndexOf('[', start);

                if (c < s)
                { //if ( is before [
                    if (symval[c + 1] == '(')
                    {
                        step = 2; // double '('
                    }
                    else
                    {
                        step = 1;
                        if (!isSigned) isOuterLayerProtected = false; //A[[hash(protected)]], A[hash((protected))], A[hash(not protected)]
                    }

                    start += step;
                    end -= step;

                    if (!isOuterLayerProtected) cur.isProtected = false;

                    callStack.Push(cur);
                    isSigned = false;


                }
                else if (s < c)
                { //if [ is before (
                    cur = new transaction();
                    if (symval[s + 1] == '[') step = 2; // double '['
                    else step = 1;
                    if (c < int.MaxValue)
                    { // pattern: A[HASH(
                        isSigned = (step == 1) ? false : true; //A[[hash(signed)]], A[hash(unsigned)]
                        cur.party = symval.Substring(start, s - start); //A[[HASH()]], a is party
                        cur.function = symval.Substring(s + step, c - s - step); //A[[HASH()]] HASH is function

                        //if one of the parties involved in the transaction is not known
                        if (!Array.Exists(whitelist, element => element == cur.party))
                        {
                            break;
                        }

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


        public static string assemble_code(string symT)
        {

            Stack<transaction> callstack = parse_digest(symT);
            string code = "";

            foreach (transaction trans in callstack)
            {
                if (trans.isProtected) code +=  hash_to_code(trans.function);
            }

            //assemble main
            code += @"
class PoirotMain
{
    static Picker p;

    static void Main()
    {
        
        var controller = new HomeController1();

        CanonicalRequestResponse req1 = p.NondetReqResp();
        CanonicalRequestResponse resp1 = p.NondetReqResp();
        CanonicalRequestResponse req2 = p.NondetReqResp();
        CanonicalRequestResponse resp2 = p.NondetReqResp(); 
        CanonicalRequestResponse req3 = p.NondetReqResp();
        CanonicalRequestResponse resp3 = p.NondetReqResp(); 
        CanonicalRequestResponse req4 = p.NondetReqResp();
        CanonicalRequestResponse resp4 = p.NondetReqResp(); 
        CanonicalRequestResponse req5 = p.NondetReqResp();
        CanonicalRequestResponse resp5 = p.NondetReqResp(); 
        CanonicalRequestResponse req6 = p.NondetReqResp();
        CanonicalRequestResponse resp6 = p.NondetReqResp();  
        CanonicalRequestResponse req7 = p.NondetReqResp();       
";

            transaction step1 = (transaction)callstack.Pop();
            transaction step2 = (transaction)callstack.Pop();
            transaction step3 = (transaction)callstack.Pop();
            transaction step4 = (transaction)callstack.Pop();
            transaction step5 = (transaction)callstack.Pop();
            transaction step6 = (transaction)callstack.Pop();
            transaction step7 = (transaction)callstack.Pop();

            if (step1.isProtected)
            {
                code += @"
                resp1 = controller.agnosticSimplePayReq(req1);
                req2 = resp1;
";
            }

            if (step2.isProtected)
            {
                code += @"
                resp2 = controller.agnosticSimplePayResp(req2);
                req3 = resp2;
";
            }

            if (step3.isProtected)
            {
                code += @"
                resp3 = controller.agnosticOAuth(req3);
                req4 = resp3;
";
            }

            if (step4.isProtected)
            {
                code += @"
                resp4 = controller.agnosticVerifyTokenRequest(req4);
                req5 = resp4;
";
            }

            if (step5.isProtected)
            {
                code += @"
                resp5 = controller.agnosticVerifyToken(req5);
                req6 = resp5;
";
            }

            if (step6.isProtected)
            {
                code += @"
                resp6 = controller.agnosticFinalGamblingRequest(req6);
                req7 = resp6;
";
            }

            if (step7.isProtected)
            {
                code += @"
                controller.agnosticGamblingSite(req7);
";
            }


            code += @"

        Contract.Assert(OAuthStates.records[0].EffectiveResult != ""untossed"");
        Contract.Assert(GamblingSite.bets[req7.id].guess == OAuthStates.records[0].EffectiveResult);
        Contract.Assert(OAuthStates.records[0].betID == req7.id);

        Contract.Assert(Contract.Exists(0, SimplePay.payments.Length, i=>
                SimplePay.payments[i].gross == GamblingSite.bets[req7.id].amount &&
                SimplePay.payments[i].orderID == req7.id &&
                SimplePay.payments[i].payee == GamblingSite.AccountID
            ));

    }
}       
";

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

        public static string SimplePayReq_code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Amazon;
using System.Globalization;
using System.Net;
using System.Collections.Specialized;
using cointoss.Models;
using System.Diagnostics.Contracts;

using cointoss.Controllers;
using Global;



interface Picker
{
    int NondetInt();
    string NondetString();
    Boolean NondetBool();
    CanonicalRequestResponse NondetReqResp();
}

namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        Picker p;
        static string site_root = ""http://localhost:2631"";
        static string recipient_email = ""cs0317b@gmail.com"";
        private TokenDBContext tokenDb = new TokenDBContext();
        private BetDbContext betDb = new BetDbContext();

        public CanonicalRequestResponse agnosticSimplePayReq(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();
            
            var bets = betDb.Bets;
            foreach (var b in bets)
            {
                betDb.Bets.Remove(b);
            }

            Bet bet = new Bet();
            bet.guess = req.flip;
            bet.amount = req.price;
            betDb.Bets.Add(bet);
            betDb.SaveChanges();
            
            //protocol agnostic code
            GamblingSite.AccountID = ""cs0317b@gmail.com"";
            GamblingSite.bets[0].guess = req.flip;
            GamblingSite.bets[0].amount = req.price;

            resp.id = 0;
            resp.price = req.price;
            resp.payee = ""cs0317b@gmail.com"";


            //Contract.Assert(GamblingSite.bets[0].amount == resp.price);

            //1. redirect to simplepay
            RemotePost post = new RemotePost();
            post.FormName = ""SimplePay"";
            post.Url = ""https://authorize.payments-sandbox.amazon.com/pba/paypipeline"";
            post.Method = ""POST"";

            post.Add(""immediateReturn"", ""1"");
            post.Add(""signatureVersion"", ""2"");
            post.Add(""signatureMethod"", ""HmacSHA256"");
            post.Add(""accessKey"", ""AKIAJB4XJRGX6XRRVIDA"");
            post.Add(""amount"", String.Format(CultureInfo.InvariantCulture, ""USD {0:0.00}"", req.price));
            post.Add(""description"", req.flip);
            post.Add(""amazonPaymentsAccountId"", ""IGFCUTPWGXVM311K1E6QTXIQ1RPEIUG5PTIMUZ"");
            post.Add(""returnUrl"", site_root + ""/Home/SimplePayResp?symT=A[[HASH1()]]"");
            post.Add(""processImmediate"", ""1"");
            post.Add(""referenceId"", Convert.ToString(bet.ID));
            //the entire msg is signed using the pre-decided simplepay secret key
            post.Add(""signature"", AmazonHelper.SignParameters(post.Params,
                        ""WfZ3JnrY8mpJ8DZ7VlL07+RYtWznX3PWHNV8Zj5M"", //simplePay secret key
                        post.Method,
                        ""authorize.payments-sandbox.amazon.com"",
                        ""/pba/paypipeline""));
            post.Post();

            
            
            return resp;

        }
    }
}
";

        public static string SimplePayResp_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public CanonicalRequestResponse agnosticSimplePayResp(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();

            int i;
            i = p.NondetInt();
            Contract.Assume(0 <= i && i < SimplePay.payments.Length);

            //protocal agnostic code. 
            //the code below should be executing on amazon's servers
            SimplePay.payments[i].orderID = req.id;
            SimplePay.payments[i].payee = req.payee;
            SimplePay.payments[i].gross = req.price;
            
            
            resp.id = req.id;
            resp.price = req.price;
            resp.payee = req.payee;

            //Contract.Assert(resp.price == GamblingSite.bets[0].amount);

            return resp;

        }
    }
}
";
        public static string OAuth_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public CanonicalRequestResponse agnosticOAuth(CanonicalRequestResponse req)
        {

            //payment is successful, we can issue OAuth token
            Token token = new Token();
            token.OAuthToken = ""TestOAuthToken"";
            token.EffectiveResult = ""untossed"";
            token.Cost = Convert.ToString(req.price);
            token.BetID = Convert.ToString(req.id);
            //pass info to front end

            ViewBag.token = token.OAuthToken;
            ViewBag.price = token.Cost;
            ViewBag.guess = token.InitialGuess;
            ViewBag.betID = token.BetID;

            tokenDb.Tokens.Add(token);
            tokenDb.SaveChanges();

            //protocol agnostic code
            OAuthStates.records[0].token = token.OAuthToken;
            OAuthStates.records[0].EffectiveResult = token.EffectiveResult;
            OAuthStates.records[0].betID = req.id;

            CanonicalRequestResponse resp = new CanonicalRequestResponse();
            resp.token = token.OAuthToken;
            resp.id = req.id;

            return resp;

        }
    }
}
";
        public static string VerifyTokenReq_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public CanonicalRequestResponse agnosticVerifyTokenRequest(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();
            resp.result = ""head"";
            resp.id = req.id;
            resp.token = req.token;
            return resp;
        }
    }
}
";

        public static string VerifyToken_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public CanonicalRequestResponse agnosticVerifyToken(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();

            

            if (req.token == OAuthStates.records[0].token && req.id == OAuthStates.records[0].betID)
            {
                resp.result = req.result;
                resp.id = req.id;
                resp.token = req.token;
                OAuthStates.records[0].EffectiveResult = req.result;
            }
            else resp.result = """";
            
            return resp;
        }
    }
}
";

        public static string GamblingReq_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public CanonicalRequestResponse agnosticFinalGamblingRequest(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();

            if (req.result != """")
            {
                resp.result = req.result;
                resp.id = req.id;
                resp.token = req.token;
            }
            else
            {
                Contract.Assume(false);
            }

            Contract.Assert(req.token == OAuthStates.records[0].token);
            Contract.Assert(req.id == OAuthStates.records[0].betID);

            return resp;
        }
    }
}
";

        public static string GamblingSite_code = @"
namespace cointoss.Controllers
{
    public partial class HomeController1 : Controller
    {
        public void agnosticGamblingSite(CanonicalRequestResponse req)
        {
            if (GamblingSite.bets[req.id].guess != req.result)
            {
                Contract.Assume(false);     
            }

        }
    }
}
";
    }
}