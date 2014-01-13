using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.IO;
using OAuthTest;

public class DSVHelper
    {
        

        public static string predicate = @"
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].clientId == Callback.clientId);
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].callback == Callback.callback);
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].token.jwt.Claims.ClientIdentifier == Callback.userInfo.Claims.ClientIdentifier);
";
        public static string[] whitelist = new string[2] { "RP", "MSFT" };
    
    
        static Dictionary<string, string> codeHashMap = new Dictionary<string, string>();

        static string dehash_server_host = "http://dehash.com:81/";   //anonymized for submission
        static string upload_path = "verification/upload.php";
        static string dehash_path = "verification/dehash.php";

        static string root = "C:\\CCP\\teamproject\\liveid\\OAuthSample\\MT-Program";

        public static string request_code = @"
        Callback.RequestAccessTokenByVerifier(RequestAccessTokenByVerifier_arg1, out RequestAccessTokenByVerifier_arg2, out RequestAccessTokenByVerifier_arg3, out RequestAccessTokenByVerifier_arg4);";

        public static string live_code = @"
        oauth20_token__srf_retVal = LiveIDServer.oauth20_token__srf(oauth20_token__srf_arg1, oauth20_token__srf_arg2, oauth20_token__srf_arg3, oauth20_token__srf_arg4, oauth20_token__srf_arg5);";

        public static string return_code = @"
        Callback.HandleTokenResponse(HandleTokenResponse_arg1, HandleTokenResponse_arg2, HandleTokenResponse_arg3, """", out HandleTokenResponse_arg4);";

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

        //this function converts a piece of code to a hash
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



        public class transaction
        {
            public string party;
            public string function;
            public bool isProtected = true;
        }



        //this function parses the symval string and returns an array of functions called in reverse-chronological order
        // A[HASH1(B[[HASH2()]])]
        protected static Stack<transaction> parse_digest(string symval)
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
                        isSigned = (step == 1) ? false : true; //A[[hash(signed)]], A[hash((insigned))]
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

        protected static string assemble_code(string path_digest)
        {

            Stack<transaction> callstack = parse_digest(path_digest);
            string code = "";

            
            //assemble main
            code += @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using OAuthTest;
using System.Web;

interface Picker
{
    int NondetInt();
    string NondetString();
    Boolean NondetBool();
    OAuthToken NondetOAuthToken();
    JsonWebToken NondetJsonWebToken();
    Dictionary<string, string> NondetDictionaryStringString();
    Dictionary<string, Authorization_Code_Entry> NonetDictionaryStringCodeEntry();
    Dictionary<string, Dictionary<string, Authorization_Code_Entry>> NondetDictionaryStringStringCodeEntry();
    Dictionary<string, App_Registration> NondetDictionaryStringAppRegistration();
}
class PoirotMain
    {
        static public Picker p;

        static public string RequestAccessTokenByVerifier_arg1 = p.NondetString();
        static public OAuthToken RequestAccessTokenByVerifier_arg2;
        static public OAuthError RequestAccessTokenByVerifier_arg3;
        static public string RequestAccessTokenByVerifier_arg4 = p.NondetString();

        static public string oauth20_token__srf_arg1= p.NondetString();
        static public string oauth20_token__srf_arg2 = p.NondetString();
        static public string oauth20_token__srf_arg3 = p.NondetString();
        static public string oauth20_token__srf_arg4 = p.NondetString();
        static public string oauth20_token__srf_arg5 = p.NondetString();
        static public OAuthToken oauth20_token__srf_retVal;

        static public HttpContext HandleTokenResponse_arg1 = HttpContext.Current;
        static public OAuthToken HandleTokenResponse_arg2;
        static public OAuthError HandleTokenResponse_arg3;
        static public string HandleTokenResponse_arg4;
        static public JsonWebToken final_result=p.NondetJsonWebToken();

        static void Main()
        {

";

            /*
            foreach (transaction trans in callstack)
            {
                if (trans.isProtected) code += hash_to_code(trans.function);
            }*/

            //First request for access token
            if (callstack.Count > 0)
            {
                transaction token_req = callstack.Pop();
                if (token_req.isProtected)
                {
                    code += hash_to_code(token_req.function);
                }
            }

            //Live code
            if (callstack.Count > 0)
            {
                transaction token_srf = callstack.Pop();
                if (token_srf.isProtected)
                {
                    code += hash_to_code(token_srf.function);
                }
            }

            code += @"
            if (oauth20_token__srf_retVal == null) return;

            HandleTokenResponse_arg2 = oauth20_token__srf_retVal;
";

            //Live code
            if (callstack.Count > 0)
            {
                transaction token_response = callstack.Pop();
                if (token_response.isProtected)
                {
                    code += hash_to_code(token_response.function);
                }
            }
            code += predicate;
            code += @"            
        }
    }
";

            return code;

        }

        public static bool DSV_Check(string digest)
        {
            generate_cs_file_from_symval(digest);
            return checkLogicProperty();
        }

        public static void generate_cs_file_from_symval(string path_digest)
        {

            TimeSpan t1 = (DateTime.UtcNow - new DateTime(1970, 1, 1));


            string content = assemble_code(path_digest);


            TimeSpan t2 = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            int num = (int)(t2.TotalMilliseconds - t1.TotalMilliseconds);


            using (StreamWriter outfile = new StreamWriter(root + "\\DSV\\Program.cs"))
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


    //**************************************digest computations*********************************************************************
        public static string digest_RequestAccessTokenByVerifier() {
            string digest=""; 
#if (!DSV)
            digest = "RP[" + DSVHelper.code_to_hash(DSVHelper.request_code) + "(())" + "]";
            //since we don't have control of LiveID's server, we have to add its digest here
            digest = "MSFT[" + DSVHelper.code_to_hash(DSVHelper.live_code) + "((" + digest + "))" + "]";
#endif
            return digest;
        }

        public static string digest_HandleTokenResponse(string digest_in)
        {
            string digest = "";
#if (!DSV)
            digest = "RP[" + DSVHelper.code_to_hash(DSVHelper.return_code) + "((" + digest_in + "))]";
#endif
            return digest;
        }
    }