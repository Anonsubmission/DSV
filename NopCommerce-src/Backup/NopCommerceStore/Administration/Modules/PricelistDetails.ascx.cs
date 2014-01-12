//------------------------------------------------------------------------------
// The contents of this file are subject to the nopCommerce Public License Version 1.0 ("License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  http://www.nopCommerce.com/License.aspx. 
// 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
// See the License for the specific language governing rights and limitations under the License.
// 
// The Original Code is nopCommerce.
// The Initial Developer of the Original Code is NopSolutions.
// All Rights Reserved.
// 
// Contributor(s): _______. 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NopSolutions.NopCommerce.BusinessLogic.ExportImport;
using NopSolutions.NopCommerce.BusinessLogic.Products;
using NopSolutions.NopCommerce.BusinessLogic.Profile;
using NopSolutions.NopCommerce.BusinessLogic.Promo.Affiliates;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Web.Administration.Modules;

namespace NopSolutions.NopCommerce.Web.Administration.Modules
{
    public partial class PricelistDetailsControl : BaseNopAdministrationUserControl
    {
        protected void DeleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                ProductManager.DeletePricelist(this.PricelistId);
                Response.Redirect(string.Format("Pricelist.aspx"));
            }
            catch (Exception exc)
            {
                ProcessException(exc);
            }
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    Pricelist pricelist = ctrlPricelistInfo.SaveInfo();
                    if (pricelist != null)
                    {
                        Response.Redirect("PricelistDetails.aspx?PricelistID=" + pricelist.PricelistId.ToString());
                    }
                    else
                    {
                        Response.Redirect("Pricelist.aspx");
                    }
                }
                catch (Exception exc)
                {
                    ProcessException(exc);
                }
            }
        }

        public int PricelistId
        {
            get
            {
                return CommonHelper.QueryStringInt("PricelistId");
            }
        }
    }
}