
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using System.Diagnostics.Contracts;
    using OAuthTest;
    
    public class App_Registration {
        public string callback;
        public string clientSecret;
        public App_Registration(string callback0, string clientSecret0)
        {
            this.callback = callback0; this.clientSecret = clientSecret0;
        }
    }
    
    public class Auth_req
    {
        static public string MSPAuth=PoirotMain.p.NondetString();
        static public string clientId = PoirotMain.p.NondetString();
        static public string callback = PoirotMain.p.NondetString();
        static public string grant_type = PoirotMain.p.NondetString();
        static public string scope = PoirotMain.p.NondetString();
    }

    public class Authorization_Code_Entry
    {
        public string code;
        public string MSPAuth;
        public string clientId;
        public string callback;
        public OAuthToken token;
        public void EnsureInvariant()
        {
            Contract.Assume(token.jwt.Claims.AppId==clientId
                && token.jwt.Claims.ClientIdentifier == LiveIDServer.MSPAuth_to_UserID[MSPAuth]
                && token.jwt.Claims.Audience == callback
                && LiveIDServer.app_registration[clientId].callback == callback
                );
        }
     }

    public class LiveIDServer 
    {
        static public Dictionary<string, App_Registration> app_registration = PoirotMain.p.NondetDictionaryStringAppRegistration();
        static public Dictionary<string, string> MSPAuth_to_UserID = PoirotMain.p.NondetDictionaryStringString();
        static public Dictionary<string, Authorization_Code_Entry> code_to_codeEntry = PoirotMain.p.NonetDictionaryStringCodeEntry();
        static public Dictionary<string, Dictionary<string, Authorization_Code_Entry>> IdpAuth = PoirotMain.p.NondetDictionaryStringStringCodeEntry();
        static LiveIDServer()
        {
            Contract.Assume(app_registration[Callback.clientId].callback == Callback.callback
                && app_registration[Callback.clientId].clientSecret == Callback.clientSecret);       
        }

       static public OAuthToken oauth20_token__srf(string clientId, string callback, string clientSecret, string varString, string grant_type) 
        {
            if (varString==null) 
                return null;
            if (PoirotMain.p.NondetBool())
                return null;

            if (grant_type == "authorization_code")
            {
                Authorization_Code_Entry entry = code_to_codeEntry[varString];
                if (entry == null)
                    return null;
                if (entry.clientId != clientId             
                    || LiveIDServer.app_registration[clientId].clientSecret != clientSecret
                    //   interestingly, we found that LiveID server doesn't check "entry.callback!=callback"
                    )
                    return null;
                
                Contract.Assume(entry.clientId == Auth_req.clientId
                         && entry.callback == Auth_req.callback);
                Contract.Assume(entry.MSPAuth == Auth_req.MSPAuth);

                Contract.Assume(IdpAuth[entry.MSPAuth][entry.clientId] == entry);
                entry.EnsureInvariant();
                return entry.token;
            }
            else if (grant_type == "refresh_token")
            {
                Contract.Assert(false);
            }
            return null;
        }
    }
