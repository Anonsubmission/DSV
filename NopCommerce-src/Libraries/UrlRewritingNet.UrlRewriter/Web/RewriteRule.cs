/* UrlRewritingNet.UrlRewrite
 * Version 2.0
 * 
 * This Library is Copyright 2006 by Albert Weinert and Thomas Bandt.
 * 
 * http://der-albert.com, http://blog.thomasbandt.de
 * 
 * This Library is provided as is. No warrenty is expressed or implied.
 * 
 * You can use these Library in free and commercial projects without a fee.
 * 
 * No charge should be made for providing these Library to a third party.
 * 
 * It is allowed to modify the source to fit your special needs. If you 
 * made improvements you should make it public available by sending us 
 * your modifications or publish it on your site. If you publish it on 
 * your own site you have to notify us. This is not a commitment that we 
 * include your modifications. 
 * 
 * This Copyright notice must be included in the modified source code.
 * 
 * You are not allowed to build a commercial rewrite engine based on 
 * this code.
 * 
 * Based on http://weblogs.asp.net/fmarguerie/archive/2004/11/18/265719.aspx
 * 
 * For further informations see: http://www.urlrewriting.net/
 */

using System;
using System.Collections.Generic;
using System.Text;
using UrlRewritingNet.Configuration;

namespace UrlRewritingNet.Web
{
    public abstract class RewriteRule
    {
        private RedirectOption redirect = RedirectOption.None;

        public RedirectOption Redirect
        {
            get { return redirect; }
            set { redirect = value; }
        }

        private RewriteOption rewrite = RewriteOption.Application;

        public RewriteOption Rewrite
        {
            get { return rewrite; }
            set { rewrite = value; }

        }
        private string name;

        public string Name
        {
            get { return name; }
            internal set { name = value; }
        }

        private RedirectModeOption redirectMode = RedirectModeOption.Temporary;

        public RedirectModeOption RedirectMode
        {
            get { return redirectMode; }
            set { redirectMode = value; }
        }
        private RewriteUrlParameterOption rewriteUrlParameter = RewriteUrlParameterOption.ExcludeFromClientQueryString;

        public RewriteUrlParameterOption RewriteUrlParameter
        {
            get { return rewriteUrlParameter; }
            set { rewriteUrlParameter = value; }
        }

        private bool ignoreCase = false;

        public bool IgnoreCase
        {
            get { return ignoreCase; }
            set { ignoreCase = value; }
        }

        public abstract bool IsRewrite(string requestUrl);
        public abstract string RewriteUrl(string url);

        public virtual void Initialize(RewriteSettings rewriteSettings)
        {
            if (rewriteSettings == null)
                throw new ArgumentNullException("rewriteSettings");
            this.redirect = rewriteSettings.Redirect;
            this.rewrite = rewriteSettings.Rewrite;
            this.redirectMode = rewriteSettings.RedirectMode;
            this.rewriteUrlParameter = rewriteSettings.RewriteUrlParameter;
            this.ignoreCase = rewriteSettings.IgnoreCase;
            this.Name = rewriteSettings.Name;
        }
    }
}
