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
    public class HomeController1 : Controller
    {
        Picker p;
        static string site_root = "http://localhost:2631";
        static string recipient_email = "cs0317b@gmail.com";
        private TokenDBContext tokenDb = new TokenDBContext();
        private BetDbContext betDb = new BetDbContext();

        //stub function
        public string FetchVerifyTokenResult(string token, string result)
        {
             //dummy return, this function should send out a direct http request
             return "";
        }

        public CanonicalRequestResponse agnosticVerifyTokenRequest(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();
            resp.result = "head";
            resp.id = req.id;
            resp.token = req.token;
            return resp;
        }

        public CanonicalRequestResponse agnosticFinalGamblingRequest(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();

            if (req.result != "")
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

        public ActionResult CoinTosser(string token, int id)
        {
            var client = new WebClient();
            
            //protocol agnostic code
            CanonicalRequestResponse req = new CanonicalRequestResponse();
            req.token = token;
            req.id = id;

            CanonicalRequestResponse req1 = agnosticVerifyTokenRequest(req);

            string OAuthResponse = FetchVerifyTokenResult(req1.token, req1.result);

            CanonicalRequestResponse resp1 = new CanonicalRequestResponse();
            resp1.result = OAuthResponse;

            agnosticFinalGamblingRequest(resp1);

            if (resp1.result != "")
            {
                ViewBag.result = req.result;
                //make a remote post back to gaming site
                RemotePost post = new RemotePost();
                post.FormName = "Cointoss";
                post.Url = site_root + "/Home/Index";
                post.Method = "POST";
                post.Add("result", OAuthResponse);
                post.Add("id", Convert.ToString(id));
                post.Post();
            }

            return View();
        }

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
            else resp.result = "";
            
            return resp;
        }

        
        public string VerifyToken()
        {
           
            NameValueCollection parameters = new NameValueCollection(Request.QueryString);
            string strToken = parameters["token"];
            var token = tokenDb.Tokens
                            .Where(t => t.OAuthToken == strToken)
                            .FirstOrDefault();
            CanonicalRequestResponse req = new CanonicalRequestResponse();
            req.result = parameters["result"];
            req.token = strToken;

            // this function has no actual use, it's there for the proof
            agnosticVerifyToken(req);

            if (token != null && token.EffectiveResult == "untossed")
            {
                token.EffectiveResult = parameters["result"];
                tokenDb.SaveChanges();
      
                return token.EffectiveResult;
            }

            return "";
        }
         

        public CanonicalRequestResponse agnosticOAuth(CanonicalRequestResponse req)
        {

            //payment is successful, we can issue OAuth token
            Token token = new Token();
            token.OAuthToken = "TestOAuthToken";
            token.EffectiveResult = "untossed";
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

        public ActionResult OAuth()
        {

            //first, reset the DB
            var tokens = tokenDb.Tokens;
            foreach (var token in tokens)
            {
                tokenDb.Tokens.Remove(token);
            }

            NameValueCollection parameters = new NameValueCollection(Request.QueryString);
            CanonicalRequestResponse req = new CanonicalRequestResponse();
            req.price = Convert.ToInt32(parameters["transactionAmount"]);
            req.id = Convert.ToInt32(parameters["referenceId"]);
            if (parameters["status"] == "PS" && parameters["recipientEmail"] == recipient_email)
            {
                agnosticOAuth(req);
            }

            return View();
        }


        public void agnosticGamblingSite(CanonicalRequestResponse req)
        {
            if (GamblingSite.bets[req.id].guess != req.result)
            {
                Contract.Assume(false);     
            }

        }

        public ActionResult Index(string result, string id, string token)
        {

            //this if statement will trigger on the last step of the protocol
            if (result == "head" || result == "tail")
            {
                int i = Convert.ToInt32(id);
                Bet bet = betDb.Bets.Where(b => b.ID == i).FirstOrDefault();

                CanonicalRequestResponse req = new CanonicalRequestResponse();
                req.result = result;
                req.id = Convert.ToInt32(id);
                
                
                ViewBag.result = result;
                ViewBag.bet = bet.amount;
                ViewBag.guess = bet.guess;
            }
                     

            return View();
        }

        public CanonicalRequestResponse agnosticSimplePayResp(CanonicalRequestResponse req)
        {
            CanonicalRequestResponse resp = new CanonicalRequestResponse();

            //protocal agnostic code. 
            //the code below should be executing on amazon's servers
            SimplePay.orderID = req.id;
            SimplePay.payee = req.payee;
            SimplePay.price = req.price;
            
            
            resp.id = req.id;
            resp.price = req.price;
            resp.payee = req.payee;

            //Contract.Assert(resp.price == GamblingSite.bets[0].amount);

            return resp;

        }


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
            GamblingSite.AccountID = "cs0317b@gmail.com";
            GamblingSite.bets[0].guess = req.flip;
            GamblingSite.bets[0].amount = req.price;

            resp.id = 0;
            resp.price = req.price;
            resp.payee = "cs0317b@gmail.com";


            //Contract.Assert(GamblingSite.bets[0].amount == resp.price);

            //1. redirect to simplepay
            RemotePost post = new RemotePost();
            post.FormName = "SimplePay";
            post.Url = "https://authorize.payments-sandbox.amazon.com/pba/paypipeline";
            post.Method = "POST";

            post.Add("immediateReturn", "1");
            post.Add("signatureVersion", "2");
            post.Add("signatureMethod", "HmacSHA256");
            post.Add("accessKey", "AKIAJB4XJRGX6XRRVIDA");
            post.Add("amount", String.Format(CultureInfo.InvariantCulture, "USD {0:0.00}", req.price));
            post.Add("description", req.flip);
            post.Add("amazonPaymentsAccountId", "IGFCUTPWGXVM311K1E6QTXIQ1RPEIUG5PTIMUZ");
            post.Add("returnUrl", site_root + "/Home/SimplePayResp?path_digest=A[[HASH1()]]");
            post.Add("processImmediate", "1");
            post.Add("referenceId", Convert.ToString(bet.ID));
            //the entire msg is signed using the pre-decided simplepay secret key
            post.Add("signature", AmazonHelper.SignParameters(post.Params,
                        "WfZ3JnrY8mpJ8DZ7VlL07+RYtWznX3PWHNV8Zj5M", //simplePay secret key
                        post.Method,
                        "authorize.payments-sandbox.amazon.com",
                        "/pba/paypipeline"));
            post.Post();

            
            
            return resp;

        }

        public void SimplePayReq(int amount, string flip)
        {
            CanonicalRequestResponse req = new CanonicalRequestResponse();
            req.price = amount;
            req.flip = flip;
            agnosticSimplePayReq(req);
        }
    }
}


class PoirotMain
{
    static Picker p;

    static void Main()
    {
        
        var controller = new HomeController1();

        CanonicalRequestResponse req1 = p.NondetReqResp();
        CanonicalRequestResponse resp1 = p.NondetReqResp();
        
        resp1 = controller.agnosticSimplePayReq(req1);

        CanonicalRequestResponse req2 = resp1;
        CanonicalRequestResponse resp2 = p.NondetReqResp();
        resp2 = controller.agnosticSimplePayResp(req2);

        CanonicalRequestResponse req3 = resp2;
        CanonicalRequestResponse resp3 = p.NondetReqResp(); 
        resp3 = controller.agnosticOAuth(req3);

        CanonicalRequestResponse req4 = resp3;
        CanonicalRequestResponse resp4 = p.NondetReqResp(); 
        resp4 = controller.agnosticVerifyTokenRequest(req4);

        CanonicalRequestResponse req5 = resp4;
        CanonicalRequestResponse resp5 = p.NondetReqResp();
        resp5 = controller.agnosticVerifyToken(req5);

        CanonicalRequestResponse req6 = resp5;
        CanonicalRequestResponse resp6 = p.NondetReqResp(); 
        resp6 = controller.agnosticFinalGamblingRequest(req6);

        CanonicalRequestResponse req7 = resp6;
        controller.agnosticGamblingSite(req7);
        
        
        Contract.Assert(OAuthStates.records[0].EffectiveResult != "untossed");
        Contract.Assert(GamblingSite.bets[req7.id].guess == OAuthStates.records[0].EffectiveResult);
        Contract.Assert(OAuthStates.records[0].betID == req7.id);

        Contract.Assert(GamblingSite.bets[req7.id].amount == SimplePay.price);
        Contract.Assert(SimplePay.orderID == req7.id);
        Contract.Assert(SimplePay.payee == GamblingSite.AccountID);
        
    }
}
