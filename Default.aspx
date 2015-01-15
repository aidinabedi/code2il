<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Code2IL.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title>Code2IL - Convert your C# code to IL assembly instructions</title>
</head>
<body>
	<form id="Default" runat="server">
		<div>
			<asp:TextBox ID="Code" runat="server" Font-Names="Consolas" Font-Size="Small" Height="400px" TextMode="MultiLine" Width="100%" Wrap="False"></asp:TextBox>
		</div>
		<asp:Button ID="Convert" runat="server" Font-Bold="False" OnClick="Convert_Click" Text="Convert To IL" Width="135px" />
		&nbsp;
		<asp:DropDownList ID="Language" runat="server" Width="58px" Font-Size="Small" AutoPostBack="True" OnSelectedIndexChanged="Language_SelectedIndexChanged">
			<asp:ListItem>C#</asp:ListItem>
			<asp:ListItem>VB</asp:ListItem>
		</asp:DropDownList>
		&nbsp;
		<asp:DropDownList ID="CompilerVersion" runat="server" Width="58px" Font-Size="Small">
			<asp:ListItem>v4.0</asp:ListItem>
			<asp:ListItem>v3.5</asp:ListItem>
		</asp:DropDownList>
		&nbsp;
		<asp:CheckBox ID="Optimize" runat="server" Font-Size="Small" Text="Optimize" Checked="True" />
		&nbsp;
		<asp:CheckBox ID="Debug" runat="server" Font-Size="Small" Text="Debug" Checked="True" />
		&nbsp;
		<asp:CheckBox ID="IncludeHeaders" runat="server" Font-Size="Small" Text="Include Headers" />
		&nbsp;
		<asp:CheckBox ID="DetectControlStrucures" runat="server" Font-Size="Small" Text="Detect Control Strucures" Checked="True" />
		<br />
		<br />
		<asp:Label ID="Output" runat="server" Font-Names="Consolas" Font-Size="Small" Height="400px" Width="100%"></asp:Label>
	</form>
</body>
</html>
