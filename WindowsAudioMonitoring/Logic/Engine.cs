using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WindowsAudioMonitoring.Logic
{
    internal class Engine : IDisposable
    {
        internal Timer loopTimer = new();
        internal bool isRunning = false;
        internal DateTime databaseConnection;

        internal List<DTO_Process> proc = new();
        internal List<DTO_Process> procSQL = new();

        public Engine()
        {
            loopTimer.Elapsed += LoopTimer_Elapsed;
            loopTimer.Interval = new TimeSpan(0,5,0).TotalMilliseconds;
            loopTimer.Enabled = true;
            loopTimer.Start();

            this.LoopTimer_Elapsed(this, null);
        }

        public void LoopTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.isRunning)
            {
                return;
            }

            this.isRunning = true;

            try
            {
                procSQL.Clear();

                if (databaseConnection.Date != DateTime.Now.Date)
                {
                    using (NpgsqlConnection conn = (NpgsqlConnection)DatabaseConnection.EstablishConnection())
                    {
                        using (NpgsqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT name, action FROM forbidden.processlist";

                            using (NpgsqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    procSQL.Add(new()
                                    {
                                        Processname = r.GetString(r.GetOrdinal("name")),
                                        Action = (DTO_Process.Actions)r.GetInt32(r.GetOrdinal("action"))
                                    });
                                }
                            }
                        }
                    }

                    databaseConnection = DateTime.Now;
                }
            }
            catch (Exception)
            {
                
            }

            proc.Clear();

            using (SQLiteConnection conn = (SQLiteConnection)DatabaseConnectionSqlite.EstablishConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM processlist;";

                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            proc.Add(new()
                            {
                                Processname = r.GetString(r.GetOrdinal("name"))
                            });
                        }
                    }
                }

                foreach (DTO_Process p in procSQL.Where(x => x.Action != DTO_Process.Actions.Delete).Except(proc))
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO processlist VALUES (NULL, @name);";
                        cmd.Parameters.AddWithValue("@name", p.Processname);

                        cmd.ExecuteNonQuery();
                    }

                    proc.Add(p);
                }

                foreach (DTO_Process p in procSQL.Where(x => x.Action == DTO_Process.Actions.Delete).Except(proc))
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM processlist WHERE \"name\" = @name;";
                        cmd.Parameters.AddWithValue("@name", p.Processname);

                        cmd.ExecuteNonQuery();
                    }

                    if (proc.Any(x => x.Processname == p.Processname))
                    {
                        proc.Remove(proc.First(x => x.Processname == p.Processname));
                    }

                }
            }

            Process[] procs = Process.GetProcesses();

            foreach (Process p in procs.Where(x => proc.Any(y => y.Processname.Contains(x.ProcessName.ToLower()))))
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        SendMailToUser(p.ProcessName);
                        SendMailToBoss(p.ProcessName);
                        AddIllegalEntry(p.ProcessName);
                    }
                    catch (Exception ex)
                    {
                    }

                    p.Kill();
                });
            }

            this.isRunning = false;
        }

        private static void AddIllegalEntry(string processname)
        {
            using (NpgsqlConnection conn = (NpgsqlConnection)DatabaseConnection.EstablishConnection())
            {
                using (NpgsqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO forbidden.guiltyusers (username,processname) VALUES (@n,@p);";
                    cmd.Parameters.AddWithValue("@n", Environment.UserName);
                    cmd.Parameters.AddWithValue("@p", processname);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SendMailToBoss(string processname)
        {
            EmailMessage emailMessage = new($"Illegale Aktion entdeckt! - {Environment.UserName}", $"Eine illegale Software wurde festgestellt - es wurde die Ausführung von \"{processname}\" entdeckt", true, new EmailAddress("markus.wackermann@api.de"));
            EmailService.SendProxyNodeBypass(emailMessage);
        }

        private static void SendMailToUser(string processname)
        {
            string emailaddress = null;
            string name = null;

            using (NpgsqlConnection conn = (NpgsqlConnection)DatabaseConnection.EstablishConnection())
            {
                using (NpgsqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT email,name FROM fuman.users WHERE samaccountname = @s LIMIT 1;";
                    cmd.Parameters.AddWithValue("@s", Environment.UserName);

                    emailaddress = (string)cmd.ExecuteScalar();

                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            emailaddress = r.GetString(r.GetOrdinal("email"));
                            name = r.GetString(r.GetOrdinal("name"));
                        }
                    }
                }
            }

            EmailMessage emailMessage = new("Illegale Aktion entdeckt!", $"Hallo <b>{name}</b>,<br/>eine illegale Software wurde auf deinem PC festgestellt - es wurde die Ausführung von \"{processname}\" entdeckt.<br/>Dein vorgesetzter wurde in Kenntnis gesetzt.", true, new EmailAddress(emailaddress));
            EmailService.SendProxyNodeBypass(emailMessage);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                loopTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
