<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="keygen.aspx.cs" Inherits="caas.keygen" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        Click the button to generate a key pair.<br />
        The public key is in the file c:\CCP\RSAKeys\pubkey.xml; the private key is in 
        the file c:\CCP\RSAKeys\prikey.xml.<br />
        <asp:Button ID="Button1" runat="server" onclick="Button1_Click" 
            Text="Generate" />
    
    </div>
    </form>
</body>
</html>
