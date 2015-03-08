================================================================
=========== Online Shopping with Amazon/Paypal =================
================================================================
AmazonWrapper/caas/...
 - contains wrapper service for the actual Amazon backend. We need this wrapper becuase
   we do not have access to Amazon's backend.
 - payComputation.cs contains a simplified version of what could be the actual back-end code.
 - Default.aspx.cs contains the redirection

PaypalWrapper/caas/...
 - A Paypal backend wrapper similar to the one for Amazon

NopCommerce-src/...
 - contains the Merchant's code
 - Payment/Nop.Payment.Amazon/AmazonHelper.cs - contains the core of our symT validation code
 - Payment/Nop.Payment.Amazon/SimplePayPaymentProcessor.cs - where we piggyback symT onto 
   NopCommerce's existing api call to Amazon's backend.
 - Payment/Nop.Payment.PayPal/PaypalHelper.cs - paypal symT validation core
 - Payment/Nop.Payment.Paypal/PaypalStandardPaymentProcessor.cs - where we piggyback symT onto
   NopCommerce's existing api call to Paypal's backend. 


================================================================
============ Third party Authentication OpenId 2.0 =============
================================================================
OpenAuth-src/...
 - /src/DotNetOpenAuth.OpenId/OpenId/Messages/PositiveAssertionResponse.cs - OpenID identity provider's core.
 - /samples/OpenIdRelyingPartyMvc/Controllers/UserController.cs - Where symT is validated
 - /src/DotNetOpenAuth.Core/Core_CCP.cs - contains the relavent relying party functions we verify

================================================================
========= Third party Authentication Facebook OAuth ============
================================================================
FacebookWrapper/caas/...
 - Wrapper for Facebook's server side code. Revalent files: /caas/oauth_req.aspx.cs and
   /caas/oauth_resp.aspx.cs

ASPNetWebStack/DotNetOpenAuth/src/...
 - contains relying party code
 - DotNetOpenAuth.AspNet/Clients/OAuth2/FacebookClient.cs - relying party core
 - DotNetOpenAuth.AspNet\Clients\OAuth2\OAuth2Client.cs - code that verifies symT


================================================================
Proof-of-concept Gambling website with 4 different independant services
================================================================
Cointoss/cointoss/...
   - contains our gambling service
   - \cointoss\Controllers\HomeController.cs - core code

Payment uses Amazon simple pay (refer to the previous section on Amazon Simplepay wrapper)


