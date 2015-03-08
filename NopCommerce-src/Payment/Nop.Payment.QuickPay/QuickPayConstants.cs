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
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.Security;
using NopSolutions.NopCommerce.Common;

namespace NopSolutions.NopCommerce.Payment.Methods.QuickPay
{
    public static class QuickPayConstants
    {
        public const string SETTING_CREDITCARD_CODE_PROPERTY = "PaymentMethod.QuickPay.CardPayment";
        public const string SETTING_USE_SANDBOX = "PaymentMethod.QuickPay.UseSandbox";
        public const string SETTING_MERCHANTID = "PaymentMethod.QuickPay.MerchantID";
        public const string SETTING_MD5SECRET = "PaymentMethod.QuickPay.MD5Secret";
    }
}
