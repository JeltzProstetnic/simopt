<%@ Page Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Submit.aspx.cs" Inherits="Vr.LiBase.Client.Submit" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <div>
        
        <asp:Label ID="Label1" runat="server" Text="Title: "></asp:Label>
        <asp:TextBox ID="TextBox1" runat="server" Width="614px" Height="16px"></asp:TextBox>
        
        <br />
        <asp:TextBox ID="TextBox2" runat="server" Height="269px" Width="500px"></asp:TextBox>
        <br />
        <asp:Button ID="Button1" runat="server" Text="Annotate" 
            onclick="Button1_Click" />
        
        <br />
        <br />
        <asp:CheckBoxList ID="CheckBoxList1" runat="server">
        </asp:CheckBoxList>
        <asp:Button ID="Button2" runat="server" Text="Submit" />
        <br />
        
    </div>
</asp:Content>
