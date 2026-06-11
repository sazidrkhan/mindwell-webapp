<%@ Page Title="User Management" Language="C#" MasterPageFile="~/Admin/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="MindWell.Admin.Users" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server"><%= RenderUsers() %></asp:Content>
