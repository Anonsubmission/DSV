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



namespace NopSolutions.NopCommerce.BusinessLogic.Measures
{
    /// <summary>
    /// Represents a measure dimension
    /// </summary>
    public partial class MeasureDimension : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the MeasureDimension class
        /// </summary>
        public MeasureDimension()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the measure dimension identifier
        /// </summary>
        public int MeasureDimensionId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the system keyword
        /// </summary>
        public string SystemKeyword { get; set; }

        /// <summary>
        /// Gets or sets the ratio
        /// </summary>
        public decimal Ratio { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        #endregion

        #region Custom Properties

        /// <summary>
        /// Gets or a value indicating whether the dimension is primary dimension
        /// </summary>
        public bool IsPrimaryDimension
        {
            get
            {
                MeasureDimension primaryMeasureDimension = MeasureManager.BaseDimensionIn;
                return ((primaryMeasureDimension != null && primaryMeasureDimension.MeasureDimensionId == this.MeasureDimensionId));
            }
        }
        #endregion
    }
}
