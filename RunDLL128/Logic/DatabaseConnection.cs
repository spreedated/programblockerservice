using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunDLL128.Logic
{
    internal static class DatabaseConnection
    {
        public static IDbConnection EstablishConnection()
        {
            NpgsqlConnectionStringBuilder b = new()
            {
                Host = "localhost",
                Port = 5432,
                Username = "root",
                Password = "1234"
            };

            NpgsqlConnection conn = new(b.ToString());
            conn.Open();

            return conn;
        }
    }
}
