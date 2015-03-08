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
// Contributor(s): 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;

namespace NopSolutions.NopCommerce.Payment.Methods.SecurePay
{
    public class SecurePaySettings
    {
        #region Properties
        public static string MerchantId
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.SecurePay.MerchantID");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.SecurePay.MerchantID", value);
            }
        }

        public static string MerchantPassword
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.SecurePay.MerchantPassword");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.SecurePay.MerchantPassword", value);
            }
        }

        public static decimal AdditionalFee
        {
            get
            {
                return SettingManager.GetSettingValueDecimalNative("PaymentMethod.SecurePay.AdditionalFee");
            }
            set
            {
                SettingManager.SetParamNative("PaymentMethod.SecurePay.AdditionalFee", value);
            }
        }
        #endregion
    }
}
