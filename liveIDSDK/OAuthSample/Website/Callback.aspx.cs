namespace OAuthTest
{

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

    public partial class Callback : System.Web.UI.Page
    {
        private const string wlCookie = "wl_auth";
        
        // Update the following values
        public const string clientId = "000000004C108D95";

        // Make sure this is identical to the redirect_uri parameter passed in WL.init() call.

        public const string callback = "http://a.shuochen.com:54432/Website/callback.aspx";

        public const string clientSecret = "zMVS1BpoNHwtdTnSzazSt2JgLd1hxRA1";


        private const string oauthUrl = "https://login.live.com/oauth20_token.srf";

        public static JsonWebToken userInfo;    //the final result of authentication

        protected void Page_Load(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;

            if (!string.IsNullOrEmpty(Request.QueryString[OAuthConstants.AccessToken]))
            {
                // There is a token available already. It should be the token flow. Ignore it.
                return;
            }


            string verifier = Request.QueryString[OAuthConstants.Code];
            OAuthToken token;
            OAuthError error;
            string digest, final_digest;

            if (!string.IsNullOrEmpty(verifier))
            {
                RequestAccessTokenByVerifier(verifier, out token, out error, out digest);
                HandleTokenResponse(context, token, error, digest, out final_digest);
#if (!DSV)
                bool result = DSVHelper.DSV_Check(final_digest);
#endif
                return;
            }

            string refreshToken = ReadRefreshToken();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                RequestAccessTokenByRefreshToken(refreshToken, out token, out error);
                HandleTokenResponse(context, token, error, "", out final_digest);
                return;
            }

            string errorCode = Request.QueryString[OAuthConstants.Error];
            string errorDesc = Request.QueryString[OAuthConstants.ErrorDescription];

            if (!string.IsNullOrEmpty(errorCode))
            {
                error = new OAuthError(errorCode, errorDesc);
                HandleTokenResponse(context, null, error, "", out final_digest);
            }

        }

        public static string ReadRefreshToken()
        {
            // read refresh token of the user identified by the site.
            return null;
        }

        private static void SaveRefreshToken(string refreshToken)
        {
            // save the refresh token and associate it with the user identified by your site credential system.
        }


        public static void RequestAccessTokenByVerifier(string verifier, out OAuthToken token, out OAuthError error, out string digest)
        {
            digest = DSVHelper.digest_RequestAccessTokenByVerifier();

            string content = String.Format("client_id={0}&redirect_uri={1}&client_secret={2}&code={3}&grant_type=authorization_code",
                HttpUtility.UrlEncode(clientId),
                HttpUtility.UrlEncode(callback),
                HttpUtility.UrlEncode(clientSecret),
                HttpUtility.UrlEncode(verifier));

            RequestAccessToken_wrapper(content, verifier, "authorization_code", out token, out error);
        }


        public static void RequestAccessTokenByRefreshToken(string refreshToken, out OAuthToken token, out OAuthError error)
        {
            string content = String.Format("client_id={0}&redirect_uri={1}&client_secret={2}&refresh_token={3}&grant_type=refresh_token",
                HttpUtility.UrlEncode(clientId),
                HttpUtility.UrlEncode(callback),
                HttpUtility.UrlEncode(clientSecret),
                HttpUtility.UrlEncode(refreshToken));

            RequestAccessToken_wrapper(content, refreshToken, "refresh_token", out token, out error);
        }

        private static void RequestAccessToken(string postContent, out OAuthToken token, out OAuthError error)
        {
            token = null;
            error = null;

            HttpWebRequest request = WebRequest.Create(oauthUrl) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            try
            {
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(postContent);
                }

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response != null)
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OAuthToken));
                    token = serializer.ReadObject(response.GetResponseStream()) as OAuthToken;
                    if (token != null)
                    {
                        return;
                    }
                }
            }
            catch (WebException e)
            {
                HttpWebResponse response = e.Response as HttpWebResponse;
                if (response != null)
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OAuthError));
                    error = serializer.ReadObject(response.GetResponseStream()) as OAuthError;
                }
            }
            catch (IOException)
            {
            }

            if (error == null)
            {
                error = new OAuthError("request_failed", "Failed to retrieve user access token.");
            }
        }

        public static void HandleTokenResponse(HttpContext context, OAuthToken token, OAuthError error, string digest, out string new_digest)
        {
            new_digest = DSVHelper.digest_HandleTokenResponse(digest);

            Dictionary<string, string> cookieValues = new Dictionary<string, string>();
            HttpCookie cookie = context.Request.Cookies[wlCookie];
            HttpCookie newCookie = new HttpCookie(wlCookie);
            newCookie.Path = "/";
            newCookie.Domain = context.Request.Headers["Host"];

            if (cookie != null && cookie.Values != null)
            {
                foreach (string key in cookie.Values.AllKeys)
                {
                    newCookie[key] = cookie[key];
                }
            }

            if (token != null)
            {
                userInfo = ReadUserInfoFromAuthToken(token);
                // The userInfo contains identifiable information about the user.
                // You may add some logic here.

                newCookie[OAuthConstants.AccessToken] = HttpUtility.UrlEncode(token.AccessToken);
                newCookie[OAuthConstants.AuthenticationToken] = HttpUtility.UrlEncode(token.AuthenticationToken);
                newCookie[OAuthConstants.Scope] = HttpUtility.UrlPathEncode(token.Scope);
                newCookie[OAuthConstants.ExpiresIn] = HttpUtility.UrlEncode(token.ExpiresIn);

                if (!string.IsNullOrEmpty(token.RefreshToken))
                {
                    SaveRefreshToken(token.RefreshToken);
                }
            }

            if (error != null)
            {
                newCookie[OAuthConstants.Error] = HttpUtility.UrlEncode(error.Code);
                newCookie[OAuthConstants.ErrorDescription] = HttpUtility.UrlPathEncode(error.Description);
            }

            context.Response.Cookies.Add(newCookie);
        }

        private static JsonWebToken ReadUserInfoFromAuthToken(OAuthToken token)
        {
            string authenticationToken = token.AuthenticationToken;

            Dictionary<int, string> keys = new Dictionary<int, string>();

            //shuo:begin
            //the original sample code seems wrong
            // keys.Add(0, clientSecret);
            keys.Add(1, clientSecret);
            //shuo:end

            JsonWebToken jwt = null;
            try
            {
                jwt = getJsonWebToken_wrapper(authenticationToken, keys, token);
                return jwt;
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }



















//******************************************DSV wrapper methods ******************************
    public partial class Callback : System.Web.UI.Page
    {

        private static void RequestAccessToken_wrapper(string content, string arg4, string arg5, out OAuthToken token, out OAuthError error)
        {
#if (!DSV) 
            RequestAccessToken(content, out token, out error);
#else
            if (arg5 == "authorization_code")
            {
                PoirotMain.oauth20_token__srf_arg1 = clientId;
                PoirotMain.oauth20_token__srf_arg2 = callback;
                PoirotMain.oauth20_token__srf_arg3 = clientSecret;
                PoirotMain.oauth20_token__srf_arg4 = arg4;
                PoirotMain.oauth20_token__srf_arg5 = arg5;
            }
            else if (arg5 == "refresh_token")
            {
                PoirotMain.oauth20_token__srf_arg1 = clientId;
                PoirotMain.oauth20_token__srf_arg2 = callback;
                PoirotMain.oauth20_token__srf_arg3 = clientSecret;
                PoirotMain.oauth20_token__srf_arg4 = arg4;
                PoirotMain.oauth20_token__srf_arg5 = arg5;
            }
            token = null; error = null;
#endif
        }

        private static JsonWebToken getJsonWebToken_wrapper(string authenticationToken, Dictionary<int, string> keys, OAuthToken token)
        {
            JsonWebToken jwt = null;
#if (!DSV)
            jwt = new JsonWebToken(authenticationToken, keys);
#else
            jwt = PoirotMain.p.NondetJsonWebToken();
            Contract.Assume(jwt == token.jwt && jwt != null);
#endif
            return jwt;
        }
    }
}