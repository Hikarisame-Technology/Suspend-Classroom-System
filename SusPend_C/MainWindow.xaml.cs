using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SusPend_C
{
    public partial class MainWindow : Window
    {
        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess(IntPtr processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int PROCESS_SUSPEND_RESUME = 0x0800;
        private const int PROCESS_ALL_ACCESS = 0x0001;

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;

        private System.Windows.Forms.NotifyIcon notifyIcon;

        const int WM_TIMER = 0x0113;
        const int TIMER_ID = 1;
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            RefreshProcesses_ListView();
            this.Title = "Fucking Classroom System Ver" + Application.ResourceAssembly.GetName().Version.ToString().ToString();
            NotifyIcon_Dis();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            BringWindowToTop(new WindowInteropHelper(this).Handle);
        }

        private void BringWindowToTop(IntPtr hWnd)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowPos(hWnd, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

        }

        private void TopCheck_Checked(object sender, RoutedEventArgs e)
        {
            //Topmost = true;
            //WindowInteropHelper helper = new WindowInteropHelper(this);
            //SetWindowPos(helper.Handle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            timer.Start();
        }

        private void TopCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            //Topmost = false;
            //WindowInteropHelper helper = new WindowInteropHelper(this);
            //SetWindowPos(helper.Handle, new IntPtr(HWND_NOTOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private List<ProcessItem> GetProcessList()
        {
            // 获取正在运行的程序列表
            List<ProcessItem> processList = new List<ProcessItem>();
            string[] processNames = new string[] { "StudentMain", "CMLauncher", "CMService", "ClassManagerApp", "REDAgent", "Student", "MultiClient", "Smonitor", "EnigmaVBUnpacker" };//EnigmaVBUnpacker为测试用例
            foreach (string processName in processNames)
            {
                Process[] processes = Process.GetProcessesByName(processName);
                int count = 1;
                foreach (Process process in processes)
                {
                    string nameWithCount = processName + " " + count;
                    processList.Add(new ProcessItem(nameWithCount, false));
                    count++;
                }
            }
            return processList;
        }

        private void RefreshProcesses()
        {
            // 刷新进程
            foreach (ProcessItem processItem in processListView.Items)
            {
                string baseProcessName = processItem.Name.Split(' ')[0];
                Process[] processes = Process.GetProcessesByName(baseProcessName);
                bool isSuspended = false;
                foreach (Process process in processes)
                {
                    if (process.ProcessName == processItem.Name.Split(' ')[0] && process.Threads[0].ThreadState == ThreadState.Wait && process.Threads[0].WaitReason == ThreadWaitReason.Suspended)
                    {
                        isSuspended = true;
                        break;
                    }
                }
                processItem.State = isSuspended ? "已挂起" : "正在运行";
            }
        }

        private void RefreshProcesses_ListView()
        {
            processListView.ItemsSource = GetProcessList();
            RefreshProcesses();
        }


        private void SuspendProcesses_Click(object sender, RoutedEventArgs e)
        {
            // 挂起程序
            foreach (ProcessItem processItem in processListView.Items)
            {
                if (processItem.State == "已挂起")
                {
                    continue;
                }
                Process[] processes = Process.GetProcessesByName(processItem.Name.Split(' ')[0]);
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        IntPtr processHandle = OpenProcess(PROCESS_SUSPEND_RESUME, false, process.Id);
                        if (processHandle != IntPtr.Zero)
                        {
                            int result = NtSuspendProcess(processHandle);
                            if (result == 0)
                            {
                                processItem.State = "已挂起";
                            }
                            else
                            {
                                MessageBox.Show(processItem.Name + " 进程挂起失败：" + result.ToString());
                            }
                            CloseHandle(processHandle);
                        }
                        else
                        {
                            MessageBox.Show("打开 " + processItem.Name + " 进程失败。");
                        }
                    }
                }
            }
            RefreshProcesses_ListView();
        }
        private void ResumeProcesses_Click(object sender, RoutedEventArgs e)
        {
            // 恢复运行
            foreach (ProcessItem processItem in processListView.Items)
            {
                Process[] processes = Process.GetProcessesByName(processItem.Name.Split(' ')[0]);
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        IntPtr processHandle = OpenProcess(PROCESS_SUSPEND_RESUME, false, process.Id);
                        if (processHandle != IntPtr.Zero)
                        {
                            int result = NtResumeProcess(processHandle);
                            if (result == 0)
                            {
                                processItem.State = "正在运行";
                            }
                            else
                            {
                                MessageBox.Show(processItem.Name + " 进程恢复运行失败：" + result.ToString());
                            }
                            CloseHandle(processHandle);
                        }
                        else
                        {
                            MessageBox.Show("打开 " + processItem.Name + " 进程失败。");
                        }
                    }
                }
            }
            RefreshProcesses_ListView();
        }

        private void Network_Click(object sender, RoutedEventArgs e)
        {
            // 断网
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PNPDeviceID LIKE 'PCI%'");
                ManagementObjectCollection adapterCollection = searcher.Get();

                foreach (ManagementObject adapter in adapterCollection)
                {
                    if (adapter.GetPropertyValue("NetConnectionStatus") != null )
                    {
                        if (adapter.GetPropertyValue("NetConnectionStatus").ToString() == "2") // 2为有链接的网络状态
                        {
                            adapter.InvokeMethod("Disable", null); // 停用适配器
                            Console.WriteLine("Adapter disabled.");
                            Network.Content = "网络恢复";
                        }
                        else if(adapter.GetPropertyValue("NetConnectionStatus").ToString() == "0") // 0为禁用
                        {
                            adapter.InvokeMethod("Enable", null); // 启用适配器
                            Console.WriteLine("Adapter enabled.");
                            Network.Content = "网络禁用";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("禁用/启用网络出现错误" + ex.Message);
            }
        }

        private void Kill_Click(object sender, RoutedEventArgs e)
        {
            // 杀死程序
            foreach (ProcessItem processItem in processListView.Items)
            {
                Process[] processes = Process.GetProcessesByName(processItem.Name.Split(' ')[0]);
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
                        if (processHandle != IntPtr.Zero)
                        {
                            if (TerminateProcess(processHandle, 0))
                            {
                                Console.WriteLine("结束并刷新");
                                RefreshProcesses_ListView();
                            }
                            else
                            {
                                Console.WriteLine(processItem.Name + " 结束进程失败，错误代码：" + Marshal.GetLastWin32Error());
                            }
                            CloseHandle(processHandle);
                        }
                        else
                        {
                            MessageBox.Show("无法打开进程句柄。错误代码: " + Marshal.GetLastWin32Error());
                        }
                    }
                }
            }
            RefreshProcesses_ListView();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses_ListView();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            var fileName = Process.GetCurrentProcess().MainModule.FileName;
            ContentDialog DelorExitDialog = new ContentDialog
            {
                Title = "是否使用自爆(自我删除)",
                Content = "点击删除则自动退出并删除本体",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "退出",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary
            };
            ContentDialogResult result = await DelorExitDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DeleteProcessFile(fileName, 10);
                notifyIcon.Dispose();
                Environment.Exit(0);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                notifyIcon.Dispose();
                Environment.Exit(0);
            }
            else {
                DelorExitDialog.Hide();
            }

        }

        private static void DeleteProcessFile(string fileName, int DelaySecond)
        {
            fileName = Path.GetFullPath(fileName);
            var folder = Path.GetDirectoryName(fileName);
            var CurrentProcessFileName = Path.GetFileName(fileName);
            var arguments = $"/c timeout /t {DelaySecond} && DEL /f {CurrentProcessFileName} ";
            var processStartInfo = new ProcessStartInfo()
            {
                Verb = "runas",
                FileName = "cmd",
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = arguments,
                WorkingDirectory = folder,
            };
            Process.Start(processStartInfo);
        }

        private void NotifyIcon_Dis() {
            var fileName = Process.GetCurrentProcess().MainModule.FileName;

            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                BalloonTipText = "Fucking Classroom System"
            };
            notifyIcon.ShowBalloonTip(2000);
            notifyIcon.Text = "Fucking Classroom System";
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("显示主窗口", null, (sender, eventArgs) =>
            {
                Show();
                Visibility = Visibility.Visible;
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
            });
            notifyIcon.ContextMenuStrip.Items.Add("自爆(10秒)", null, (sender, eventArgs) =>
            {
                notifyIcon.Dispose();
                DeleteProcessFile(fileName,4);
                Environment.Exit(0);
            });
            notifyIcon.ContextMenuStrip.Items.Add("关闭程序", null, (sender, eventArgs) =>
            {
                notifyIcon.Dispose();
                Environment.Exit(0);
            });
            //托盘双击响应
            notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler((o, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    notifyIcon.ContextMenuStrip.Items[0].PerformClick();
                }
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                notifyIcon.ShowBalloonTip(20, "提示", "Fucking Classroom System已最小化至托盘", System.Windows.Forms.ToolTipIcon.Info);
            }
        }
    }

    public class ProcessItem
    {
        public string Name { get; set; }
        public string State { get; set; }

        public ProcessItem(string name, bool isSuspended)
        {
            Name = name;
            State = isSuspended ? "已挂起" : "正在运行";
        }
    }

}