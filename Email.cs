using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RemapService
{
    public class Email
    {
        public void Send(string emailAddress, bool success, string connectionString )
        {
            Logger logger = new Logger();
            try
            {
                string sql =
                    @"INSERT INTO[dbo].[EmailQueue]([EQ_To],[EQ_From],[EQ_Subject],[EQ_Body],[EQ_IsHtml],[EQ_Status],[EQ_StatusText],[EQ_Category],[EQ_Created],[EQ_LastUpdate])VALUES (" +
                    emailAddress + ",netpost@aesoponline.com, RemapProcess," +
                    (success ? "Remap was sucessfully processed!" : "Error during Remap Processing!") + ",0,0,Remap,1" +
                    DateTime.Now + "," + DateTime.Now + ")";
                string commandText = sql;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        int updatedRows = command.ExecuteNonQuery();
                    }
                }
                logger.Log("Sent email for remap to " + emailAddress, connectionString);
            }
            catch (Exception ex)
            {
               logger.Log("Error Sending email" + ex.ToString(), connectionString);
            }
        }
    }
}
