<%@ Page Title="Startseite" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="Vr.LiBase.Client._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <asp:Panel DefaultButton="SearchButton" runat="server">
        <h2>
            <asp:Label ID="Label1" runat="server"><%=GetString("Search") %></asp:Label>
        </h2>
        <asp:Label ID="Label2" runat="server" Text="Search Term: "></asp:Label>
        <asp:TextBox ID="SearchTermTextfield" runat="server" Width="614px" Height="16px"></asp:TextBox>
        <br />
        <asp:Button ID="SearchButton" runat="server" Text="Search" OnClick="SearchButton_Click" />
    </asp:Panel>
    <br />
    <br />
    <asp:BulletedList ID="BulletedList1" runat="server">
    </asp:BulletedList>
    <br />
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
    </asp:UpdatePanel>
</asp:Content>
