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
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Caching;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.BusinessLogic.Data;
using NopSolutions.NopCommerce.Common.Utils;

namespace NopSolutions.NopCommerce.BusinessLogic.Tax
{
    /// <summary>
    /// Tax provider manager
    /// </summary>
    public partial class TaxProviderManager
    {
        #region Constants
        private const string TAXPROVIDERS_ALL_KEY = "Nop.taxprovider.all";
        private const string TAXPROVIDERS_BY_ID_KEY = "Nop.taxprovider.id-{0}";
        private const string TAXPROVIDERS_PATTERN_KEY = "Nop.taxprovider.";
        #endregion
        
        #region Methods
        /// <summary>
        /// Deletes a tax provider
        /// </summary>
        /// <param name="taxProviderId">Tax provider identifier</param>
        public static void DeleteTaxProvider(int taxProviderId)
        {
            var taxProvider = GetTaxProviderById(taxProviderId);
            if (taxProvider == null)
                return;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(taxProvider))
                context.TaxProviders.Attach(taxProvider);
            context.DeleteObject(taxProvider);
            context.SaveChanges();
            if (TaxProviderManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(TAXPROVIDERS_PATTERN_KEY);
            }
        }

        /// <summary>
        /// Gets a tax provider
        /// </summary>
        /// <param name="taxProviderId">Tax provider identifier</param>
        /// <returns>Tax provider</returns>
        public static TaxProvider GetTaxProviderById(int taxProviderId)
        {
            if (taxProviderId == 0)
                return null;

            string key = string.Format(TAXPROVIDERS_BY_ID_KEY, taxProviderId);
            object obj2 = NopRequestCache.Get(key);
            if (TaxProviderManager.CacheEnabled && (obj2 != null))
            {
                return (TaxProvider)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from tp in context.TaxProviders
                        where tp.TaxProviderId == taxProviderId
                        select tp;
            var taxProvider = query.SingleOrDefault();

            if (TaxProviderManager.CacheEnabled)
            {
                NopRequestCache.Add(key, taxProvider);
            }
            return taxProvider;
        }

        /// <summary>
        /// Gets all tax providers
        /// </summary>
        /// <returns>Shipping rate computation method collection</returns>
        public static List<TaxProvider> GetAllTaxProviders()
        {
            string key = string.Format(TAXPROVIDERS_ALL_KEY);
            object obj2 = NopRequestCache.Get(key);
            if (TaxProviderManager.CacheEnabled && (obj2 != null))
            {
                return (List<TaxProvider>)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from tp in context.TaxProviders
                        orderby tp.DisplayOrder
                        select tp;
            var taxProviders = query.ToList();

            if (TaxProviderManager.CacheEnabled)
            {
                NopRequestCache.Add(key, taxProviders);
            }
            return taxProviders;
        }

        /// <summary>
        /// Inserts a tax provider
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        /// <param name="configureTemplatePath">The configure template path</param>
        /// <param name="className">The class name</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>Tax provider</returns>
        public static TaxProvider InsertTaxProvider(string name, string description,
           string configureTemplatePath, string className, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            description = CommonHelper.EnsureMaximumLength(description, 4000);
            configureTemplatePath = CommonHelper.EnsureMaximumLength(configureTemplatePath, 500);
            className = CommonHelper.EnsureMaximumLength(className, 500);

            var context = ObjectContextHelper.CurrentObjectContext;

            var taxProvider = context.TaxProviders.CreateObject();
            taxProvider.Name = name;
            taxProvider.Description = description;
            taxProvider.ConfigureTemplatePath = configureTemplatePath;
            taxProvider.ClassName = className;
            taxProvider.DisplayOrder = displayOrder;

            context.TaxProviders.AddObject(taxProvider);
            context.SaveChanges();

            if (TaxProviderManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(TAXPROVIDERS_PATTERN_KEY);
            }
            return taxProvider;
        }

        /// <summary>
        /// Updates the tax provider
        /// </summary>
        /// <param name="taxProviderId">The tax provider identifier</param>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        /// <param name="configureTemplatePath">The configure template path</param>
        /// <param name="className">The class name</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>Tax provider</returns>
        public static TaxProvider UpdateTaxProvider(int taxProviderId,
            string name, string description, string configureTemplatePath,
            string className, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            description = CommonHelper.EnsureMaximumLength(description, 4000);
            configureTemplatePath = CommonHelper.EnsureMaximumLength(configureTemplatePath, 500);
            className = CommonHelper.EnsureMaximumLength(className, 500);

            var taxProvider = GetTaxProviderById(taxProviderId);
            if (taxProvider == null)
                return null;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(taxProvider))
                context.TaxProviders.Attach(taxProvider);

            taxProvider.Name = name;
            taxProvider.Description = description;
            taxProvider.ConfigureTemplatePath = configureTemplatePath;
            taxProvider.ClassName = className;
            taxProvider.DisplayOrder = displayOrder;
            context.SaveChanges();
            
            if (TaxProviderManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(TAXPROVIDERS_PATTERN_KEY);
            }
            return taxProvider;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether cache is enabled
        /// </summary>
        public static bool CacheEnabled
        {
            get
            {
                return SettingManager.GetSettingValueBoolean("Cache.TaxProviderManager.CacheEnabled");
            }
        }
        #endregion
    }
}
