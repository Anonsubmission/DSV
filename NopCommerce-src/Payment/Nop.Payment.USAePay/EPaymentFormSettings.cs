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

namespace NopSolutions.NopCommerce.Payment.Methods.USAePay
{
    public class EPaymentFormSettings
    {
        #region Properties
        public static string GatewayUrl
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.USAePay.EPaymentForm.GatewayUrl", "https://sandbox.usaepay.com/interface/epayform/");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.GatewayUrl", value);
            }
        }

        public static string ServiceUrl
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.USAePay.EPaymentForm.ServiceUrl", "https://sandbox.usaepay.com/soap/gate/3213EA2A");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.ServiceUrl", value);
            }
        }

        public static string SourceKey
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.USAePay.EPaymentForm.SourceKey");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.SourceKey", value);
            }
        }

        public static bool AuthorizeOnly
        {
            get
            {
                return SettingManager.GetSettingValueBoolean("PaymentMethod.USAePay.EPaymentForm.AuthorizeOnly", false);
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.AuthorizeOnly", value.ToString());
            }
        }

        public static bool UsePIN
        {
            get
            {
                return SettingManager.GetSettingValueBoolean("PaymentMethod.USAePay.EPaymentForm.UsePIN", true);
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.UsePIN", value.ToString());
            }
        }

        public static string PIN
        {
            get
            {
                return SettingManager.GetSettingValue("PaymentMethod.USAePay.EPaymentForm.PIN");
            }
            set
            {
                SettingManager.SetParam("PaymentMethod.USAePay.EPaymentForm.PIN", value);
            }
        }

        public static decimal AdditionalFee
        {
            get
            {
                return SettingManager.GetSettingValueDecimalNative("PaymentMethod.USAePay.EPaymentForm.AdditionalFee");
            }
            set
            {
                SettingManager.SetParamNative("PaymentMethod.USAePay.EPaymentForm.AdditionalFee", value);
            }
        }
        #endregion
    }
}
