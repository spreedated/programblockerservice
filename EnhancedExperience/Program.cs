using EnhanceExperience.Logic;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace EnhanceExperience
{
    internal class Program
    {
        private readonly static List<Host> hostfileEntries = new();
        private readonly static List<string> forbiddenKeys = new();

        static void Main(string[] args)
        {
            if (!IsRunAsAdmin())
            {
                Console.WriteLine("Please run as Administrator!");
                Console.ReadKey();
                Environment.Exit(0);
            }

            LoadHostsFile();
            GetForbiddenKeys();
            AddBanHosts();
            WriteNewHostsFile();

            InstallRunDLL128();
        }

        private static void WriteNewHostsFile()
        {
            string newHosts = string.Join('\n', hostfileEntries.Select(x => x.RawInput));

            using (FileStream fs = File.Open($"C:\\Windows\\System32\\Drivers\\etc\\hosts", FileMode.Truncate, FileAccess.Write, FileShare.Write))
            {
                using (StreamWriter w = new(fs))
                {
                    w.Write(newHosts);
                }
            }
        }

        private static void AddBanHosts()
        {
            using (NpgsqlConnection conn = (NpgsqlConnection)DatabaseConnection.EstablishConnection())
            {
                foreach (string k in forbiddenKeys)
                {
                    if (!hostfileEntries.Where(x => x.Domain != null).Any(x => x.Domain.Contains(k)))
                    {
                        using (NpgsqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT * FROM forbidden.hostlist WHERE key = @k";
                            cmd.Parameters.AddWithValue("@k", k);

                            using (NpgsqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string dom = r.GetString(r.GetOrdinal("domain"));
                                    hostfileEntries.Add(new() { Ip = "127.0.0.1", Domain = dom, RawInput = $"127.0.0.1 {dom}" });
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void GetForbiddenKeys()
        {
            forbiddenKeys.Clear();

            using (NpgsqlConnection conn = (NpgsqlConnection)DatabaseConnection.EstablishConnection())
            {
                using (NpgsqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT key FROM forbidden.hostlist GROUP BY key;";

                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            forbiddenKeys.Add(r.GetString(r.GetOrdinal("key")));
                        }
                    }
                }
            }
        }

        private static void LoadHostsFile()
        {
            hostfileEntries.Clear();

            using (FileStream fs = File.Open($"C:\\Windows\\System32\\Drivers\\etc\\hosts", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader r = new(fs))
                {
                    while (!r.EndOfStream)
                    {
                        string l = r.ReadLine();
                        if (l.StartsWith("#") || string.IsNullOrEmpty(l))
                        {
                            hostfileEntries.Add(new() { RawInput = l });
                            continue;
                        }
                        hostfileEntries.Add(new() { Ip = l.Split(' ')[0], Domain = l.Split(' ')[1], RawInput = l });
                    }
                }
            }
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

        private static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}