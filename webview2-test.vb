Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms

Public Class Form1
    ' ---- 浏览器控件 ----
    Private ReadOnly btnBack As New Button()
    Private ReadOnly btnForward As New Button()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly btnHome As New Button()
    Private ReadOnly txtAddress As New TextBox()
    Private ReadOnly cboEngine As New ComboBox()
    Private ReadOnly btnGo As New Button()
    Private ReadOnly webView As New WebView2()

    ' ---- 搜索引擎定义（名称 / 搜索地址模板 / 主页）----
    Private Class SearchEngine
        Public ReadOnly Name As String
        Public ReadOnly SearchUrl As String
        Public ReadOnly HomeUrl As String
        Public Sub New(n As String, s As String, h As String)
            Name = n
            SearchUrl = s
            HomeUrl = h
        End Sub
        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    Private ReadOnly engines As SearchEngine() = {
        New SearchEngine("百度", "https://www.baidu.com/s?wd=", "https://www.baidu.com/"),
        New SearchEngine("必应 Bing", "https://www.bing.com/search?q=", "https://www.bing.com/"),
        New SearchEngine("谷歌 Google", "https://www.google.com/search?q=", "https://www.google.com/"),
        New SearchEngine("搜狗", "https://www.sogou.com/web?query=", "https://www.sogou.com/"),
        New SearchEngine("DuckDuckGo", "https://duckduckgo.com/?q=", "https://duckduckgo.com/")
    }

    ' 地址栏占位提示文字
    Private Const AddressHint As String = "输入网址或搜索内容"

    Private loadStart As DateTime

    Public Sub New()
        InitializeComponent()

        Me.Size = New Size(1100, 750)
        Me.MinimumSize = New Size(700, 500)
        Me.Text = "WebViewTest"
        Me.StartPosition = FormStartPosition.CenterScreen

        ' ---------- 导航工具栏 ----------
        btnBack.Text = "后退"
        btnBack.Location = New Point(10, 10)
        btnBack.Size = New Size(70, 28)
        btnBack.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        AddHandler btnBack.Click, AddressOf BtnBack_Click
        Me.Controls.Add(btnBack)

        btnForward.Text = "前进"
        btnForward.Location = New Point(90, 10)
        btnForward.Size = New Size(70, 28)
        btnForward.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        AddHandler btnForward.Click, AddressOf BtnForward_Click
        Me.Controls.Add(btnForward)

        btnRefresh.Text = "刷新"
        btnRefresh.Location = New Point(170, 10)
        btnRefresh.Size = New Size(70, 28)
        btnRefresh.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
        Me.Controls.Add(btnRefresh)

        btnHome.Text = "主页"
        btnHome.Location = New Point(250, 10)
        btnHome.Size = New Size(70, 28)
        btnHome.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        AddHandler btnHome.Click, AddressOf BtnHome_Click
        Me.Controls.Add(btnHome)

        ' ---------- 地址 / 搜索栏 ----------
        txtAddress.Location = New Point(330, 10)
        txtAddress.Size = New Size(560, 28)
        txtAddress.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtAddress.Text = AddressHint
        txtAddress.ForeColor = Color.Gray
        AddHandler txtAddress.Enter, AddressOf TxtAddress_Enter
        AddHandler txtAddress.Leave, AddressOf TxtAddress_Leave
        AddHandler txtAddress.KeyDown, AddressOf TxtAddress_KeyDown
        Me.Controls.Add(txtAddress)

        ' ---------- 搜索引擎选择 ----------
        cboEngine.DropDownStyle = ComboBoxStyle.DropDownList
        cboEngine.Location = New Point(900, 10)
        cboEngine.Size = New Size(110, 28)
        cboEngine.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        For Each e As SearchEngine In engines
            cboEngine.Items.Add(e)
        Next
        cboEngine.SelectedIndex = 0   ' 默认百度
        Me.Controls.Add(cboEngine)

        ' ---------- 前往按钮 ----------
        btnGo.Text = "前往"
        btnGo.Location = New Point(1020, 10)
        btnGo.Size = New Size(70, 28)
        btnGo.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler btnGo.Click, AddressOf BtnGo_Click
        Me.Controls.Add(btnGo)

        ' ---------- 页面区域 ----------
        webView.Location = New Point(10, 45)
        webView.Size = New Size(1080, 695)
        webView.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler webView.NavigationStarting, AddressOf WebView_NavigationStarting
        AddHandler webView.NavigationCompleted, AddressOf WebView_NavigationCompleted
        AddHandler webView.CoreWebView2InitializationCompleted, AddressOf WebView_CoreWebView2InitializationCompleted
        Me.Controls.Add(webView)

        AddHandler Me.Shown, AddressOf Form_Shown
    End Sub

    Private Sub Form_Shown(sender As Object, e As EventArgs)
        LoadCoreAndHomeAsync()
    End Sub

    ' 异步初始化并打开主页
    Private Async Sub LoadCoreAndHomeAsync()
        Await webView.EnsureCoreWebView2Async()
        If webView.CoreWebView2 IsNot Nothing Then
            webView.CoreWebView2.Settings.AreDevToolsEnabled = True
            NavigateHome()
        End If
    End Sub

    ' 当前选中的搜索引擎
    Private Function CurrentEngine() As SearchEngine
        If cboEngine.SelectedIndex >= 0 Then
            Return CType(cboEngine.SelectedItem, SearchEngine)
        End If
        Return engines(0)
    End Function

    ' 打开主页
    Private Sub NavigateHome()
        If webView.CoreWebView2 IsNot Nothing Then
            webView.CoreWebView2.Navigate(CurrentEngine().HomeUrl)
        End If
    End Sub

    ' 前往 / 搜索入口（按钮与回车共用）
    Private Async Sub BtnGo_Click(sender As Object, e As EventArgs)
        Await NavigateOrSearch()
    End Sub

    Private Async Sub TxtAddress_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Await NavigateOrSearch()
        End If
    End Sub

    ' 统一处理“是网址就浏览，否则就搜索”
    Private Async Function NavigateOrSearch() As Task
        Dim input As String = txtAddress.Text.Trim()
        If String.IsNullOrWhiteSpace(input) OrElse input = AddressHint Then Return

        If webView.CoreWebView2 Is Nothing Then
            Await webView.EnsureCoreWebView2Async()
        End If
        If webView.CoreWebView2 Is Nothing Then Return

        Dim url As String = TryResolveUrl(input)
        If url IsNot Nothing Then
            txtAddress.Text = url
            txtAddress.ForeColor = Color.Black
            webView.CoreWebView2.Navigate(url)
        Else
            ' 不是网址，当作搜索词
            Dim query As String = Uri.EscapeDataString(input)
            webView.CoreWebView2.Navigate(CurrentEngine().SearchUrl & query)
            txtAddress.Text = input
            txtAddress.ForeColor = Color.Black
        End If
    End Function

    ' 尝试把输入解析成可访问网址；解析不出则返回 Nothing（按搜索处理）
    Private Function TryResolveUrl(input As String) As String
        Dim candidate As String = input
        If input.Contains("://", StringComparison.Ordinal) Then
            candidate = input
        ElseIf input.Contains(" ") OrElse Not LooksLikeDomain(input) Then
            Return Nothing
        Else
            candidate = "https://" & input
        End If

        Dim uri As Uri = Nothing
        If Uri.TryCreate(candidate, UriKind.Absolute, uri) Then
            If String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) OrElse
               String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) Then
                Return uri.ToString()
            End If
        End If
        Return Nothing
    End Function

    ' 简单判断是否像域名（无空格，且含“.”或为 localhost）
    Private Function LooksLikeDomain(input As String) As Boolean
        If input.Contains(" ") Then Return False
        If input.Contains(".") Then Return True
        If input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) Then Return True
        Return False
    End Function

    ' 地址栏占位提示：获得焦点时清空
    Private Sub TxtAddress_Enter(sender As Object, e As EventArgs)
        If txtAddress.Text = AddressHint Then
            txtAddress.Text = ""
            txtAddress.ForeColor = Color.Black
        End If
    End Sub

    ' 地址栏占位提示：失去焦点且为空时恢复
    Private Sub TxtAddress_Leave(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtAddress.Text) Then
            txtAddress.Text = AddressHint
            txtAddress.ForeColor = Color.Gray
        End If
    End Sub

    Private Sub BtnBack_Click(sender As Object, e As EventArgs)
        If webView.CanGoBack Then webView.GoBack()
    End Sub

    Private Sub BtnForward_Click(sender As Object, e As EventArgs)
        If webView.CanGoForward Then webView.GoForward()
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
        If webView.CoreWebView2 IsNot Nothing Then webView.Reload()
    End Sub

    Private Sub BtnHome_Click(sender As Object, e As EventArgs)
        NavigateHome()
    End Sub

    ' 开始加载：记录时间
    Private Sub WebView_NavigationStarting(sender As Object, e As CoreWebView2NavigationStartingEventArgs)
        loadStart = DateTime.Now
    End Sub

    ' 加载完成：更新标题与地址栏
    Private Sub WebView_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        Dim ms As Long = CInt((DateTime.Now - loadStart).TotalMilliseconds)
        Dim title As String = ""
        If webView.CoreWebView2 IsNot Nothing Then
            title = webView.CoreWebView2.DocumentTitle
        End If
        Me.Text = If(String.IsNullOrEmpty(title), $"WebViewTest - {ms} ms", $"WebViewTest - {title}")
        If webView.CoreWebView2 IsNot Nothing AndAlso Not String.IsNullOrEmpty(webView.Source.ToString()) Then
            txtAddress.Text = webView.Source.ToString()
            txtAddress.ForeColor = Color.Black
        End If
    End Sub

    ' 内核初始化完成
    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        ' 如需关闭右键菜单或设置用户代理等，可在这里配置
    End Sub
End Class
