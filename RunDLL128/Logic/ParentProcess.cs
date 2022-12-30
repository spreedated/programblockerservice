using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace RunDLL128.Logic
{
    public static class ParentProcess
    {
        public static int GetID(int processID)
        {
            return Process.GetProcessById(processID).Parent().Id;
        }

        public static string GetOwner(int processId)
        {

            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    return argList[0];
                }
            }

            return "NO OWNER";
        }
    }

    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if (processId.NextValue().Equals(pid))
                {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    }
    //from: http://yacsharpblog.blogspot.com/
    namespace System.Runtime.CompilerServices
    {
        [AttributeUsage(
            AttributeTargets.Assembly
            | AttributeTargets.Class
            | AttributeTargets.Method,
            Inherited = false,
            AllowMultiple = false)
        ]
        public class ExtensionAttribute : Attribute
        {
        }
    }
}
