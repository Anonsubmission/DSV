<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="oauth_resp.aspx.cs" enableEventValidation="false" Inherits="caas.oauth_resp" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
 
    
    <form id="form1" runat="server" >
    <div>
    
        <asp:Label ID="UrlLabel" runat="server" Text="aaaaa"></asp:Label>

        <br />
        <!--
        <asp:HiddenField id="immediateReturn" runat="server" value="1"/>
        <asp:HiddenField id="signatureVersion" runat="server" value="2"/>
        <asp:HiddenField id="signatureMethod" runat="server" value="1"/>
        <asp:HiddenField id="accessKey" runat="server" value="HmacSHA256"/>
        <asp:HiddenField id="amount" runat="server" value="USD 25.00"/>
        <asp:HiddenField id="description" runat="server" value="description=Your store name, 4"/>
        <asp:HiddenField id="amazonPaymentsAccountId" runat="server" value="IGFCUTPWGXVM311K1E6QTXIQ1RPEIUG5PTIMUZ"/>
        <asp:HiddenField id="returnUrl" runat="server" value="http://localhost:8242/AmazonSimplePayReturn.aspx"/>
        <asp:HiddenField id="processImmediate" runat="server" value="0"/>
        <asp:HiddenField id="referenceId" runat="server" value="4"/>
        <asp:HiddenField id="signature" runat="server" value="kKZCoc8VdikktJSVXR9vzpQjfpQd0icnz6L460q6"/>
        -->

        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="pay" />
    
    </div>
    </form>

</body>
</html>
