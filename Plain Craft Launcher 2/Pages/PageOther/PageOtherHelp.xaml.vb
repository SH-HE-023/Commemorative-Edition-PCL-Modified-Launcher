Public Class PageOtherHelp
    Implements IRefreshable

#Region "初始化"

    '滚动条
    Private Sub PageOther_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub
    '初始化加载器信息
    Private Sub PageOther_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanBack, Nothing, HelpLoader, AddressOf HelpListLoad)
    End Sub

#End Region

    ''' <summary>
    ''' 将帮助列表对象实例化为主页 UI。
    ''' </summary>
    Private Sub HelpListLoad(Loader As LoaderTask(Of Integer, List(Of HelpEntry)))
        Try

            '初始化
            PanList.Children.Clear()
            PanBack.ScrollToHome()
            '只保留需要的分类：指南、Minecraft、启动器、个性化 全部隐藏，保留百科与帮助
            Dim HiddenTypes As String() = {"指南", "Minecraft", "启动器", "个性化"}
            Dim HelpItems = Loader.Output
            '获取全部分类
            Dim Types As New List(Of String)
            For Each Item As HelpEntry In HelpItems
                If BuildType = BuildTypes.Release AndAlso Not Item.ShowInPublic Then Continue For
                If BuildType <> BuildTypes.Release AndAlso Not Item.ShowInSnapshot Then Continue For
                For Each Type In Item.Types
                    If HiddenTypes.Contains(Type) Then Continue For
                    If Not Types.Contains(Type) Then Types.Add(Type)
                Next
            Next
            '将指南页面置顶
            If Types.Contains("指南") Then
                Types.Remove("指南")
                Types.Insert(0, "指南")
            End If
            '转化为 UI
            For Each Type As String In Types
                '确认所属该分类的项目
                Dim TypeItems As New List(Of HelpEntry)
                For Each Item In HelpItems
                    If BuildType = BuildTypes.Release AndAlso Not Item.ShowInPublic Then Continue For
                    If BuildType <> BuildTypes.Release AndAlso Not Item.ShowInSnapshot Then Continue For
                    If Item.Types.Contains(Type) Then TypeItems.Add(Item)
                Next
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Type, .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.SetContentBuilder(TypeItems, Function(Entry) Entry.ToListItem)
                If Type = "指南" Then
                    NewCard.BuildContent()
                Else
                    NewCard.IsSwapped = True
                End If
                PanList.Children.Add(NewCard)
            Next

            '在最前面插入「声明」卡片
            PanList.Children.Insert(0, BuildDeclareCard())

        Catch ex As Exception
            Logger.Error(ex, "加载帮助列表 UI 失败")
        End Try
    End Sub

    ''' <summary>
    ''' 构建帮助页置顶的「声明」卡片：免责声明、作者信息、所用开源项目及其原始链接。
    ''' </summary>
    Private Function BuildDeclareCard() As MyCard
        Dim Card As New MyCard With {.Title = "声明", .Margin = New Thickness(0, 0, 0, 15)}
        Dim Stack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 14)}
        Card.Children.Add(Stack)
        Card.SwapControl = Stack

        '上游署名与赞助链接（放在声明页首位）
        AddDeclareTitle(Stack, "上游项目与原作者")
        AddDeclareText(Stack, "Plain Craft Launcher 2（PCL）—— 本项目全部代码基础的来源，作者 龙腾猫跃（MelonYue）。本项目为第三方基于 PCL 独立进行的二次创作，与原作者无关，也不代表其立场或背书。")
        AddDeclareLink(Stack, "赞助原作者（爱发电）：https://afdian.com/a/LTCat", "https://afdian.com/a/LTCat")
        AddDeclareLink(Stack, "PCL 上游仓库：Meloong-Git/PCL", "https://github.com/Meloong-Git/PCL")

        Dim DeclareText As String =
            "本程序 PCL CEPCLML 修改版（内部标识 CEPCLML，Commemorative Edition PCL Modified Launcher）是基于 Plain Craft Launcher 2（PCL）二次修改的第三方衍生版本，" &
            "属于免费、开源的社区项目，与微软（Microsoft）、Mojang、PCL 原作者及其他任何公司、团队或项目均无任何关联，" &
            "不代表任何第三方的立场或背书，也不受其支持或认证。" &
            "你可以自由查看、修改、分发本项目的源代码，但请遵守上游许可《PCL 分发有限许可》与《PCL 存储库合理使用指南》（见仓库根目录 LICENCE）；" &
            "因使用本程序（含联机、开服、第三方组件）造成的任何数据丢失、账号风险或其他后果，由使用者自行承担。" &
            "Minecraft 游戏本体、皮肤、Mod、整合包等资源的著作权归 Mojang 及各原作者所有，本程序不提供、不分发任何盗版游戏内容，请勿用于商业用途或非法传播。"
        AddDeclareText(Stack, DeclareText)

        AddDeclareTitle(Stack, "作者")
        AddDeclareText(Stack, "作者：SH-HE-023（哔哩哔哩：彩CAN）")
        AddDeclareLink(Stack, "GitHub 项目主页：SH-HE-023/Commemorative-Edition-CEPCLML-Modified-Launcher",
                       "https://github.com/SH-HE-023/Commemorative-Edition-PCL-Modified-Launcher")

        AddDeclareTitle(Stack, "本项目使用的开源项目")
        AddDeclareText(Stack, "以下为本启动器在代码、组件或功能上直接使用的开源项目，尊重并感谢每一位原作者：")
        AddDeclareItem(Stack, "PCL2（Plain Craft Launcher 2）", "本项目的代码基础，作者 龙腾猫跃（MelonYue）", "https://github.com/Meloong-Git/PCL")
        AddDeclareItem(Stack, "MeloongCore", "PCL2 的配套核心库（随 PCL2 仓库开源）", "https://github.com/Meloong-Git/PCL")
        AddDeclareItem(Stack, "Newtonsoft.Json", "JSON 序列化，作者 James Newton-King", "https://github.com/JamesNK/Newtonsoft.Json")
        AddDeclareItem(Stack, "NAudio", "音频播放与音量控制，作者 Mark Heath", "https://github.com/naudio/NAudio")
        AddDeclareItem(Stack, "Ookii.Dialogs.Wpf", "系统文件/文件夹选择对话框，作者 Ookii", "https://github.com/augustoproiete/ookii-dialogs-wpf")
        AddDeclareItem(Stack, "Imazen.WebP / libwebp", "WebP 图片解码，作者 Imazen / Google", "https://github.com/imazen/libwebp-net")
        AddDeclareItem(Stack, "CacheCow", "HTTP 缓存，作者 Aliostad", "https://github.com/aliostad/CacheCow")
        AddDeclareItem(Stack, "ThrottleDebounce", "节流与防抖工具库", "https://github.com/throttle-debounce/ThrottleDebounce")
        AddDeclareItem(Stack, "ASP.NET Web API Client（System.Net.Http.Formatting）", "HTTP 客户端格式化，微软开源", "https://github.com/aspnet/AspNetWebStack")
        AddDeclareItem(Stack, "EasyTier", "组网联机的底层虚拟局域网实现", "https://github.com/EasyTier/EasyTier")
        AddDeclareItem(Stack, "Campfire", "独立中继的 Minecraft 联机工具，作者 Xocio", "https://github.com/Xocio/Campfire")
        AddDeclareItem(Stack, "e4mc", "让单人世界通过域名分享给好友联机", "https://github.com/e4mc-minecraft/e4mc")
        AddDeclareText(Stack, "另外：Radmin LAN 为免费（非开源）第三方软件，仅作为 IPv6 不通时的备选方案由本启动器调用，版权归其开发者所有。")

        Card.BuildContent()
        Return Card
    End Function

    Private Sub AddDeclareTitle(Parent As StackPanel, Text As String)
        Dim Block As New TextBlock With {
            .Text = Text, .FontSize = 14, .FontWeight = FontWeights.Bold,
            .Margin = New Thickness(0, 12, 0, 6), .TextWrapping = TextWrapping.Wrap
        }
        Block.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
        Parent.Children.Add(Block)
    End Sub

    Private Sub AddDeclareText(Parent As StackPanel, Text As String)
        Dim Block As New TextBlock With {
            .Text = Text, .FontSize = 13, .LineHeight = 21, .Margin = New Thickness(0, 0, 0, 4),
            .TextWrapping = TextWrapping.Wrap
        }
        Block.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray2")
        Parent.Children.Add(Block)
    End Sub

    Private Sub AddDeclareItem(Parent As StackPanel, Name As String, Desc As String, Url As String)
        AddDeclareText(Parent, "· " & Name & "　" & Desc)
        Dim Link As New MyTextButton With {
            .Text = Url, .FontSize = 12, .Margin = New Thickness(14, 0, 0, 6),
            .HorizontalAlignment = HorizontalAlignment.Left, .Padding = New Thickness(2, 1, 2, 1)
        }
        AddHandler Link.Click, Sub() OpenUrlSafe(Url)
        Parent.Children.Add(Link)
    End Sub

    Private Sub AddDeclareLink(Parent As StackPanel, Text As String, Url As String)
        Dim Link As New MyTextButton With {
            .Text = Text, .FontSize = 13, .Margin = New Thickness(0, 0, 0, 4),
            .HorizontalAlignment = HorizontalAlignment.Left, .Padding = New Thickness(2, 1, 2, 1)
        }
        AddHandler Link.Click, Sub() OpenUrlSafe(Url)
        Parent.Children.Add(Link)
    End Sub

    Private Sub OpenUrlSafe(Url As String)
        Try
            Process.Start(New ProcessStartInfo With {.FileName = Url, .UseShellExecute = True})
        Catch ex As Exception
            Logger.Warn(ex, "打开链接失败：" & Url)
        End Try
    End Sub

    ''' <summary>
    ''' 帮助项目的点击事件。
    ''' </summary>
    Public Shared Sub OnItemClick(Entry As HelpEntry)
        Try
            If Entry.IsEvent Then
                CustomEvent.Raise(Entry.EventType, Entry.EventData)
            Else
                EnterHelpPage(Entry)
            End If
        Catch ex As Exception
            Logger.Error(ex, "处理帮助项目点击时发生意外错误")
        End Try
    End Sub
    Public Shared Sub EnterHelpPage(Location As String)
        RunInThread(
        Sub()
            If Not HelpLoader.State = LoadState.Finished Then HelpLoader.WaitForExit(GetUuid)
            Dim Entry As New HelpEntry(Location)
            RunInUi(
            Sub()
                Dim FrmHelpDetail As New PageOtherHelpDetail
                If FrmHelpDetail.Init(Entry) Then
                    FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.HelpDetail, .Additional = {Entry, FrmHelpDetail}})
                Else
                    Logger.Warn("已取消进入帮助项目，这一般是由于 xaml 初始化失败，且用户在弹窗中手动放弃")
                End If
            End Sub)
        End Sub)
    End Sub
    Public Shared Sub EnterHelpPage(Entry As HelpEntry)
        RunInThread(
        Sub()
            If Not HelpLoader.State = LoadState.Finished Then HelpLoader.WaitForExit(GetUuid)
            RunInUi(
            Sub()
                Dim FrmHelpDetail As New PageOtherHelpDetail
                If FrmHelpDetail.Init(Entry) Then
                    FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.HelpDetail, .Additional = {Entry, FrmHelpDetail}})
                Else
                    Logger.Warn("已取消进入帮助项目，这一般是由于 xaml 初始化失败，且用户在弹窗中手动放弃")
                End If
            End Sub)
        End Sub)
    End Sub

    ''' <summary>
    ''' 搜索帮助。
    ''' </summary>
    Public Sub SearchRun() Handles SearchBox.TextChanged
        If String.IsNullOrWhiteSpace(SearchBox.Text) Then
            '隐藏
            AniStart({
                 AaOpacity(PanSearch, -PanSearch.Opacity, 100),
                 AaCode(
                 Sub()
                     PanSearch.Height = 0
                     PanSearch.Visibility = Visibility.Collapsed
                     PanList.Visibility = Visibility.Visible
                 End Sub,, True),
                 AaOpacity(PanList, 1 - PanList.Opacity, 150, 30)
            }, "FrmOtherHelp Search Switch")
        Else
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of HelpEntry))
            Dim HiddenTypes As String() = {"指南", "Minecraft", "启动器", "个性化"}
            For Each Entry As HelpEntry In HelpLoader.Output
                If Not Entry.ShowInSearch OrElse (BuildType = BuildTypes.Release AndAlso Not Entry.ShowInPublic) Then Continue For
                If Not Entry.ShowInSearch OrElse (BuildType <> BuildTypes.Release AndAlso Not Entry.ShowInSnapshot) Then Continue For
                If Entry.Types.Any(Function(T) HiddenTypes.Contains(T)) Then Continue For
                QueryList.Add(New SearchEntry(Of HelpEntry) With {
                    .Item = Entry,
                    .SearchSource = New List(Of SearchSource) From {
                        New SearchSource(Entry.Title, 1),
                        New SearchSource(Entry.Desc, 0.5),
                        New SearchSource(Entry.Search, 1.5)
                    }
                })
                'New SearchSource(If(Entry.IsEvent, If(Entry.EventData, ""), Entry.XamlContent), 0.2)
            Next
            '进行搜索，构造列表
            Dim SearchResult = Search(QueryList, SearchBox.Text, MaxBlurCount:=5, MinBlurSimilarity:=0.08)
            PanSearchList.Children.Clear()
            If Not SearchResult.Any() Then
                PanSearch.Title = "无搜索结果"
                PanSearchList.Visibility = Visibility.Collapsed
            Else
                PanSearch.Title = "搜索结果"
                For Each Result In SearchResult
                    Dim Item = Result.Item.ToListItem
                    If ModeDebug Then Item.Info = If(Result.AbsoluteRight, "完全匹配，", "") & "相似度：" & Math.Round(Result.Similarity, 3) & "，" & Item.Info
                    PanSearchList.Children.Add(Item)
                Next
                PanSearchList.Visibility = Visibility.Visible
            End If
            '显示
            AniStart({
                 AaOpacity(PanList, -PanList.Opacity, 100),
                 AaCode(
                 Sub()
                     PanList.Visibility = Visibility.Collapsed
                     PanSearch.Visibility = Visibility.Visible
                     PanSearch.TriggerForceResize()
                 End Sub,, True),
                 AaOpacity(PanSearch, 1 - PanSearch.Opacity, 150, 30)
            }, "FrmOtherHelp Search Switch")
        End If
    End Sub

    Public Sub Refresh() Implements IRefreshable.Refresh
        PageOtherLeft.RefreshHelp()
    End Sub
End Class
