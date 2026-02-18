<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Search.aspx.cs" Inherits="Vr.LiBase.Client.Search" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2>
        <asp:Label ID="Label1" runat="server"><%=GetString("Search") %></asp:Label>
    </h2>
    <asp:Label ID="Label2" runat="server" Text="Search Term: "></asp:Label>
    <asp:TextBox ID="TextBox1" runat="server" Width="614px" Height="16px"></asp:TextBox>
    <br />
    <asp:Button ID="Button1" runat="server" Text="Search" onclick="Button1_Click" />




    <br />
    <br />
    <asp:BulletedList ID="BulletedList1" runat="server">
    </asp:BulletedList>
    <br />




    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">

    </asp:UpdatePanel>
    </div>
    </form>
</body>
</html>
