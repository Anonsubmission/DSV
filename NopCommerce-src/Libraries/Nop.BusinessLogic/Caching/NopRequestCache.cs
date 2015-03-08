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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using NopSolutions.NopCommerce.BusinessLogic.Configuration;

namespace NopSolutions.NopCommerce.BusinessLogic.Caching
{
    /// <summary>
    /// Represents a NopRequestCache
    /// </summary>
    public partial class NopRequestCache
    {
        #region Ctor
        
        /// <summary>
        /// Creates a new instance of the NopRequestCache class
        /// </summary>
        private NopRequestCache()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new instance of the NopRequestCache class
        /// </summary>
        protected static IDictionary GetItems()
        {
            HttpContext current = HttpContext.Current;
            if (current != null)
            {
                return current.Items;
            }

            return null;
        }
        
        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public static object Get(string key)
        {
            var _items = GetItems();
            if (_items == null)
                return null;

            return _items[key];
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="obj">object</param>
        public static void Add(string key, object obj)
        {
            var _items = GetItems();
            if (_items == null)
                return;

            if (IsEnabled && (obj != null))
            {
                _items.Add(key, obj);
            }
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            var _items = GetItems();
            if (_items == null)
                return;

            _items.Remove(key);
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public static void RemoveByPattern(string pattern)
        {
            var _items = GetItems();
            if (_items == null)
                return;

            IDictionaryEnumerator enumerator = _items.GetEnumerator();
            Regex regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = new List<String>();
            while (enumerator.MoveNext())
            {
                if (regex.IsMatch(enumerator.Key.ToString()))
                {
                    keysToRemove.Add(enumerator.Key.ToString());
                }
            }

            foreach (string key in keysToRemove)
            {
                _items.Remove(key);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the cache is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        #endregion
    }
}
