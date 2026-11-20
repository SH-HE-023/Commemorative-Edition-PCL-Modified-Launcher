' 组网联机（EasyTier）辅助模块
' 说明：只做纯逻辑（下载内核、启停进程、解析 easytier-cli 输出），不涉及界面。
Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Text.RegularExpressions

Module ModEasyTier

    Public Const CoreVersion As String = "2.6.4"
    Public Const CoreDownloadUrl As String = "https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-x86_64-v2.6.4.zip"
    ' 社区公益中继（可中转，延迟较低）；连不上可在界面里换成别的
    Public Const DefaultRelay As String = "tcp://easytier.weiai.org.cn:11010"

    Private _core As Process

    ''' <summary>内核存放目录</summary>
    Public ReadOnly Property EasyTierDir As String
        Get
            Return PathTemp & "EasyTier\"
        End Get
    End Property

    ''' <summary>内核可执行文件：优先用启动器里已经内置好的那一套，其次才是下载下来的标准命名</summary>
    Public ReadOnly Property CoreExe As String
        Get
            Dim Builtin As String = EasyTierDir & "联机模块.exe"
            If File.Exists(Builtin) Then Return Builtin
            Dim PclBuiltin As String = Paths.Base & "CEPCLML\EasyTier\联机模块.exe"
            If File.Exists(PclBuiltin) Then Return PclBuiltin
            Return EasyTierDir & "easytier-core.exe"
        End Get
    End Property

    Public ReadOnly Property CliExe As String
        Get
            Dim Builtin As String = EasyTierDir & "联机模块 CLI.exe"
            If File.Exists(Builtin) Then Return Builtin
            Dim PclBuiltin As String = Paths.Base & "CEPCLML\EasyTier\联机模块 CLI.exe"
            If File.Exists(PclBuiltin) Then Return PclBuiltin
            Return EasyTierDir & "easytier-cli.exe"
        End Get
    End Property

    ''' <summary>组网是否正在运行（进程句柄丢失时按进程名兜底判断）</summary>
    Public ReadOnly Property IsRunning As Boolean
        Get
            Try
                If _core IsNot Nothing AndAlso Not _core.HasExited Then Return True
            Catch
            End Try
            Try
                Dim Name As String = IO.Path.GetFileNameWithoutExtension(CoreExe)
                For Each P As Process In Process.GetProcessesByName(Name)
                    Try
                        If P.MainModule IsNot Nothing AndAlso P.MainModule.FileName.StartsWithF(IO.Path.GetDirectoryName(CoreExe)) Then Return True
                    Catch
                    End Try
                Next
            Catch
            End Try
            Return False
        End Get
    End Property

    ''' <summary>当前是哪种模式：tun（有虚拟网卡）/ no-tun（无网卡，仅端口转发）</summary>
    Public Property LastMode As String = ""

    ''' <summary>启动后附加的提示文字，界面可以显示出来</summary>
    Public Property LastWarning As String = ""

    ''' <summary>首次使用时从 GitHub 下载 EasyTier 内核并解压，已存在则直接返回。</summary>
    Public Sub EnsureCore()
        If File.Exists(CoreExe) AndAlso File.Exists(CliExe) Then Return
        Directory.CreateDirectory(EasyTierDir)
        Dim ZipPath As String = EasyTierDir & "easytier.zip"
        Dim TempDir As String = EasyTierDir & "unpack\"
        If File.Exists(ZipPath) Then File.Delete(ZipPath)
        Using Client As New Net.WebClient()
            Client.Headers.Add("User-Agent", "CEPCLMLLauncher/0.1")
            Client.DownloadFile(CoreDownloadUrl, ZipPath)
        End Using
        If Directory.Exists(TempDir) Then Directory.Delete(TempDir, True)
        Directory.CreateDirectory(TempDir)
        ZipFile.ExtractToDirectory(ZipPath, TempDir)
        '压缩包内会多套一层目录，把文件平铺出来
        For Each FilePath As String In Directory.GetFiles(TempDir, "*", SearchOption.AllDirectories)
            Dim Name As String = Path.GetFileName(FilePath)
            Dim Target As String = EasyTierDir & Name
            If File.Exists(Target) Then File.Delete(Target)
            File.Move(FilePath, Target)
        Next
        Directory.Delete(TempDir, True)
        File.Delete(ZipPath)
        If Not File.Exists(CoreExe) Then Throw New Exception("EasyTier 内核解压失败，请重试或检查网络")
    End Sub

    ''' <summary>
    ''' 启动组网。需要管理员权限创建虚拟网卡，因此会弹一次 UAC。
    ''' </summary>
    Public Sub StartNetwork(NetworkName As String, Secret As String, Relay As String)
        If String.IsNullOrWhiteSpace(NetworkName) Then Throw New Exception("请先填写网络名称")
        If NetworkName.Trim.Length < 4 Then Throw New Exception("网络名称至少 4 个字符，方便朋友输入")
        If String.IsNullOrWhiteSpace(Secret) Then Throw New Exception("请先填写网络密码")
        If Secret.Trim.Length < 4 Then Throw New Exception("网络密码至少 4 个字符")
        EnsureCore()
        StopNetwork()

        Dim Args As String = BuildArguments(NetworkName.Trim, Secret.Trim, Relay, True)
        LastMode = "tun"
        LastWarning = ""
        If Not TryLaunch(Args, True) Then
            '没有管理员权限时虚拟网卡建不起来，退回无网卡模式，至少还能靠端口映射连
            Logger.Warn("EasyTier 带虚拟网卡启动失败，改用无网卡模式")
            Args = BuildArguments(NetworkName.Trim, Secret.Trim, Relay, False)
            LastMode = "no-tun"
            LastWarning = "当前是无网卡模式（没有拿到管理员权限）。这种方式只有在游戏里用「对局域网开放」+ 端口转发时才连得上。"
            If Not TryLaunch(Args, False) Then
                Throw New Exception("EasyTier 内核启动后立刻退出了，请检查杀毒软件是否拦截，或手动以管理员身份运行一次。")
            End If
        End If
    End Sub

    Private Function BuildArguments(NetworkName As String, Secret As String, Relay As String, UseTun As Boolean) As String
        Dim Args As New StringBuilder()
        Args.Append($"--network-name ""{NetworkName}"" --network-secret ""{Secret}"" -d -l 11010 ")
        If Not String.IsNullOrWhiteSpace(Relay) Then Args.Append($"-p ""{Relay.Trim()}"" ")
        If Not UseTun Then Args.Append("--no-tun ")
        '游戏走端口映射，两条 loopback 都挂上
        Args.Append("--port-forward tcp://127.0.0.1:25565/10.114.114.114:25565 ")
        Args.Append("--port-forward udp://127.0.0.1:25565/10.114.114.114:25565 ")
        Return Args.ToString().Trim()
    End Function

    ''' <summary>启动内核并观察 2 秒，还活着就算成功</summary>
    Private Function TryLaunch(Args As String, NeedAdmin As Boolean) As Boolean
        Dim Info As New ProcessStartInfo(CoreExe)
        Info.WorkingDirectory = IO.Path.GetDirectoryName(CoreExe)
        Info.UseShellExecute = NeedAdmin
        If NeedAdmin Then Info.Verb = "runas"
        Info.WindowStyle = ProcessWindowStyle.Hidden
        Info.Arguments = Args
        Try
            _core = Process.Start(Info)
        Catch ex As Exception
            If NeedAdmin Then Return False '用户点了取消 UAC
            Throw New Exception("启动组网失败：" & ex.Message)
        End Try
        If _core Is Nothing Then Return False
        Threading.Thread.Sleep(2000)
        Try
            _core.Refresh()
            If _core.HasExited Then
                _core = Nothing
                Return False
            End If
        Catch
        End Try
        Return True
    End Function

    ''' <summary>停止组网，并清理可能残留的内核进程。</summary>
    Public Sub StopNetwork()
        Try
            If _core IsNot Nothing AndAlso Not _core.HasExited Then
                _core.Kill()
                _core.WaitForExit(500)
            End If
        Catch
        End Try
        _core = Nothing
        Try
            Dim CoreName As String = IO.Path.GetFileNameWithoutExtension(CoreExe)
            Dim CoreDir As String = IO.Path.GetDirectoryName(CoreExe)
            For Each P As Process In Process.GetProcessesByName(CoreName)
                Try
                    If P.MainModule IsNot Nothing AndAlso P.MainModule.FileName.StartsWithF(CoreDir) Then
                        P.Kill()
                    End If
                Catch
                End Try
            Next
        Catch
        End Try
    End Sub

    ''' <summary>读取 easytier-cli node 输出里的虚拟 IP，未连接时返回空字符串。</summary>
    Public Function GetVirtualIp() As String
        Dim Output As String = RunCli("node")
        If String.IsNullOrEmpty(Output) Then Return ""
        For Each Line As String In Output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If Not Line.Contains("Virtual IP") Then Continue For
            Dim Cells As String() = Line.Split("|"c)
            If Cells.Length >= 3 Then
                Dim Value As String = Cells(2).Trim()
                If Value <> "" Then Return Value
            End If
        Next
        Return ""
    End Function

    ''' <summary>读取已连接的节点列表（不含本机），用于显示“已连接 N 人”。</summary>
    Public Function GetPeers() As List(Of String)
        Dim Result As New List(Of String)
        Dim Output As String = RunCli("peer")
        If String.IsNullOrEmpty(Output) Then Return Result
        For Each Line As String In Output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If Not Line.StartsWithF("|") Then Continue For
            If Line.Contains("hostname") OrElse Line.Contains("---") Then Continue For
            Dim Cells As String() = Line.Split("|"c)
            If Cells.Length < 3 Then Continue For
            Dim Host As String = Cells(2).Trim()
            If Host = "" OrElse String.Equals(Host, "Local", StringComparison.OrdinalIgnoreCase) OrElse Host.Contains("Local") Then Continue For
            Result.Add(Host)
        Next
        Return Result
    End Function

    ''' <summary>执行 easytier-cli 并返回输出。</summary>
    Private Function RunCli(Command As String) As String
        If Not File.Exists(CliExe) Then Return ""
        Try
            Dim Info As New ProcessStartInfo(CliExe)
            Info.WorkingDirectory = EasyTierDir
            Info.UseShellExecute = False
            Info.RedirectStandardOutput = True
            Info.RedirectStandardError = True
            Info.CreateNoWindow = True
            Info.StandardOutputEncoding = Encoding.UTF8
            Info.Arguments = Command
            Using P As Process = Process.Start(Info)
                Dim Out As String = P.StandardOutput.ReadToEnd()
                If Not P.WaitForExit(6000) Then
                    Try
                        P.Kill()
                    Catch
                    End Try
                End If
                Return Out
            End Using
        Catch
            Return ""
        End Try
    End Function

End Module
