using Npgsql;
using RunDLL128.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RunDLL128.Logic
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
            loopTimer.Interval = new TimeSpan(0,10,0).TotalMilliseconds;
            loopTimer.Enabled= true;
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
                    //Send Email
                    p.Kill();
                });
            }

            this.isRunning = false;
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
