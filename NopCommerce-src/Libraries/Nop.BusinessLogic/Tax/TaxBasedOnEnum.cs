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
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Products;



namespace NopSolutions.NopCommerce.BusinessLogic.Tax
{
    /// <summary>
    /// Represents the tax based on enumeration
    /// </summary>
    public enum TaxBasedOnEnum : int
    {
        /// <summary>
        /// Billing address
        /// </summary>
        BillingAddress = 1,
        /// <summary>
        /// Shipping address
        /// </summary>
        ShippingAddress = 2,
        /// <summary>
        /// Default address
        /// </summary>
        DefaultAddress = 3,
        /// <summary>
        /// Shipping origin address
        /// </summary>
        ShippingOrigin = 4,
    }
}
