Class MainWindow
    Private Declare Function OpenProcess Lib "kernel32" (ByVal dwDesiredAccess As Integer, ByVal bInheritHandle As Integer, ByVal dwProcessId As Integer) As Integer
    Private Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Integer) As Integer
    Private Const SYNCHRONIZE = &H100000
    Private Const STANDARD_RIGHTS_REQUIRED = &HF0000
    Private Const PROCESS_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED Or SYNCHRONIZE Or &HFFF)
    Private Declare Function NtSuspendProcess Lib "ntdll.dll" (ByVal hProc As Integer) As Integer
    Private Declare Function NtResumeProcess Lib "ntdll.dll" (ByVal hProc As Integer) As Integer
    Private Declare Function TerminateProcess Lib "kernel32" (ByVal hProcess As Integer, ByVal uExitCode As Integer) As Integer
    Private hProcess As Integer
    ReadOnly ProcessVersion = Application.ResourceAssembly.GetName().Version.ToString()
    ReadOnly ST As New SuspendTrue()
    ReadOnly SF As New SuspendFalse()
    ReadOnly RT As New ResumeTrue()
    ReadOnly RF As New ResumeFalse()

    Private Sub cmdSuspend_Click(sender As Object, e As RoutedEventArgs) Handles cmdSuspend.Click
        Suspend_Stop()
    End Sub

    Private Sub cmdResume_Click(sender As Object, e As RoutedEventArgs) Handles cmdResume.Click
        Resume_Pass()
    End Sub

    '用于获取程序PID
    Private Sub GetPid_Click(sender As Object, e As RoutedEventArgs) Handles GetPid.Click
        Pids1.Text = ""
        Pids2.Text = ""
        If (JiYuKill.IsChecked) Then
            JiYu()
        ElseIf (RuiJieKill.IsChecked) Then
            RuiJie()
        ElseIf (RedSpiderKill.IsChecked) Then
            RedSpider()
        ElseIf (OsEasyKill.IsChecked) Then
            OsEasy()
        End If

    End Sub

    '使用获取到的程序PID进行挂起操作
    Public Sub Suspend_Stop()
        If OsEasyKill.IsChecked Then '判断 成功则循环2遍查找Text内PID进行挂起操作
            Dim a = 0
            If IsNumeric(Pids1.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids1.Text))
                If hProcess <> 0 Then
                    NtSuspendProcess(hProcess)
                    a = +1
                End If
            End If
            If IsNumeric(Pids2.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids2.Text))
                If hProcess <> 0 Then
                    NtSuspendProcess(hProcess)
                    If a = 1 Then
                        ST.ShowAsync()
                    Else
                        SF.ShowAsync()
                    End If
                End If
            End If

        Else '失败则一遍
            If IsNumeric(Pids1.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids1.Text))
                If hProcess <> 0 Then
                    NtSuspendProcess(hProcess)
                End If
                ST.ShowAsync()
            Else
                SF.ShowAsync()
            End If
        End If
        CloseHandle(hProcess)
    End Sub

    '将挂起线程的对应PID进程解锁继续运行
    Public Sub Resume_Pass()
        If OsEasyKill.IsChecked Then '判断 成功则循环2遍查找Text内PID进行恢复操作
            Dim a = 0
            If IsNumeric(Pids1.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids1.Text))
                If hProcess <> 0 Then
                    NtResumeProcess(hProcess)
                    a = +1
                End If
            End If
            If IsNumeric(Pids2.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids2.Text))
                If hProcess <> 0 Then
                    NtResumeProcess(hProcess)
                    If a = 1 Then
                        RT.ShowAsync()
                    Else
                        RF.ShowAsync()
                    End If
                End If
            End If
        Else
            '失败则一遍
            If IsNumeric(Pids1.Text) Then
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, False, CLng(Pids1.Text))
                If hProcess <> 0 Then
                    NtResumeProcess(hProcess)
                End If
                RT.ShowAsync()
            Else
                RF.ShowAsync()
            End If
        End If
        CloseHandle(hProcess)
    End Sub
    Public Sub JiYu() '极域电子教室 任务管理器无法结束进程 有保护 但是使用ProcessExplorer可直接结束 (版本为 极域电子教室V6.0 2016豪华版 StudentMain.exe)
        Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='StudentMain.exe'")
        For Each process As ManagementObject In searcher.Get()
            Pids1.Text = process("Handle")
        Next
        If Pids1.Text = "" Then
            Pids1.Text = "Null"
        End If

    End Sub


    Public Sub RuiJie()  '锐捷云课堂 CMService为附加程序可以忽视作用为主程序Kill后重新启动(猜测) CMLauncher.exe or ClassManagerApp.exe
        Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='CMLauncher.exe'")
        For Each process As ManagementObject In searcher.Get()
            Pids1.Text = process("Handle")
        Next
        If Pids1.Text = "" Then
            Dim searcher1 As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='ClassManagerApp.exe'")
            For Each process As ManagementObject In searcher1.Get()
                Pids1.Text = process("Handle")
            Next
        End If
        If Pids1.Text = "" Then
            Pids1.Text = "Null"
        End If
    End Sub
    Public Sub RedSpider() '红蜘蛛 父程序为checkrs.exe权限为NT级 目前未知 (版本为红蜘蛛V6.2.1160 REDAgent.exe)
        Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='REDAgent.exe'")
        For Each process As ManagementObject In searcher.Get()
            Pids1.Text = process("Handle")
        Next
        If Pids1.Text = "" Then
            Pids1.Text = "Null"
        End If
    End Sub
    Public Sub OsEasy() '这个玩意我是想不通为什么教师端和学生端写一起 PM发现该程序有2个附属进程与教师机互连 故全部挂起 (版本为噢易多媒体V10.8.1.4221 Student.exe And MultiClient.exe)
        Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='Student.exe'")
        For Each process As ManagementObject In searcher.Get()
            Pids1.Text = process("Handle")
        Next
        If Pids1.Text = "" Then
            Pids1.Text = "Null"
        End If
        Dim searcher1 As New ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name='MultiClient.exe'")
        For Each process As ManagementObject In searcher1.Get()
            Pids2.Text = process("Handle")
        Next
        If Pids2.Text = "" Then
            Pids2.Text = "Null"
        End If
    End Sub

    Private Sub JiYuKill_Checked(sender As Object, e As RoutedEventArgs) Handles JiYuKill.Checked
        Pids2.IsReadOnly = True
    End Sub

    Private Sub RuiJieKill_Checked(sender As Object, e As RoutedEventArgs) Handles RuiJieKill.Checked
        Pids2.IsReadOnly = True
    End Sub

    Private Sub RedSpiderKill_Checked(sender As Object, e As RoutedEventArgs) Handles RedSpiderKill.Checked
        Pids2.IsReadOnly = True
    End Sub

    Private Sub OsEasyKill_Checked(sender As Object, e As RoutedEventArgs) Handles OsEasyKill.Checked
        Pids2.IsReadOnly = False
    End Sub

    Private Sub HKS_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles HKS.MouseDown
        Dim Dialog As New AboutDialog()
        Dialog.ShowAsync()
    End Sub

    Private Sub Web_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles Web.MouseDoubleClick
        Process.Start("https://fcs.hksstudio.work")
    End Sub

    Private Sub Directions_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Directions.MouseDown
        Dim Dialog As New ManualDialog()
        Dialog.ShowAsync()
    End Sub

    Private Sub Window_T_Loaded(sender As Object, e As RoutedEventArgs) Handles Window_T.Loaded
        Window_T.Title = "Fucking Classroom System Ver" + ProcessVersion + "_Alpha"

        Tips()

        '自动识别控制端
        Dim ProcessJiYu = Process.GetProcessesByName("StudentMain")
        Dim ProcessRuiJie = Process.GetProcessesByName("CMLauncher")
        Dim ProcessRuiJie2 = Process.GetProcessesByName("ClassManagerApp")
        Dim ProcessRedSpider = Process.GetProcessesByName("REDAgent")
        Dim ProcessOsEasy = Process.GetProcessesByName("Student")
        Dim Test As Integer = 0
        '检测到程序运行 (单循环判断)
        Select Case ProcessJiYu.Count
            Case 1
                JiYuKill.IsChecked = True
                Test += 1
            Case Else
                Select Case ProcessRuiJie.Count Or ProcessRuiJie2.Count
                    Case 1
                        RuiJieKill.IsChecked = True
                    Case Else
                        Select Case ProcessRedSpider.Count
                            Case 1
                                RedSpiderKill.IsChecked = True
                            Case Else
                                Select Case ProcessOsEasy.Count
                                    Case 1
                                        OsEasyKill.IsChecked = True
                                    Case Else
                                        Tip.Content = "没有找到对应被控端"
                                End Select
                        End Select
                End Select
        End Select

        If Test = 1 Then
            If ProcessRuiJie.Count = 1 Or ProcessRuiJie2.Count = 1 Then
                RuiJieKill.IsChecked = True
                Test += 1
            End If
            If ProcessRedSpider.Count = 1 Then
                RedSpiderKill.IsChecked = True
                Test += 1
            End If
            If ProcessOsEasy.Count = 1 Then
                OsEasyKill.IsChecked = True
                Test += 1
            End If
            If Test > 1 Then
                Tip.Content = "检测到" + Test.ToString + "个被控端 请手动选择"
            End If
        End If

        '都说select更容易看 .... 我怎么感觉If更容易的 可能是我要查找的条件不够多吧
        'If ProcessJiYu.Count = 1 Then
        '    JiYuKill.IsChecked = True
        'ElseIf ProcessRuiJie.Count = 1 Or ProcessRuiJie2.Count = 1 Then
        '    RuiJieKill.IsChecked = True
        'ElseIf ProcessRedSpider.Count = 1 Then
        '    RedSpiderKill.IsChecked = True
        'ElseIf ProcessOsEasy.Count = 1 Then
        '    OsEasyKill.IsChecked = True
        'End If

        '添加启动参数自动选择并获取PID
        For Each seg As String In Environment.GetCommandLineArgs()
            Select Case seg.ToLower
        '添加AutoSleep后自动挂起并结束程序
                Case "-autosleep"
                    If (JiYuKill.IsChecked) Then
                        JiYu()
                        Suspend_Stop()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (RuiJieKill.IsChecked) Then
                        RuiJie()
                        Suspend_Stop()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (RedSpiderKill.IsChecked) Then
                        RedSpider()
                        Suspend_Stop()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (OsEasyKill.IsChecked) Then
                        OsEasy()
                        Suspend_Stop()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    End If
        '添加AutoUnlock后自动挂起并结束程序
                Case "-autounlock"
                    If (JiYuKill.IsChecked) Then
                        JiYu()
                        Resume_Pass()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (RuiJieKill.IsChecked) Then
                        RuiJie()
                        Resume_Pass()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (RedSpiderKill.IsChecked) Then
                        RedSpider()
                        Resume_Pass()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    ElseIf (OsEasyKill.IsChecked) Then
                        OsEasy()
                        Resume_Pass()
                        System.Threading.Thread.Sleep(100)
                        Me.Close()
                    End If
                Case "-jiyu"
                    JiYu()
                    Suspend_Stop()
                Case "-ruijie"
                    RuiJie()
                    Suspend_Stop()
                Case "-redspider"
                    RedSpider()
                    Suspend_Stop()
                Case "-oseasy"
                    OsEasy()
                    Suspend_Stop()
            End Select
        Next

    End Sub
    Public Sub Tips()
        Dim TipText() As String =
        {"（；´д｀）ゞ",
        "╰(*°▽°*)╯",
        "￣へ￣",
        "╥﹏╥...",
        "（＾∀＾●）ﾉｼ",
        "(￣ε(#￣)☆╰╮o(￣皿￣///)",
        "o(≧口≦)o",
        "(～﹃～)~zZ"}
        Randomize()
        For i As Integer = 0 To Int(8 * Rnd())
            Tip.Content = TipText(i)
        Next
    End Sub

    Private Sub Tip_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Tip.MouseDown
        Tips()
    End Sub

End Class
