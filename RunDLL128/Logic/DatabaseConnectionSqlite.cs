using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Data.SQLite;
using System;

namespace RunDLL128.Logic
{
    internal static class DatabaseConnectionSqlite
    {
        public static IDbConnection EstablishConnection()
        {
            string sqlPath = Path.Combine(Path.GetDirectoryName(typeof(DatabaseConnectionSqlite).Assembly.Location), "db.db");

            SQLiteConnectionStringBuilder b = new()
            {
                DataSource = sqlPath
            };

            SQLiteConnection conn = new(b.ToString());
            conn.Open();

            int c;

            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='processlist';";

                c = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (c <= 0)
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE processlist (id INTEGER PRIMARY KEY, name TEXT NOT NULL);";
                    cmd.ExecuteNonQuery();
                }
            }

            return conn;
        }
    }
}
