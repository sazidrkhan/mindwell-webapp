<%@ Page Title="Forum Topic - MindWell" Language="C#" MasterPageFile="~/User/Site.Master" AutoEventWireup="true" CodeBehind="ForumTopic.aspx.cs" Inherits="MindWell.User.ForumTopic" ValidateRequest="false" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server"><%= RenderForumTopic() %></asp:Content>
