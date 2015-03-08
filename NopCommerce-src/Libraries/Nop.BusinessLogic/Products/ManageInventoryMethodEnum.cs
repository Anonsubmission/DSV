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

namespace NopSolutions.NopCommerce.BusinessLogic.Products
{
    /// <summary>
    /// Represents a method of inventory manage,ent
    /// </summary>
    public enum ManageInventoryMethodEnum : int
    {
        /// <summary>
        /// Don't track inventory for product variant
        /// </summary>
        DontManageStock = 0,
        /// <summary>
        /// Track inventory for product variant
        /// </summary>
        ManageStock = 1,
        /// <summary>
        /// Track inventory for product variant by product attributes
        /// </summary>
        ManageStockByAttributes = 2,
    }
}
