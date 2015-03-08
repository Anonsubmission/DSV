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

using NopSolutions.NopCommerce.BusinessLogic.Shipping;
using NopSolutions.NopCommerce.BusinessLogic.Tax;

namespace NopSolutions.NopCommerce.BusinessLogic.Tax
{
    /// <summary>
    /// Represents a tax provider
    /// </summary>
    public partial class TaxProvider : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the tax provider class
        /// </summary>
        public TaxProvider()
        {
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the tax provider identifier
        /// </summary>
        public int TaxProviderId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the configure template path
        /// </summary>
        public string ConfigureTemplatePath { get; set; }

        /// <summary>
        /// Gets or sets the class name
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        #endregion

        #region Custom Properties 
        
        /// <summary>
        /// Gets or a value indicating whether the entity is default
        /// </summary>
        public bool IsDefault
        {
            get
            {
                TaxProvider activeTaxProvider = TaxManager.ActiveTaxProvider;
                return ((activeTaxProvider != null && activeTaxProvider.TaxProviderId == this.TaxProviderId));
            }
        }
        #endregion
    }
}