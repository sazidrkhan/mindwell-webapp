<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Admin/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="MindWell.Admin.Dashboard" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server"><%= RenderDashboard() %></asp:Content>
