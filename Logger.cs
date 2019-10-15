using System;
using System.Data.SqlClient;

namespace RemapService
{
    public class Logger
    {
        public void Log(string errorMessage, string connectionString)
        {
            try
            {
                string sql =
                    "INSERT INTO log4netlog([Date], [Machine], [Thread], [Level], [Logger], [Message]) VALUES ('" +
                    DateTime.Now + "','" + Environment.MachineName + "', '1', 'INFO', 'RemapProcess','" + errorMessage + "')";
                string commandText = sql;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        int updatedRows = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}