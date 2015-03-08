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
using System.Globalization;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Directory;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.Shipping;
using NopSolutions.NopCommerce.Common.Utils;

namespace NopSolutions.NopCommerce.Payment.Methods.CyberSource
{
    /// <summary>
    /// Represents an CyberSource hosted payment gateway
    /// </summary>
    public class HostedPaymentProcessor : IPaymentMethod
    {
        #region Methods
        /// <summary>
        /// Process payment
        /// </summary>
        /// <param name="paymentInfo">Payment info required for an order processing</param>
        /// <param name="customer">Customer</param>
        /// <param name="orderGuid">Unique order identifier</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void ProcessPayment(PaymentInfo paymentInfo, Customer customer, Guid orderGuid, ref ProcessPaymentResult processPaymentResult)
        {
            processPaymentResult.PaymentStatus = PaymentStatusEnum.Pending;
        }

        /// <summary>
        /// Post process payment (payment gateways that require redirecting)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>The error status, or String.Empty if no errors</returns>
        public string PostProcessPayment(Order order)
        {
            RemotePost post = new RemotePost();

            post.FormName = "CyberSource";
            post.Url = HostedPaymentSettings.GatewayUrl;
            post.Method = "POST";

            post.Add("merchantID", HostedPaymentSettings.MerchantId);
            post.Add("orderPage_timestamp", HostedPaymentHelper.OrderPageTimestamp);
            post.Add("orderPage_transactionType", "authorization");
            post.Add("orderPage_version", "4");
            post.Add("orderPage_serialNumber", HostedPaymentSettings.SerialNumber);

            post.Add("amount", String.Format(CultureInfo.InvariantCulture, "{0:0.00}", order.OrderTotal));
            post.Add("currency", order.CustomerCurrencyCode);
            post.Add("orderNumber", order.OrderId.ToString());

            post.Add("billTo_firstName", order.BillingFirstName);
            post.Add("billTo_lastName", order.BillingLastName);
            post.Add("billTo_street1", order.BillingAddress1);
            Country billCountry = CountryManager.GetCountryById(order.BillingCountryId);
            if(billCountry != null)
            {
                post.Add("billTo_country", billCountry.TwoLetterIsoCode);
            }
            StateProvince billState = StateProvinceManager.GetStateProvinceById(order.BillingStateProvinceId);
            if(billState != null)
            {
                post.Add("billTo_state", billState.Abbreviation);
            }
            post.Add("billTo_city", order.BillingCity);
            post.Add("billTo_postalCode", order.BillingZipPostalCode);
            post.Add("billTo_phoneNumber", order.BillingPhoneNumber);
            post.Add("billTo_email", order.BillingEmail);

            if (order.ShippingStatus != ShippingStatusEnum.ShippingNotRequired)
            {
                post.Add("shipTo_firstName", order.ShippingFirstName);
                post.Add("shipTo_lastName", order.ShippingLastName);
                post.Add("shipTo_street1", order.ShippingAddress1);
                Country shipCountry = CountryManager.GetCountryById(order.ShippingCountryId);
                if (shipCountry != null)
                {
                    post.Add("shipTo_country", shipCountry.TwoLetterIsoCode);
                }
                StateProvince shipState = StateProvinceManager.GetStateProvinceById(order.ShippingStateProvinceId);
                if (shipState != null)
                {
                    post.Add("shipTo_state", shipState.Abbreviation);
                }
                post.Add("shipTo_city", order.ShippingCity);
                post.Add("shipTo_postalCode", order.ShippingZipPostalCode);
            }

            post.Add("orderPage_receiptResponseURL", String.Format("{0}CheckoutCompleted.aspx", CommonHelper.GetStoreLocation(false)));
            post.Add("orderPage_receiptLinkText", "Return");

            post.Add("orderPage_signaturePublic", HostedPaymentHelper.CalcRequestSign(post.Params));

            post.Post();

            return String.Empty;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee()
        {
            return HostedPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void Capture(Order order, ref ProcessPaymentResult processPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Refunds payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void Refund(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Voids paymen
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void Void(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="paymentInfo">Payment info required for an order processing</param>
        /// <param name="customer">Customer</param>
        /// <param name="orderGuid">Unique order identifier</param>
        /// <param name="processPaymentResult">Process payment result</param>
        public void ProcessRecurringPayment(PaymentInfo paymentInfo, Customer customer, Guid orderGuid, ref ProcessPaymentResult processPaymentResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cancels recurring payment
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="cancelPaymentResult">Cancel payment result</param>
        public void CancelRecurringPayment(Order order, ref CancelPaymentResult cancelPaymentResult)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool CanCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool CanPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool CanRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool CanVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        /// <returns>A recurring payment type of payment method</returns>
        public RecurringPaymentTypeEnum SupportRecurringPayments
        {
            get
            {
                return RecurringPaymentTypeEnum.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        /// <returns>A payment method type</returns>
        public PaymentMethodTypeEnum PaymentMethodType
        {
            get
            {
                return PaymentMethodTypeEnum.Standard;
            }
        }
        #endregion
    }
}
