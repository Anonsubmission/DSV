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
    /// Represents a related product
    /// </summary>
    public partial class RelatedProduct : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the RelatedProduct class
        /// </summary>
        public RelatedProduct()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the related product identifier
        /// </summary>
        public int RelatedProductId { get; set; }

        /// <summary>
        /// Gets or sets the first product identifier
        /// </summary>
        public int ProductId1 { get; set; }

        /// <summary>
        /// Gets or sets the second product identifier
        /// </summary>
        public int ProductId2 { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
        #endregion 

        #region Custom Properties 
      
        /// <summary>
        /// Gets the first product
        /// </summary>
        public Product Product1
        {
            get
            {
                return ProductManager.GetProductById(this.ProductId1);
            }
        }

        /// <summary>
        /// Gets the second product
        /// </summary>
        public Product Product2
        {
            get
            {
                return ProductManager.GetProductById(this.ProductId2);
            }
        }
        #endregion
    }

}
