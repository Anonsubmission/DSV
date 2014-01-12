﻿//------------------------------------------------------------------------------
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

namespace NopSolutions.NopCommerce.BusinessLogic.Security
{
    /// <summary>
    /// Network IP address range implementation
    /// </summary>
    public partial class BannedIpNetwork : BaseEntity
    {
        #region Constructor
        /// <summary>
        /// Default constructor. Creates a new instance of IpNetwork
        /// </summary>
        public BannedIpNetwork() : base() { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the IP address unique identifier
        /// </summary>
        public int BannedIpNetworkId { get; set; }

        /// <summary>
        /// Gets or sets the starting IP address in the range
        /// </summary>
        public string StartAddress { get; set; }

        /// <summary>
        /// Gets or sets the ending IP address in the range
        /// </summary>
        public string EndAddress { get; set; }

        /// <summary>
        /// Gets or sets a reason why the IP network was banned
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets a list of exceptions in the IP Network
        /// </summary>
        public string IpException { get; set; }

        /// <summary>
        /// Gets or sets when the IP address record was banned
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets when the banned IP address record was last updated
        /// </summary>
        public DateTime UpdatedOn { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Returns the IP range as a formatted string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}-{1}", StartAddress, EndAddress);
        }
        #endregion
    }
}
