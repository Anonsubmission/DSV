
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


        Callback.RequestAccessTokenByVerifier(RequestAccessTokenByVerifier_arg1, out RequestAccessTokenByVerifier_arg2, out RequestAccessTokenByVerifier_arg3, out RequestAccessTokenByVerifier_arg4);//2014-1-10
        oauth20_token__srf_retVal = LiveIDServer.oauth20_token__srf(oauth20_token__srf_arg1, oauth20_token__srf_arg2, oauth20_token__srf_arg3, oauth20_token__srf_arg4, oauth20_token__srf_arg5);//2014-1-10
            if (oauth20_token__srf_retVal == null) return;

            HandleTokenResponse_arg2 = oauth20_token__srf_retVal;

        Callback.HandleTokenResponse(HandleTokenResponse_arg1, HandleTokenResponse_arg2, HandleTokenResponse_arg3, "", out HandleTokenResponse_arg4);//2014-1-10
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].clientId == Callback.clientId);
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].callback == Callback.callback);
            Contract.Assert(LiveIDServer.IdpAuth[Auth_req.MSPAuth][Auth_req.clientId].token.jwt.Claims.UserId == Callback.userInfo.Claims.UserId);
            
        }
    }
