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
using System.Linq;
using System.Text;


namespace NopSolutions.NopCommerce.BusinessLogic.Tax
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Finds tax rate
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="taxCategoryId">Tax category identifier</param>
        /// <returns>Found tax rates</returns>
        public static List<TaxRate> FindTaxRates(this List<TaxRate> source,
            int countryId, int taxCategoryId)
        {
            var result = new List<TaxRate>();
            foreach (TaxRate taxRate in source)
            {
                if (taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId)
                    result.Add(taxRate);
            }
            return result;
        }
    }
}
