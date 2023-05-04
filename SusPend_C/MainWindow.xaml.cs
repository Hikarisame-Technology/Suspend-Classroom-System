using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;

namespace SusPend_C
{

    public partial class MainWindow : Window
    {
        private const int PROCESS_SUSPEND_RESUME = 0x0800;

        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess(IntPtr processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        public MainWindow()
        {
            InitializeComponent();
            processListView.ItemsSource = GetProcessList();
            RefreshProcesses();
            this.Title = "Fucking Classroom System Ver" + Application.ResourceAssembly.GetName().Version.ToString().ToString();
        }

        private List<ProcessItem> GetProcessList()
        {
            List<ProcessItem> processList = new List<ProcessItem>();
            string[] processNames = new string[] { "StudentMain", "CMLauncher", "ClassManagerApp", "REDAgent", "Student", "MultiClient", "Smonitor", "EnigmaVBUnpacker" };//EnigmaVBUnpacker为测试用例
            foreach (string processName in processNames)
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    processList.Add(new ProcessItem(process.ProcessName, false));
                }
            }
            return processList;
        }

        private void RefreshProcesses()
        {
            foreach (ProcessItem processItem in processListView.Items)
            {
                Process[] processes = Process.GetProcessesByName(processItem.Name);
                bool isSuspended = false;
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName == processItem.Name && process.Threads[0].ThreadState == ThreadState.Wait && process.Threads[0].WaitReason == ThreadWaitReason.Suspended)
                    {
                        isSuspended = true;
                        break;
                    }
                }
                processItem.State = isSuspended ? "已挂起" : "正在运行";
            }
        }


        private void SuspendProcesses_Click(object sender, RoutedEventArgs e)
        {
            foreach (ProcessItem processItem in processListView.Items)
            {
                //if (processItem.State == "已挂起") continue;
                Process[] processes = Process.GetProcessesByName(processItem.Name);
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
                        }
                        else
                        {
                            MessageBox.Show("打开 " + processItem.Name + " 进程失败。");
                        }
                    }
                }
            }
            processListView.Items.Refresh();
            RefreshProcesses();
        }
        private void ResumeProcesses_Click(object sender, RoutedEventArgs e)
        {
            foreach (ProcessItem processItem in processListView.Items)
            {
                //if (processItem.State == "正在运行") continue;
                Process[] processes = Process.GetProcessesByName(processItem.Name);
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
                        }
                        else
                        {
                            MessageBox.Show("打开 " + processItem.Name + " 进程失败。");
                        }
                    }
                }
            }
            processListView.Items.Refresh();
            RefreshProcesses();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            processListView.ItemsSource = GetProcessList();
            RefreshProcesses();
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