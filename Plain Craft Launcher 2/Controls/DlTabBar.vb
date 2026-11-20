Imports System.Windows.Media
Imports System.Windows.Controls
Imports System.Windows.Input

''' <summary>
''' 下载页的胶囊式分类标签栏。
''' </summary>
Public Class DlTabBar
    Inherits MyPageLeft

    Private ReadOnly Pills As New List(Of (SubID As FormMain.PageSubType, Pill As Border, Lab As TextBlock))

    Public Sub New()
        Height = 46
        VerticalAlignment = VerticalAlignment.Center
        Dim Host As New StackPanel With {
            .Orientation = Orientation.Horizontal,
            .VerticalAlignment = VerticalAlignment.Center,
            .Margin = New Thickness(4, 0, 0, 0)
        }
        Children.Add(Host)
        AddPill(Host, "原版游戏", FormMain.PageSubType.DownloadInstall)
        AddPill(Host, "Mod", FormMain.PageSubType.DownloadMod)
        AddPill(Host, "整合包", FormMain.PageSubType.DownloadPack)
        AddPill(Host, "数据包", FormMain.PageSubType.DownloadDataPack)
        AddPill(Host, "资源包", FormMain.PageSubType.DownloadResourcePack)
        AddPill(Host, "光影包", FormMain.PageSubType.DownloadShader)
    End Sub

    Private Sub AddPill(Host As StackPanel, Text As String, ID As FormMain.PageSubType)
        Dim Lab As New TextBlock With {
            .Text = Text, .FontSize = 13, .VerticalAlignment = VerticalAlignment.Center
        }
        Dim Pill As New Border With {
            .CornerRadius = New CornerRadius(13), .Height = 26, .Margin = New Thickness(4, 0, 4, 0),
            .Padding = New Thickness(16, 0, 16, 0), .Cursor = Cursors.Hand,
            .Background = New SolidColorBrush(Color.FromRgb(233, 238, 245))
        }
        Pill.Child = Lab
        AddHandler Pill.MouseLeftButtonUp, Sub(s, e) Go(ID)
        Host.Children.Add(Pill)
        Pills.Add((ID, Pill, Lab))
    End Sub

    ''' <summary>
    ''' 同步当前分类的高亮胶囊（未知子页则保持原状）。
    ''' </summary>
    Public Sub SyncActive(ID As FormMain.PageSubType)
        If Not Pills.Exists(Function(P) P.SubID = ID) Then Return
        Dim Accent As Brush = Nothing
        Try : Accent = CType(Application.Current.FindResource("ColorBrush4"), Brush) : Catch : End Try
        If Accent Is Nothing Then Accent = New SolidColorBrush(Color.FromArgb(255, 46, 111, 242))
        For Each P In Pills
            Dim Active As Boolean = P.SubID = ID
            If Active Then
                P.Pill.Background = Accent
                P.Pill.BorderThickness = New Thickness(1)
                P.Lab.Foreground = Brushes.White
                P.Lab.FontWeight = FontWeights.SemiBold
            Else
                P.Pill.Background = New SolidColorBrush(Color.FromRgb(233, 238, 245))
                P.Pill.BorderThickness = New Thickness(1)
                P.Lab.Foreground = New SolidColorBrush(Color.FromArgb(210, 58, 76, 98))
                P.Lab.FontWeight = FontWeights.Normal
            End If
        Next
    End Sub

    Private Sub Go(ID As FormMain.PageSubType)
        SyncActive(ID)
        FrmMain.PageChange(FormMain.PageType.Download, ID)
    End Sub

End Class
