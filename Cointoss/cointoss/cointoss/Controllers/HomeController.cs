using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Mvc;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.Amazon;
using System.Globalization;
using System.Net;
using System.Collections.Specialized;
using cointoss.Models;

namespace cointoss.Controllers
{
    public class HomeController : Controller
    {
        static string site_root = "http://localhost:2631";
        static string recipient_email = "cs0317b@gmail.com";
        private TokenDBContext tokenDb = new TokenDBContext();
        private BetDbContext betDb = new BetDbContext();
        string key_root = "C:\\CCP";

        string _sig;

        //cointosser helper function
        public string FetchVerifyTokenResult(string uri)
        {
            var client = new WebClient();
            return client.DownloadString(uri);
        }

        public ActionResult CoinTosser(string token, string price, string id)
        {
            var client = new WebClient();
            //get info from nopcommerce submission form
            string dummyResult = "head";

            //first, check if signature is valid
            NameValueCollection parameters = new NameValueCollection(Request.QueryString);
            ASCIIEncoding ByteConverter = new ASCIIEncoding();
            string sig = parameters["signature"];
            parameters.Remove("signature");
            string returnURL = Request.Url.Scheme +"://"+Request.Url.Host+":"+ Request.Url.Port + "/Home/CoinTosser?" + ToQueryString(parameters);
            if (!CryptoHelper.VerifySignedHash(returnURL, sig))
            {
                ViewBag.error = " Error: Signature verification failed!";
            }
            else
            {
                ViewBag.error = "";
            }

            //first, calculate pathdigest
            NameValueCollection redirParams = new NameValueCollection();
            string old_hash = parameters["path_digest"];
            string new_hash = CCPHelper.code_to_hash(CCPHelper.VerifyTokenReq_code);
            string path_digest = "CoinTosser[" + new_hash + "((" + old_hash + "))]";
            redirParams["path_digest"] = path_digest;
            redirParams["token"] = token;
            redirParams["result"] = dummyResult;

            /*
             * site_root +"/Home/VerifyToken?token=" +token + "&result=" + result
             */
            string redirectUri = Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port +
                                 "/Home/VerifyToken?" + ToQueryString(redirParams);

            string OAuthResponse = FetchVerifyTokenResult(redirectUri);

            string[] split = OAuthResponse.Split('#');

            string effectiveResult = split[0];
            old_hash = split[1];

            if (effectiveResult == dummyResult)
            {
                new_hash = CCPHelper.code_to_hash(CCPHelper.GamblingReq_code);
                path_digest = "CoinTosser[" + new_hash + "((" + old_hash + "))]";

                //create signature
                NameValueCollection redirQueryParams = new NameValueCollection();
                redirQueryParams["id"] = id;
                redirQueryParams["result"] = effectiveResult;
                redirQueryParams["path_digest"] = path_digest;
                string return_uri = site_root + "/Home/Index?" + ToQueryString(redirQueryParams);
                string signature = CryptoHelper.HashAndSignBytes(return_uri);

                ViewBag.result = effectiveResult;
                //make a remote post back to gaming site
                RemotePost post = new RemotePost();
                post.FormName = "Cointoss";
                post.Url = site_root + "/Home/Index";
                post.Method = "POST";
                post.Add("result", effectiveResult);
                post.Add("id", id);
                post.Add("path_digest", path_digest);
                post.Add("signature", signature);
                post.Post();
            }

            return View();
        }

        public string VerifyToken()
        {
            NameValueCollection parameters = new NameValueCollection(Request.QueryString);
            string strToken = parameters["token"];
            var token = tokenDb.Tokens
                            .Where(t => t.OAuthToken == strToken)
                            .FirstOrDefault();

            if (token != null && token.EffectiveResult == "untossed")
            {
                token.EffectiveResult = parameters["result"];
                tokenDb.SaveChanges();
                
                string old_hash = parameters["path_digest"];
                string new_hash = CCPHelper.code_to_hash(CCPHelper.VerifyToken_code);
                string path_digest = "OAuth[" + new_hash + "((" + old_hash + "))]";

                string result = token.EffectiveResult + "#" + path_digest;

                return result;
            }
            return "";
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

            //first, check if signature is valid
            ASCIIEncoding ByteConverter = new ASCIIEncoding();
            string sig = parameters["signature"];

            parameters.Remove("signature");
            string returnURL = Request.Url.Scheme +"://"+Request.Url.Host+":"+ Request.Url.Port + "/Home/OAuth?" + ToQueryString(parameters);
            if (!CryptoHelper.VerifySignedHash(returnURL,sig))
            {
                ViewBag.error = "Error! signature doesn't match";
                return View();
            }
            else ViewBag.error = "";

            if (parameters["status"] == "PS" && parameters["recipientEmail"] == recipient_email)
            {
                //payment is successful, we can issue OAuth token
                Token token = new Token();
                token.OAuthToken = "TestOAuthToken";
                token.EffectiveResult = "untossed";
                token.Cost = parameters["transactionAmount"];
                token.BetID = parameters["referenceId"];
                //pass info to front end

                NameValueCollection redirParams = new NameValueCollection();


                //first, calculate pathdigest
                string old_hash = parameters["path_digest"];
                string new_hash = CCPHelper.code_to_hash(CCPHelper.OAuth_code);
                string path_digest = "OAuth[[" + new_hash + "(" + old_hash + ")]]";
                redirParams["path_digest"] = path_digest;

                //assemble HTTP params
                redirParams["token"] = token.OAuthToken;
                redirParams["price"] = token.Cost;
                redirParams["id"] = token.BetID;
              
                //'/Home/CoinTosser?token='+'@ViewBag.token'+'&price='+'@ViewBag.price' + '&id=' + '@ViewBag.betID'
                string redirectUri = Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port +
                                     "/Home/CoinTosser?" + ToQueryString(redirParams);

                redirectUri += "&signature=" + HttpUtility.UrlEncode(CryptoHelper.HashAndSignBytes(redirectUri));

                ViewBag.redirUri = redirectUri;

                tokenDb.Tokens.Add(token);
                tokenDb.SaveChanges();
            }

            return View();
        }

        public ActionResult Index(string result, string id, string path_digest, string signature)
        {
            

            //this if statement will trigger on the last step of the protocol
            if (result == "head" || result == "tail")
            {
                //first, verify that signature is correct
                //create signature
                NameValueCollection redirQueryParams = new NameValueCollection();
                redirQueryParams["id"] = id;
                redirQueryParams["result"] = result;
                redirQueryParams["path_digest"] = path_digest;
                string return_uri = site_root + "/Home/Index?" + ToQueryString(redirQueryParams);

                if (!CryptoHelper.VerifySignedHash(return_uri, signature))
                {
                    ViewBag.error = "Error: Signature wrong!";
                }
                else ViewBag.error = "";

                string new_hash = CCPHelper.code_to_hash(CCPHelper.GamblingSite_code);
                string new_path_digest = "GamblingSite[[" + new_hash + "(" + path_digest + ")]]";

                CCPHelper.generate_cs_file_from_symval(new_path_digest);
                if (!CCPHelper.checkLogicProperty())
                {
                    ViewBag.error = "Error: Boogie Verification failed";
                }
                else
                {

                    int i = Convert.ToInt32(id);
                    Bet bet = betDb.Bets.Where(b => b.ID == i).FirstOrDefault();


                    ViewBag.result = result;
                    ViewBag.bet = bet.amount;
                    ViewBag.guess = bet.guess;
                }
            }


            return View();
        }

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            return string.Join("&", array);
        }

       
        public void SimplePayResp()
        {

            string returnUrl = site_root + "/Home/OAuth";

            NameValueCollection parameters = new NameValueCollection(Request.QueryString);

            string old_hash = parameters["path_digest"];
            string new_hash = CCPHelper.code_to_hash(CCPHelper.SimplePayResp_code);
            string path_digest = "CaaS[[" + new_hash + "(" + old_hash + ")]]";
            parameters["path_digest"] = path_digest;

            parameters.Remove("signature");
            
            //Note that localhost:8243 and AmazonSimplePayReturn.aspx are hardcoded right now, idealy we want them to be dynamically inserted
            Response.StatusCode = 302;
            Response.Status = "302 Moved Temporarily";
            Response.RedirectLocation = returnUrl + "?" + ToQueryString(parameters);

            _sig = CryptoHelper.HashAndSignBytes(Response.RedirectLocation);
            Response.RedirectLocation += "&signature=" + _sig;


            Response.End();
            
        }

        public void SimplePayReq(int amount, string flip)
        {
            var bets = betDb.Bets;
            foreach (var b in bets)
            {
                betDb.Bets.Remove(b);
            }

            betDb.SaveChanges();

            Bet bet = new Bet();
            bet.guess = flip;
            bet.amount = amount;
            betDb.Bets.Add(bet);
            betDb.SaveChanges();

            string codeHash = CCPHelper.code_to_hash(CCPHelper.SimplePayReq_code);

            //1. redirect to simplepay
            RemotePost post = new RemotePost();
            post.FormName = "SimplePay";
            post.Url = "https://authorize.payments-sandbox.amazon.com/pba/paypipeline";
            post.Method = "POST";

            post.Add("immediateReturn", "1");
            post.Add("signatureVersion", "2");
            post.Add("signatureMethod", "HmacSHA256");
            post.Add("accessKey", "AKIAJB4XJRGX6XRRVIDA");
            post.Add("amount", String.Format(CultureInfo.InvariantCulture, "USD {0:0.00}", amount));
            post.Add("description", flip);
            post.Add("amazonPaymentsAccountId", "IGFCUTPWGXVM311K1E6QTXIQ1RPEIUG5PTIMUZ");
            post.Add("returnUrl", site_root + "/Home/SimplePayResp?path_digest=GamblingSite[["+codeHash+"()]]");
            post.Add("processImmediate", "1");
            post.Add("referenceId", Convert.ToString(bet.ID));
            //the entire msg is signed using the pre-decided simplepay secret key
            post.Add("signature", AmazonHelper.SignParameters(post.Params,
                        "WfZ3JnrY8mpJ8DZ7VlL07+RYtWznX3PWHNV8Zj5M", //simplePay secret key
                        post.Method,
                        "authorize.payments-sandbox.amazon.com",
                        "/pba/paypipeline"));
            post.Post();   

        }
    }
}
