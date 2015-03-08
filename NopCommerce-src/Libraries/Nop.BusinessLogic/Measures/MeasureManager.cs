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
using NopSolutions.NopCommerce.Common;
using NopSolutions.NopCommerce.Common.Utils;

namespace NopSolutions.NopCommerce.BusinessLogic.Measures
{
    /// <summary>
    /// Measure dimension manager
    /// </summary>
    public partial class MeasureManager
    {
        #region Constants
        private const string MEASUREDIMENSIONS_ALL_KEY = "Nop.measuredimension.all";
        private const string MEASUREDIMENSIONS_BY_ID_KEY = "Nop.measuredimension.id-{0}";
        private const string MEASUREWEIGHTS_ALL_KEY = "Nop.measureweight.all";
        private const string MEASUREWEIGHTS_BY_ID_KEY = "Nop.measureweight.id-{0}";
        private const string MEASUREDIMENSIONS_PATTERN_KEY = "Nop.measuredimension.";
        private const string MEASUREWEIGHTS_PATTERN_KEY = "Nop.measureweight.";
        #endregion

        #region Methods

        #region Dimensions
        /// <summary>
        /// Deletes measure dimension
        /// </summary>
        /// <param name="measureDimensionId">Measure dimension identifier</param>
        public static void DeleteMeasureDimension(int measureDimensionId)
        {
            var measureDimension = GetMeasureDimensionById(measureDimensionId);
            if (measureDimension == null)
                return;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(measureDimension))
                context.MeasureDimensions.Attach(measureDimension);
            context.DeleteObject(measureDimension);
            context.SaveChanges();
            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREDIMENSIONS_PATTERN_KEY);
            }
        }

        /// <summary>
        /// Gets a measure dimension by identifier
        /// </summary>
        /// <param name="measureDimensionId">Measure dimension identifier</param>
        /// <returns>Measure dimension</returns>
        public static MeasureDimension GetMeasureDimensionById(int measureDimensionId)
        {
            if (measureDimensionId == 0)
                return null;

            string key = string.Format(MEASUREDIMENSIONS_BY_ID_KEY, measureDimensionId);
            object obj2 = NopRequestCache.Get(key);
            if (MeasureManager.CacheEnabled && (obj2 != null))
            {
                return (MeasureDimension)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from md in context.MeasureDimensions
                        where md.MeasureDimensionId == measureDimensionId
                        select md;
            var measureDimension = query.SingleOrDefault();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.Add(key, measureDimension);
            }
            return measureDimension;
        }

        /// <summary>
        /// Gets a measure dimension by system keyword
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <returns>Measure dimension</returns>
        public static MeasureDimension GetMeasureDimensionBySystemKeyword(string systemKeyword)
        {
            if (String.IsNullOrEmpty(systemKeyword))
                return null;

            var measureDimensions = GetAllMeasureDimensions();
            foreach (var measureDimension in measureDimensions)
                if (measureDimension.SystemKeyword.ToLowerInvariant() == systemKeyword.ToLowerInvariant())
                    return measureDimension;
            return null;
        }

        /// <summary>
        /// Gets all measure dimensions
        /// </summary>
        /// <returns>Measure dimension collection</returns>
        public static List<MeasureDimension> GetAllMeasureDimensions()
        {
            string key = MEASUREDIMENSIONS_ALL_KEY;
            object obj2 = NopRequestCache.Get(key);
            if (MeasureManager.CacheEnabled && (obj2 != null))
            {
                return (List<MeasureDimension>)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from md in context.MeasureDimensions
                        orderby md.DisplayOrder
                        select md;
            var measureDimensionCollection = query.ToList();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.Add(key, measureDimensionCollection);
            }
            return measureDimensionCollection;
        }

        /// <summary>
        /// Inserts a measure dimension
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="ratio">The ratio</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>A measure dimension</returns>
        public static MeasureDimension InsertMeasureDimension(string name,
            string systemKeyword, decimal ratio, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            systemKeyword = CommonHelper.EnsureMaximumLength(systemKeyword, 100);

            var context = ObjectContextHelper.CurrentObjectContext;

            var measure = context.MeasureDimensions.CreateObject();
            measure.Name = name;
            measure.SystemKeyword = systemKeyword;
            measure.Ratio = ratio;
            measure.DisplayOrder = displayOrder;

            context.MeasureDimensions.AddObject(measure);
            context.SaveChanges();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREDIMENSIONS_PATTERN_KEY);
            }
            return measure;
        }

        /// <summary>
        /// Updates the measure dimension
        /// </summary>
        /// <param name="measureDimensionId">Measure dimension identifier</param>
        /// <param name="name">The name</param>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="ratio">The ratio</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>A measure dimension</returns>
        public static MeasureDimension UpdateMeasureDimension(int measureDimensionId,
            string name, string systemKeyword, decimal ratio, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            systemKeyword = CommonHelper.EnsureMaximumLength(systemKeyword, 100);

            var measure = GetMeasureDimensionById(measureDimensionId);
            if (measure == null)
                return null;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(measure))
                context.MeasureDimensions.Attach(measure);

            measure.Name = name;
            measure.SystemKeyword = systemKeyword;
            measure.Ratio = ratio;
            measure.DisplayOrder = displayOrder;
            context.SaveChanges();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREDIMENSIONS_PATTERN_KEY);
            }
            return measure;
        }

        /// <summary>
        /// Converts dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="sourceMeasureDimension">Source dimension</param>
        /// <param name="targetMeasureDimension">Target dimension</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertDimension(decimal quantity,
            MeasureDimension sourceMeasureDimension, MeasureDimension targetMeasureDimension)
        {
            decimal result = quantity;
            if (sourceMeasureDimension.MeasureDimensionId == targetMeasureDimension.MeasureDimensionId)
                return result;
            if (result != decimal.Zero && sourceMeasureDimension.MeasureDimensionId != targetMeasureDimension.MeasureDimensionId)
            {
                result = ConvertToPrimaryMeasureDimension(result, sourceMeasureDimension);
                result = ConvertFromPrimaryMeasureDimension(result, targetMeasureDimension);
            }
            result = Math.Round(result, 2);
            return result;
        }

        /// <summary>
        /// Converts to primary measure dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="sourceMeasureDimension">Source dimension</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertToPrimaryMeasureDimension(decimal quantity,
            MeasureDimension sourceMeasureDimension)
        {
            decimal result = quantity;
            if (result != decimal.Zero && sourceMeasureDimension.MeasureDimensionId != BaseDimensionIn.MeasureDimensionId)
            {
                decimal exchangeRatio = sourceMeasureDimension.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new NopException(string.Format("Exchange ratio not set for dimension [{0}]", sourceMeasureDimension.Name));
                result = result / exchangeRatio;
            }
            return result;
        }

        /// <summary>
        /// Converts from primary dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="targetMeasureDimension">Target dimension</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertFromPrimaryMeasureDimension(decimal quantity,
            MeasureDimension targetMeasureDimension)
        {
            decimal result = quantity;
            if (result != decimal.Zero && targetMeasureDimension.MeasureDimensionId != BaseDimensionIn.MeasureDimensionId)
            {
                decimal exchangeRatio = targetMeasureDimension.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new NopException(string.Format("Exchange ratio not set for dimension [{0}]", targetMeasureDimension.Name));
                result = result * exchangeRatio;
            }
            return result;
        }

        #endregion

        #region Weights

        /// <summary>
        /// Deletes measure weight
        /// </summary>
        /// <param name="measureWeightId">Measure weight identifier</param>
        public static void DeleteMeasureWeight(int measureWeightId)
        {
            var measureWeight = GetMeasureWeightById(measureWeightId);
            if (measureWeight == null)
                return;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(measureWeight))
                context.MeasureWeights.Attach(measureWeight);
            context.DeleteObject(measureWeight);
            context.SaveChanges();
            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREWEIGHTS_PATTERN_KEY);
            }
        }

        /// <summary>
        /// Gets a measure weight by identifier
        /// </summary>
        /// <param name="measureWeightId">Measure weight identifier</param>
        /// <returns>Measure weight</returns>
        public static MeasureWeight GetMeasureWeightById(int measureWeightId)
        {
            if (measureWeightId == 0)
                return null;

            string key = string.Format(MEASUREWEIGHTS_BY_ID_KEY, measureWeightId);
            object obj2 = NopRequestCache.Get(key);
            if (MeasureManager.CacheEnabled && (obj2 != null))
            {
                return (MeasureWeight)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from mw in context.MeasureWeights
                        where mw.MeasureWeightId == measureWeightId
                        select mw;
            var measureWeight = query.SingleOrDefault();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.Add(key, measureWeight);
            }
            return measureWeight;
        }

        /// <summary>
        /// Gets a measure weight by system keyword
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <returns>Measure weight</returns>
        public static MeasureWeight GetMeasureWeightBySystemKeyword(string systemKeyword)
        {
            if (String.IsNullOrEmpty(systemKeyword))
                return null;

            var measureWeights = GetAllMeasureWeights();
            foreach (var measureWeight in measureWeights)
                if (measureWeight.SystemKeyword.ToLowerInvariant() == systemKeyword.ToLowerInvariant())
                    return measureWeight;
            return null;
        }

        /// <summary>
        /// Gets all measure weights
        /// </summary>
        /// <returns>Measure weight collection</returns>
        public static List<MeasureWeight> GetAllMeasureWeights()
        {
            string key = MEASUREWEIGHTS_ALL_KEY;
            object obj2 = NopRequestCache.Get(key);
            if (MeasureManager.CacheEnabled && (obj2 != null))
            {
                return (List<MeasureWeight>)obj2;
            }

            var context = ObjectContextHelper.CurrentObjectContext;
            var query = from mw in context.MeasureWeights
                        orderby mw.DisplayOrder
                        select mw;
            var measureWeightCollection = query.ToList();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.Add(key, measureWeightCollection);
            }
            return measureWeightCollection;
        }

        /// <summary>
        /// Inserts a measure weight
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="ratio">The ratio</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>A measure weight</returns>
        public static MeasureWeight InsertMeasureWeight(string name,
            string systemKeyword, decimal ratio, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            systemKeyword = CommonHelper.EnsureMaximumLength(systemKeyword, 100);

            var context = ObjectContextHelper.CurrentObjectContext;

            var weight = context.MeasureWeights.CreateObject();
            weight.Name = name;
            weight.SystemKeyword = systemKeyword;
            weight.Ratio = ratio;
            weight.DisplayOrder = displayOrder;

            context.MeasureWeights.AddObject(weight);
            context.SaveChanges();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREWEIGHTS_PATTERN_KEY);
            }
            return weight;
        }

        /// <summary>
        /// Updates the measure weight
        /// </summary>
        /// <param name="measureWeightId">Measure weight identifier</param>
        /// <param name="name">The name</param>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="ratio">The ratio</param>
        /// <param name="displayOrder">The display order</param>
        /// <returns>A measure weight</returns>
        public static MeasureWeight UpdateMeasureWeight(int measureWeightId, string name,
            string systemKeyword, decimal ratio, int displayOrder)
        {
            name = CommonHelper.EnsureMaximumLength(name, 100);
            systemKeyword = CommonHelper.EnsureMaximumLength(systemKeyword, 100);

            var weight = GetMeasureWeightById(measureWeightId);
            if (weight == null)
                return null;

            var context = ObjectContextHelper.CurrentObjectContext;
            if (!context.IsAttached(weight))
                context.MeasureWeights.Attach(weight);

            weight.Name = name;
            weight.SystemKeyword = systemKeyword;
            weight.Ratio = ratio;
            weight.DisplayOrder = displayOrder;
            context.SaveChanges();

            if (MeasureManager.CacheEnabled)
            {
                NopRequestCache.RemoveByPattern(MEASUREWEIGHTS_PATTERN_KEY);
            }
            return weight;
        }

        /// <summary>
        /// Converts weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="sourceMeasureWeight">Source weight</param>
        /// <param name="targetMeasureWeight">Target weight</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertWeight(decimal quantity,
            MeasureWeight sourceMeasureWeight, MeasureWeight targetMeasureWeight)
        {
            decimal result = quantity;
            if (sourceMeasureWeight.MeasureWeightId == targetMeasureWeight.MeasureWeightId)
                return result;
            if (result != decimal.Zero && sourceMeasureWeight.MeasureWeightId != targetMeasureWeight.MeasureWeightId)
            {
                result = ConvertToPrimaryMeasureWeight(result, sourceMeasureWeight);
                result = ConvertFromPrimaryMeasureWeight(result, targetMeasureWeight);
            }
            result = Math.Round(result, 2);
            return result;
        }

        /// <summary>
        /// Converts to primary measure weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="sourceMeasureWeight">Source weight</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertToPrimaryMeasureWeight(decimal quantity, MeasureWeight sourceMeasureWeight)
        {
            decimal result = quantity;
            if (result != decimal.Zero && sourceMeasureWeight.MeasureWeightId != BaseWeightIn.MeasureWeightId)
            {
                decimal exchangeRatio = sourceMeasureWeight.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new NopException(string.Format("Exchange ratio not set for weight [{0}]", sourceMeasureWeight.Name));
                result = result / exchangeRatio;
            }
            return result;
        }

        /// <summary>
        /// Converts from primary weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="targetMeasureWeight">Target weight</param>
        /// <returns>Converted value</returns>
        public static decimal ConvertFromPrimaryMeasureWeight(decimal quantity,
            MeasureWeight targetMeasureWeight)
        {
            decimal result = quantity;
            if (result != decimal.Zero && targetMeasureWeight.MeasureWeightId != BaseWeightIn.MeasureWeightId)
            {
                decimal exchangeRatio = targetMeasureWeight.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new NopException(string.Format("Exchange ratio not set for weight [{0}]", targetMeasureWeight.Name));
                result = result * exchangeRatio;
            }
            return result;
        }

        #endregion

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the dimension that will be used as a default
        /// </summary>
        public static MeasureDimension BaseDimensionIn
        {
            get
            {
                int baseDimensionIn = SettingManager.GetSettingValueInteger("Common.BaseDimensionIn");
                return MeasureManager.GetMeasureDimensionById(baseDimensionIn);
            }
            set
            {
                if (value != null)
                    SettingManager.SetParam("Common.BaseDimensionIn", value.MeasureDimensionId.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the weight that will be used as a default
        /// </summary>
        public static MeasureWeight BaseWeightIn
        {
            get
            {
                int baseWeightIn = SettingManager.GetSettingValueInteger("Common.BaseWeightIn");
                return MeasureManager.GetMeasureWeightById(baseWeightIn);
            }
            set
            {
                if (value != null)
                    SettingManager.SetParam("Common.BaseWeightIn", value.MeasureWeightId.ToString());
            }
        }

        /// <summary>
        /// Gets a value indicating whether cache is enabled
        /// </summary>
        public static bool CacheEnabled
        {
            get
            {
                return SettingManager.GetSettingValueBoolean("Cache.MeasureManager.CacheEnabled");
            }
        }
        #endregion
    }
}