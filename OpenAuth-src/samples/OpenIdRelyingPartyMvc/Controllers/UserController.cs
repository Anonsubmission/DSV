namespace OpenIdRelyingPartyMvc.Controllers {
	using System;
    using System.Collections;
	using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Collections.ObjectModel;
	using System.Linq;
	using System.Threading.Tasks;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
	using System.Web;
	using System.Web.Mvc;
	using System.Web.Security;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId;
    using DotNetOpenAuth.OpenId.Extensions;
    using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
    using DotNetOpenAuth.OpenId.Messages;
	using DotNetOpenAuth.OpenId.RelyingParty;
    using DotNetOpenAuth.OpenId.RelyingParty.Extensions;
    using DotNetOpenAuth.Test.OpenId.Extensions;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Diagnostics;

    public class transaction
    {
        public string party;
        public string function;
        public string coverage;
        public bool isEncrypted;
    }

	public class UserController : Controller {
		private static OpenIdRelyingParty openid = new OpenIdRelyingParty();

		public ActionResult Index() {
			if (!User.Identity.IsAuthenticated) {
				Response.Redirect("~/User/Login?ReturnUrl=Index");
			}

			return View("Index");
		}

		public ActionResult Logout() {
			FormsAuthentication.SignOut();
			return Redirect("~/Home");
		}

		public ActionResult Login() {
			// Stage 1: display login form to user
			return View("Login");
		}

        //ERIC'S CODE - begin
        protected internal static async Task<IEnumerable<KeyValuePair<string, string>>> ParseUrlEncodedFormContentAsync(HttpRequestMessage request)
        {
            if (request.Content != null && request.Content.Headers.ContentType != null
                && request.Content.Headers.ContentType.MediaType.Equals("application/x-www-form-urlencoded"))
            {
                return HttpUtility.ParseQueryString(await request.Content.ReadAsStringAsync()).AsKeyValuePairs();
            }

            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        //this function parses the symval string and returns an array of functions called in reverse-chronological order
        // A[HASH1(B[[HASH2()]])]
        protected static Stack parse_symval(string symval)
        {
            Stack callStack = new Stack();

            int step = 0, start = 0, end = symval.Length-1;
            int c = 0, s = 0, s_end=0;

            while (start < end)
            {
                step = 0;
                c = (symval.IndexOf('(', start) == -1) ? int.MaxValue : symval.IndexOf('(', start);
                s = (symval.IndexOf('[', start) == -1) ? int.MaxValue : symval.IndexOf('[', start);

                if (c < s)
                { //if ( is before [
                    if (symval[c + 1] == '(') step = 2; // double '('
                    else step = 1;
                    start += step;
                    end -= step;
                }
                else if (s < c)
                { //if [ is before (
                    transaction cur = new transaction();
                    if (symval[s + 1] == '[') step = 2; // double '['
                    else step = 1;
                    if (c < int.MaxValue)
                    { // pattern: A[HASH(
                        cur.isEncrypted = (step == 1) ? false : true; //[[encrypted]] [not encrypted]
                        cur.party = symval.Substring(start, s - start); //A[[HASH()]], a is party
                        cur.function = symval.Substring(s + step, c - s - step); //A[[HASH()]] HASH is function

                        ////if one of the parties involved in the transaction is not known
                        //if (!Array.Exists(whitelist, element => element == cur.party))
                        //{
                        //    break;
                        //}

                        //get the coverage field if the msg was encrypted
                        s_end=0;
                        if(step==2){
                            s_end = (symval.LastIndexOf(']', end - 1) == -1) ? 0 : symval.LastIndexOf(']', end - 1);
                            cur.coverage = (s_end == 0) ? "" : symval.Substring(s_end + 1, end - s_end - 1);
                        }
                        callStack.Push(cur);
                    }
                    start = c;
                    end = (step == 2) ? s_end-1 : end-step; //Case 1:A[HASH1()], Case 2: A[[HASH1()]COVERAGE]
                }
                else
                { // the only case for this would be an invalid string
                    break;
                }
            }

            return callStack;
        }

        static string SourceCode_Global= @"
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using System.Net.Http;
using System.Web;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Messages;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OpenId.ChannelElements;
using DotNetOpenAuth.OpenId.Extensions;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId.RelyingParty.Extensions;


public class GlobalState
{
    public static MessageProtections appliedProtection;
    public static PositiveAssertionResponse is_positive_assertion;
    public static ClaimsResponse actualExt, claimedExt;
    public static IdentifierDiscoveryResult claimedEndPoint, actualEndPoint;

}

";

        //this function generates a C# model from a symval string
        protected static string assemble_code(string symval, Dictionary<string, string> fields)
        {

            string code = SourceCode_Global;

            Stack callStack = parse_symval(symval);
            transaction op_msg, rp_msg;

            op_msg = (transaction)callStack.Pop();
            rp_msg = (transaction)callStack.Pop();

            code += "@@@@OP_PLACEHOLDER@@@@"; //we delay code insertion after verifying the signature of the symval is valid

            code += PositiveAssertionResponse.hash_to_code(rp_msg.function);

            code += @"

interface Picker
{
    int NondetInt();
    Channel1 NondetChannel1();
    Uri NondetUri();
    OpenIdRelyingParty1 NondetOpenIdRelyingParty1();
    string NondetString();
    HttpRequest NondetHttpRequest();
    ClaimsResponse NondetClaimsResponse();
    IdentifierDiscoveryResult NondetIdentifierDiscoveryResult();
    Identifier NondetIdentifier();
}


enum Identity
{
    Email,
    ClaimedId
};

class PoirotMain
{
    static PositiveAssertionResponse1 Auth_resp = new PositiveAssertionResponse1();
    static PositiveAssertionResponse1 SignIn_req = new PositiveAssertionResponse1();
    static PositiveAssertionResponse1 pas_nondet = new PositiveAssertionResponse1();
    static HttpResponseMessage result;
    static Channel1 c;
    static OpenIdRelyingParty1 rp = new OpenIdRelyingParty1();
    static Picker p;
    static IAuthenticationResponse result1;
    static string auth_req_sessionID, auth_req_realm;

    //This variable should be set by the developer to determine which identity we use
    static Identity identity;


    static void init()
    {
        c = p.NondetChannel1();
        Auth_resp.SessionID= auth_req_sessionID; 
        Contract.Assume (Auth_resp.ReturnTo.Authority== auth_req_realm);  //This is an assignment, but a property cannot be assigned.
        Auth_resp.Recipient = p.NondetUri(); Auth_resp.ReturnTo = p.NondetUri(); Auth_resp.ClaimedIdentifier = p.NondetIdentifier();
        rp = p.NondetOpenIdRelyingParty1();
        SignIn_req.Recipient = p.NondetUri(); SignIn_req.ReturnTo = p.NondetUri(); SignIn_req.ClaimedIdentifier = p.NondetIdentifier();
        pas_nondet.Recipient = p.NondetUri(); pas_nondet.ReturnTo = p.NondetUri(); pas_nondet.ClaimedIdentifier = p.NondetIdentifier();
        

        GlobalState.actualExt = p.NondetClaimsResponse();
        GlobalState.claimedExt = p.NondetClaimsResponse();

        GlobalState.actualEndPoint = p.NondetIdentifierDiscoveryResult();
        GlobalState.claimedEndPoint = p.NondetIdentifierDiscoveryResult();

        //Assume we are using email to authenticate
        identity = Identity.Email;
    }

    static PositiveAssertionResponse1 Get_ID_Assertion(string sessionID, string realm)
    {
        if (sessionID == Auth_resp.SessionID && realm == Auth_resp.ReturnTo.Authority)
            return Auth_resp;
        else
            return pas_nondet;
    }
     
    static void Main()
    {
  
        init();

        //initialization
        GlobalState.is_positive_assertion = new PositiveAssertionResponse();

        result = c.PrepareResponseAsync_CCP(Auth_resp);
        //Check for extensions
        ClaimsResponse sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg((IAuthenticationResponse)result, true);
        GlobalState.actualExt = sreg;
";

            //calculate what to havoc
            bool symvalSigned = false;
            string[] key_arr= op_msg.coverage.Split(',');
            if (op_msg.coverage != null)
            {
                //findout where the signed fields are and havoc the rest
                //coverage example: "claimed_id,identity,symval,assoc_handle,op_endpoint,response_nonce,return_to,ns.sreg,sreg.email"
                foreach(string key in key_arr){
                    //each key is a deterministic field
                    if (key == "symval") symvalSigned = true;
                    else if (key == "return_to")
                    {
        //                public string _ReturnTo_Authority;
        //public string _ReturnTo_Scheme;
        //public string _ReturnTo_AbsolutePath;
                        code += @"
        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(SignIn_req.ReturnTo == Auth_resp.ReturnTo);
";
                    }
                    else if (key == "claimed_id")
                    {
                        //                public string _ReturnTo_Authority;
                        //public string _ReturnTo_Scheme;
                        //public string _ReturnTo_AbsolutePath;
                        code += @"
        //signature coverage    -- returnTo and ClaimedIdentifier are protected by the signature
        Contract.Assume(Auth_resp.ClaimedIdentifier == SignIn_req.ClaimedIdentifier);
";
                    }
                }
            }

            //string a = "";
            string a = PositiveAssertionResponse.hash_to_code(op_msg.function);

            //if symval is signed, we add op's code to our proof
            
            code = (symvalSigned) ? Regex.Replace(code, "@@@@OP_PLACEHOLDER@@@@", PositiveAssertionResponse.hash_to_code(op_msg.function)) : "";

            code += @"
        CancellationToken x = default(CancellationToken);
        result1 = rp.GetResponseAsync_ccp(SignIn_req, x);
        sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(result1, true);
        GlobalState.claimedExt = sreg;";


            //check if extensions are signed
            int pos = Array.IndexOf(key_arr, "ns.sreg");
            if (pos > -1){
                code += @"
        //signature coverage
        if (identity == Identity.Email)
            Contract.Assume(GlobalState.actualExt.Email!=null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);";
            }

            

            code += @"
        //RP check: Does the final returnTo field match our origin?
        //This is supplied by the developer of the convincee
        Contract.Assert(Auth_resp.ReturnTo == HttpContext.Current.Request.Url);

        //check for extension
        if (identity == Identity.Email)
            Contract.Assert(GlobalState.actualExt.Email!=null && GlobalState.claimedExt.Email == GlobalState.actualExt.Email);
        else if(identity == Identity.ClaimedId)
            Contract.Assert(Auth_resp.ClaimedIdentifier == SignIn_req.ClaimedIdentifier);

        //RP check: verify claimed id resolves to the correct endpoint
        Contract.Assert(GlobalState.is_positive_assertion == null || GlobalState.claimedEndPoint == GlobalState.actualEndPoint);

        //shuo's assertion
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).ClaimedIdentifier == SignIn_req.ClaimedIdentifier);
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).ReturnTo == HttpContext.Current.Request.Url);
        Contract.Assert(Get_ID_Assertion(auth_req_sessionID, auth_req_realm).Recipient.Authority == HttpContext.Current.Request.Url.Authority);    
    }
}";

            return code;
        }


        static string root = "C:\\CCP\\teamproject\\OpenAuth\\OpenAuth";

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

        public static void generate_cs_file_from_symval(string symval, Dictionary<string, string> fields)
        {
            TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            
            string content = assemble_code(symval, fields);

            TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);

            using (StreamWriter outfile = new StreamWriter(root + "\\Program.cs"))
            {
                outfile.Write(content);
            }

        }

		[ValidateInput(false)]
		public async Task<ActionResult> Authenticate(string returnUrl) {

            HttpRequestMessage req = this.Request.AsHttpRequestMessage();
            Dictionary<string, string> fields = new Dictionary<string, string>();
            fields.AddRange(await ParseUrlEncodedFormContentAsync(req));

            if (fields.Count == 0 && req.Method.Method != "POST")
            { // OpenID 2.0 section 4.1.2
                fields.AddRange(HttpUtility.ParseQueryString(req.RequestUri.Query).AsKeyValuePairs());
            }


            string mode;
            if (fields.TryGetValue("openid.mode", out mode))
            {
                string symVal="";
                fields.TryGetValue("openid.symval",out symVal);

                //first, we add RP's code onto our symval 
                string hash_rp = PositiveAssertionResponse.code_to_hash(PositiveAuthenticationResponse.SourceCode_RP);
                //((AuthenticationRequest)request).ProviderEndpoint.Authority + "[[" + PositiveAssertionResponse.hashvalue_op + "()]";
                symVal = this.Request.Url.Authority + "[[" + hash_rp + "("+symVal+")]]";

                

                generate_cs_file_from_symval(symVal, fields);

                TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));


                int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);

                HttpRequestMessage request = this.Request.AsHttpRequestMessage();

                MessageReceivingEndpoint recipient;
                recipient = request.GetRecipient();

                IProtocolMessage message = openid.Channel.MessageFactory.GetNewRequestMessage(recipient, fields);

                // If there was no data, or we couldn't recognize it as a message, abort.
                if (message == null)
                {
                    return null;
                }

                // We have a message!  Assemble it.
                var messageAccessor = openid.Channel.MessageDescriptions.GetAccessor(message);
                messageAccessor.Deserialize(fields);

                //IDirectedProtocolMessage message = await openid.Channel.ReadFromRequestAsync_ccp(fields, request, this.Response.ClientDisconnectedToken);
                
                //only the final response will be here
                var response_ccp = await openid.GetResponseAsync_ccp(message, this.Response.ClientDisconnectedToken);
                //var response_ccp = await openid.GetResponseAsync(req, this.Response.ClientDisconnectedToken);

                // Stage 3: OpenID Provider sending assertion response
                if (!checkLogicProperty())
                {
                    return new EmptyResult();
                }
                switch (response_ccp.Status)
                {
                    case AuthenticationStatus.Authenticated:
                        Session["FriendlyIdentifier"] = response_ccp.FriendlyIdentifierForDisplay;
                        var cookie = FormsAuthentication.GetAuthCookie(response_ccp.ClaimedIdentifier, false);
                        Response.SetCookie(cookie);
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    case AuthenticationStatus.Canceled:
                        ViewData["Message"] = "Canceled at provider";
                        return View("Login");
                    case AuthenticationStatus.Failed:
                        ViewData["Message"] = response_ccp.Exception.Message;
                        return View("Login");
                }

                return new EmptyResult();

            } 
            else
            {
                var response = await openid.GetResponseAsync(this.Request, this.Response.ClientDisconnectedToken);
                if (response == null)
                {
                    // Stage 2: user submitting Identifier
                    Identifier id;
                    if (Identifier.TryParse(Request.Form["openid_identifier"], out id))
                    {
                        try
                        {
                            var request = await openid.CreateRequestAsync(Request.Form["openid_identifier"]);

                            //Eric - add extension
                            var sregRequest = new ClaimsRequest();
                            sregRequest.Email = DemandLevel.Require;
                            request.AddExtension(sregRequest);

                            var redirectingResponse = await request.GetRedirectingResponseAsync(this.Response.ClientDisconnectedToken);
                            // this code is handled by HttpResponseMessageActionResult :: ExecuteResult(ControllerContext context)
                            return redirectingResponse.AsActionResult();
                        }
                        catch (ProtocolException ex)
                        {
                            ViewData["Message"] = ex.Message;
                            return View("Login");
                        }
                    }
                    else
                    {
                        ViewData["Message"] = "Invalid identifier";
                        return View("Login");
                    }
                }
                return new EmptyResult();
            }
            //ERIC'S CODE - end
		}
	}
}
