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

namespace NopSolutions.NopCommerce.BusinessLogic.Shipping
{
    /// <summary>
    /// Shipping rate computation method manager
    /// </summary>
    public partial class ShippingRateComputationMethodManager
    {
        #region Constants
        private const string SHIPPINGRATECOMPUTATIONMETHODS_ALL_KEY = "Nop.shippingratecomputationmethod.all-{0}";
        private const string SHIPPINGRATECOMPUTATIONMETHODS_BY_ID_KEY = "Nop.shippingratecomputationmethod.id-{0}";
        private const string SHIPPINGRATECOMPUTATIONMETHODS_PATTERN_KEY = "Nop.shippingratecomputationmethod.";
        #endregion

        #region Methods

        /// <summary>
        /// Deletes a shipping rate computation method
        /// </summary>
        /// <param name="shippingRateComputationMethodId">Shipping rate computation method identifier</param>
        public static void DeleteShippingRateComputationMethod(int shippingRateComputationMethodId)
        {
            var shippingRateComputationMethod = GetShippingRateComputationMethodById(shippingRateComputationMethodId);
            if (shippingRateComputationMethod == null)
                return;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(shippingRateComputationMethod))
                context.ShippingRateComputationMethods.Attach(shippingRateComputationMethod);
            context.DeleteObject(shippingRateComputationMethod);
            context.SaveChanges();

            if (ShippingRateComputationMethodManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(SHIPPINGRATECOMPUTATIONMETHODS_PATTERN_KEY);
            }
        }

        /// <summary>
        /// Gets a shipping rate computation method
        /// </summary>
        /// <param name="shippingRateComputationMethodId">Shipping rate computation method identifier</param>
        /// <returns>Shipping rate computation method</returns>
        public static ShippingRateComputationMethod GetShippingRateComputationMethodById(int shippingRateComputationMethodId)
        {
            if (shippingRateComputationMethodId == 0)
                return null;

            string key = string.Format(SHIPPINGRATECOMPUTATIONMETHODS_BY_ID_KEY, shippingRateComputationMethodId);
            object obj2 = NopRequestCache.Get(key);
            if (ShippingRateComputationMethodManager.CacheEnabled && (obj2 != null))
            {
                return (ShippingRateComputationMethod)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from s in context.ShippingRateComputationMethods
                        where s.ShippingRateComputationMethodId == shippingRateComputationMethodId
                        select s;
            var shippingRateComputationMethod = query.SingleOrDefault();

            if (ShippingRateComputationMethodManager.CacheEnabled)
            {
                NopRequestCache.Add(key, shippingRateComputationMethod);
            }
            return shippingRateComputationMethod;
        }

        /// <summary>
        /// Gets all shipping rate computation methods
        /// </summary>
        /// <returns>Shipping rate computation method collection</returns>
        public static List<ShippingRateComputationMethod> GetAllShippingRateComputationMethods()
        {
            bool showHidden = NopContext.Current.IsAdmin;
            return GetAllShippingRateComputationMethods(showHidden);
        }

        /// <summary>
        /// Gets all shipping rate computation methods
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Shipping rate computation method collection</returns>
        public static List<ShippingRateComputationMethod> GetAllShippingRateComputationMethods(bool showHidden)
        {
            string key = string.Format(SHIPPINGRATECOMPUTATIONMETHODS_ALL_KEY, showHidden);
            object obj2 = NopRequestCache.Get(key);
            if (ShippingRateComputationMethodManager.CacheEnabled && (obj2 != null))
            {
                return (List<ShippingRateComputationMethod>)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from s in context.ShippingRateComputationMethods
                        orderby s.DisplayOrder
                        where showHidden || s.IsActive
                        select s;
            var shippingRateComputationMethods = query.ToList();

            if (ShippingRateComputationMethodManager.CacheEnabled)
            {
                NopRequestCache.Add(key, shippingRateComputationMethods);
            }
            return shippingRateComputationMethods;
        }

        /// <summary>
        /// Inserts a shipping rate computation method
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        /// <param name="configureTemplatePath">The configure template path</param>
        /// <param name="className">The class name</param>
        /// <param name="isActive">The value indicating whether the method is active</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>Shipping rate computation method</returns>
        public static ShippingRateComputationMethod InsertShippingRateComputationMethod(string name,
            string description, string configureTemplatePath, string className,
            bool isActive, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            description = CommonHelper.EnsureMaximumLength(description, 4000);
            configureTemplatePath = CommonHelper.EnsureMaximumLength(configureTemplatePath, 500);
            className = CommonHelper.EnsureMaximumLength(className, 500);

            var context = ObjectContextHelper.CurrentObjectContext;

            var shippingRateComputationMethod = context.ShippingRateComputationMethods.CreateObject();
            shippingRateComputationMethod.Name = name;
            shippingRateComputationMethod.Description = description;
            shippingRateComputationMethod.ConfigureTemplatePath = configureTemplatePath;
            shippingRateComputationMethod.ClassName = className;
            shippingRateComputationMethod.IsActive = isActive;
            shippingRateComputationMethod.DisplayOrder = displayOrder;

            context.ShippingRateComputationMethods.AddObject(shippingRateComputationMethod);
            context.SaveChanges();

            if (ShippingRateComputationMethodManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(SHIPPINGRATECOMPUTATIONMETHODS_PATTERN_KEY);
            }
            return shippingRateComputationMethod;
        }

        /// <summary>
        /// Updates the shipping rate computation method
        /// </summary>
        /// <param name="shippingRateComputationMethodId">The shipping rate computation method identifier</param>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        /// <param name="configureTemplatePath">The configure template path</param>
        /// <param name="className">The class name</param>
        /// <param name="isActive">The value indicating whether the method is active</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>Shipping rate computation method</returns>
        public static ShippingRateComputationMethod UpdateShippingRateComputationMethod(int shippingRateComputationMethodId,
            string name, string description, string configureTemplatePath, string className,
            bool isActive, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            description = CommonHelper.EnsureMaximumLength(description, 4000);
            configureTemplatePath = CommonHelper.EnsureMaximumLength(configureTemplatePath, 500);
            className = CommonHelper.EnsureMaximumLength(className, 500);

            var shippingRateComputationMethod = GetShippingRateComputationMethodById(shippingRateComputationMethodId);
            if (shippingRateComputationMethod == null)
                return null;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(shippingRateComputationMethod))
                context.ShippingRateComputationMethods.Attach(shippingRateComputationMethod);

            shippingRateComputationMethod.Name = name;
            shippingRateComputationMethod.Description = description;
            shippingRateComputationMethod.ConfigureTemplatePath = configureTemplatePath;
            shippingRateComputationMethod.ClassName = className;
            shippingRateComputationMethod.IsActive = isActive;
            shippingRateComputationMethod.DisplayOrder = displayOrder;
            context.SaveChanges();

            if (ShippingRateComputationMethodManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(SHIPPINGRATECOMPUTATIONMETHODS_PATTERN_KEY);
            }
            return shippingRateComputationMethod;
        }
        
        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        /// <param name="shippingRateComputationMethodId">The shipping rate computation method identifier</param>
        /// <returns>A shipping rate computation method type</returns>
        public static ShippingRateComputationMethodTypeEnum GetShippingRateComputationMethodTypeEnum(int shippingRateComputationMethodId)
        {
            var method = GetShippingRateComputationMethodById(shippingRateComputationMethodId);
            if (method == null)
                return ShippingRateComputationMethodTypeEnum.Unknown;
            var iMethod = Activator.CreateInstance(Type.GetType(method.ClassName)) as IShippingRateComputationMethod;
            return iMethod.ShippingRateComputationMethodType;
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
                return SettingManager.GetSettingValueBoolean("Cache.ShippingRateComputationMethodManager.CacheEnabled");
            }
        }
        #endregion
    }
}