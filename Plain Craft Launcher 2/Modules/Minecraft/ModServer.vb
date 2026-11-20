' 开服 与 e4mc 公网联机 辅助模块
' 说明：本模块只做纯逻辑（下载、写文件、读日志），不涉及界面。
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json.Linq

Module ModServer

    ''' <summary>
    ''' 取该实例对应的 Minecraft 版本号。Fabric / Forge 等安装出来的文件夹名不是游戏版本号，
    ''' 需要从版本 JSON 里读 inheritsFrom（Fabric/Quilt）或 id。
    ''' </summary>
    Public Function GameVersionOf(Instance As McInstance) As String
        If Instance Is Nothing Then Return ""
        Dim Json As JObject = Instance.JsonObject
        If Json IsNot Nothing Then
            If Json("inheritsFrom") IsNot Nothing Then Return Json("inheritsFrom").ToString()
            If Json("id") IsNot Nothing Then Return Json("id").ToString()
        End If
        Return Instance.Name
    End Function

    ''' <summary>
    ''' 该实例是否属于可以装 e4mc 的版本（带加载器，能读 mods 文件夹）。
    ''' </summary>
    Public Function CanUseE4mc(Instance As McInstance) As Boolean
        If Instance Is Nothing Then Return False
        If Not Instance.Modable Then Return False
        Select Case Instance.State
            Case McInstance.McInstanceState.Fabric
                Return True
            Case McInstance.McInstanceState.Forge
                Return True
            Case McInstance.McInstanceState.NeoForge
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' 为指定实例生成一个官方服务端目录：server.jar + eula.txt + server.properties + start.bat。
    ''' 返回服务端目录路径（以 \ 结尾）。
    ''' </summary>
    Public Function PrepareServer(Instance As McInstance, Port As Integer, Motd As String, MaxPlayers As Integer) As String
        If Instance Is Nothing Then Throw New Exception("没有选中任何版本")
        If Instance.JsonObject Is Nothing Then Throw New Exception("版本信息尚未加载，请重新进入版本页后再试")

        Dim Downloads As JObject = TryCast(Instance.JsonObject("downloads"), JObject)
        Dim ServerInfo As JObject = If(Downloads Is Nothing, Nothing, TryCast(Downloads("server"), JObject))
        If ServerInfo Is Nothing Then Throw New Exception($"版本 {GameVersionOf(Instance)} 没有官方服务端文件，无法一键开服")

        Dim Url As String = ServerInfo("url").ToString()
        Dim Dir As String = Instance.PathVersion & "server\"
        Directory.CreateDirectory(Dir)

        ' 服务端核心
        Dim Jar As String = Dir & "server.jar"
        If Not File.Exists(Jar) Then
            If Not Uri.IsWellFormedUriString(Url, UriKind.Absolute) Then
                Throw New Exception("服务端下载地址异常：" & Url)
            End If
            If File.Exists(Jar & ".part") Then File.Delete(Jar & ".part")
            Using Client As New WebClient()
                Client.Headers.Add("User-Agent", "BlockBoxLauncher/0.1")
                Client.DownloadFile(Url, Jar & ".part")
            End Using
            If File.Exists(Jar) Then File.Delete(Jar)
            File.Move(Jar & ".part", Jar)
        End If

        ' EULA
        File.WriteAllText(Dir & "eula.txt", "eula=true", Encoding.UTF8)

        ' 配置
        Dim Sb As New StringBuilder()
        Sb.AppendLine("#Minecraft server properties")
        Sb.AppendLine("server-port=" & Port.ToString())
        Sb.AppendLine("server-ip=")
        Sb.AppendLine("motd=" & If(String.IsNullOrWhiteSpace(Motd), "Minecraft Server", Motd))
        Sb.AppendLine("max-players=" & Math.Max(1, MaxPlayers).ToString())
        Sb.AppendLine("online-mode=false")
        Sb.AppendLine("enable-rcon=false")
        Sb.AppendLine("allow-flight=true")
        Sb.AppendLine("spawn-protection=0")
        Sb.AppendLine("view-distance=10")
        File.WriteAllText(Dir & "server.properties", Sb.ToString(), Encoding.UTF8)

        ' 启动脚本（内存按需改）
        Dim Bat As New StringBuilder()
        Bat.AppendLine("@echo off")
        Bat.AppendLine("chcp 65001 >nul")
        Bat.AppendLine("cd /d ""%~dp0""")
        Bat.AppendLine("if not exist ""%JAVA%"" set JAVA=javaw")
        Bat.AppendLine("start """" ""%JAVA%"" -Xms2G -Xmx4G -jar server.jar nogui")
        File.WriteAllText(Dir & "start.bat", Bat.ToString(), Encoding.Default)

        Return Dir
    End Function

    ''' <summary>
    ''' 从 Modrinth 安装与当前版本匹配的 e4mc 到该实例的 mods 目录，返回文件名。
    ''' e4mc 的作用：装进客户端后，单人游戏会自动拿到一个公网地址，朋友直接加这个地址即可进来。
    ''' </summary>
    Public Function InstallE4mc(Instance As McInstance) As String
        If Not CanUseE4mc(Instance) Then Throw New Exception("该版本不是可安装 Mod 的版本，请先安装一个 Fabric / Forge / NeoForge 版本")

        Dim GameVer As String = GameVersionOf(Instance)
        Dim WantLoaders As New List(Of String)
        Select Case Instance.State
            Case McInstance.McInstanceState.Fabric
                WantLoaders.Add("fabric")
            Case McInstance.McInstanceState.Forge
                WantLoaders.Add("forge")
            Case McInstance.McInstanceState.NeoForge
                WantLoaders.Add("neoforge")
        End Select

        Dim Raw As JToken = FetchJson("https://api.modrinth.com/v2/project/e4mc/version")
        Dim Versions As JArray = TryCast(Raw, JArray)
        If Versions Is Nothing Then Throw New Exception("e4mc 版本列表读取失败")

        Dim Target As JObject = Nothing
        For Each Item As JObject In Versions
            Dim Loaders As JArray = TryCast(Item("loaders"), JArray)
            Dim Games As JArray = TryCast(Item("game_versions"), JArray)
            If Loaders Is Nothing OrElse Games Is Nothing Then Continue For
            ' 加载器匹配
            Dim LoaderOk As Boolean = False
            For Each Token As JToken In Loaders
                If WantLoaders.Contains(Token.ToString()) Then LoaderOk = True
            Next
            If Not LoaderOk Then Continue For
            ' 游戏版本匹配
            Dim GameOk As Boolean = False
            For Each Token As JToken In Games
                If Token.ToString() = GameVer Then GameOk = True
            Next
            If Not GameOk Then Continue For
            Target = Item
            Exit For
        Next

        If Target Is Nothing Then Throw New Exception($"e4mc 没有支持 {GameVer} 的版本")

        Dim Files As JArray = TryCast(Target("files"), JArray)
        If Files Is Nothing Then Throw New Exception("e4mc 下载文件信息缺失")
        Dim Primary As JObject = Nothing
        For Each Item As JObject In Files
            If Item("primary") IsNot Nothing AndAlso Item("primary").ToObject(Of Boolean) Then Primary = Item
        Next
        If Primary Is Nothing AndAlso Files.Count > 0 Then Primary = TryCast(Files(0), JObject)
        If Primary Is Nothing Then Throw New Exception("e4mc 下载文件信息缺失")

        Dim Url As String = Primary("url").ToString()
        Dim FileName As String = Primary("filename").ToString()

        Dim ModsDir As String = Instance.PathIndie & "mods\"
        Directory.CreateDirectory(ModsDir)
        Dim SavePath As String = ModsDir & FileName
        If Not File.Exists(SavePath) Then
            Using Client As New WebClient()
                Client.Headers.Add("User-Agent", "BlockBoxLauncher/0.1")
                Client.DownloadFile(Url, SavePath)
            End Using
        End If

        Return FileName
    End Function

    ''' <summary>
    ''' 从实例日志里读取 e4mc 分到的公网地址（形如 xxxxx.e4mc.link）。
    ''' 没有找到时返回空字符串。
    ''' </summary>
    Public Function ReadE4mcAddress(Instance As McInstance) As String
        If Instance Is Nothing Then Return ""
        Dim LogPath As String = Instance.PathIndie & "logs\latest.log"
        If Not File.Exists(LogPath) Then Return ""
        Dim Content As String
        Using Reader As New StreamReader(New FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8)
            Content = Reader.ReadToEnd()
        End Using
        Dim MatchResult As Match = Regex.Match(Content, "([A-Za-z0-9][A-Za-z0-9-]*\.e4mc\.link(?::\d+)?)")
        If MatchResult.Success Then Return MatchResult.Groups(1).Value
        Return ""
    End Function

    ''' <summary>
    ''' 读取 e4mc 提供的分享地址（部分版本会写成 https://xxxx.e4mc.link 形式）。
    ''' </summary>
    Public Function ReadE4mcShareUrl(Instance As McInstance) As String
        Dim Address As String = ReadE4mcAddress(Instance)
        If String.IsNullOrEmpty(Address) Then Return ""
        Return "https://" & Address.Replace(":25565", "")
    End Function

    Private Function FetchJson(Url As String) As JToken
        Using Client As New WebClient()
            Client.Headers.Add("User-Agent", "BlockBoxLauncher/0.1")
            Client.Encoding = Encoding.UTF8
            Return JToken.Parse(Client.DownloadString(Url))
        End Using
    End Function

End Module
