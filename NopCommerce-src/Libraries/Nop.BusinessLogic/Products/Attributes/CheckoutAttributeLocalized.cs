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
using NopSolutions.NopCommerce.BusinessLogic.Media;
using NopSolutions.NopCommerce.BusinessLogic.Promo.Discounts;
using NopSolutions.NopCommerce.BusinessLogic.Templates;
using NopSolutions.NopCommerce.BusinessLogic.Products;
using System.Globalization;



namespace NopSolutions.NopCommerce.BusinessLogic.Products.Attributes
{
    /// <summary>
    /// Represents a localized checkout attribute
    /// </summary>
    public partial class CheckoutAttributeLocalized : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the CheckoutAttributeLocalized class
        /// </summary>
        public CheckoutAttributeLocalized()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the localized checkout attribute identifier
        /// </summary>
        public int CheckoutAttributeLocalizedId { get; set; }

        /// <summary>
        /// Gets or sets the checkout attribute identifier
        /// </summary>
        public int CheckoutAttributeId { get; set; }

        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text prompt
        /// </summary>
        public string TextPrompt { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets the checkout attribute
        /// </summary>
        public virtual CheckoutAttribute NpCheckoutAttribute { get; set; }

        #endregion
    }
}
