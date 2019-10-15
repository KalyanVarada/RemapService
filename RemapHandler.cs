using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RemapService.Models;
using RemapService.Models.Users;

namespace RemapService
{
    public class RemapHandler
    {
        
        public async Task<bool> ProcessRemap(RemapDTO remapRequest, IOptions<Envoirment> envoirment)
        {
            int orgId = 0;
            var env = envoirment.Value.Setting.FirstOrDefault(x => x.Name == remapRequest.Envoirment);
            string connectionString = env.ConnectionString;
            string productServiceUrl = env.ProductServiceUrl;
            bool taEnabled = false;
            Logger logger = new Logger();
            Email email = new Email();
            try
            {
                var selectSql = "DECLARE @context_info varbinary(128) " +
                          "SET @context_info = CAST('bypass trigger' AS varbinary(128)) " +
                          "SET CONTEXT_INFO @context_info " +
                          "DECLARE @orgID int = (SELECT org_id FROM organization o (nolock) WHERE o.Org_XrefID = " + remapRequest.OldOrgXRefId + ") " +
                          "SELECT * FROM organization o (nolock) WHERE org_id=@orgID " +
                          "SELECT * FROM dbo.Permission_ProfileHdr " +
                          "WHERE pph_id in ( " +
                          "SELECT pph_id " +
                          "FROM dbo.permission_profilehdr (nolock) " +
                          "WHERE org_id = @orgID " +
                          "AND( " +
                          "SecurityGroupId is not null " +
                          "OR PermissionProfileId is not null " +
                          "OR SecurityScopeId is not null " +
                          ") " +
                          "AND pph_deleted = 0 " +
                          ") " +
                          "SELECT * FROM dbo.permission_profileDetails " +
                          "WHERE ppd_id in ( " +
                          "SELECT ppd_id " +
                          "FROM dbo.permission_profilehdr pph (nolock) " +
                          "JOIN dbo.permission_profiledetails ppd (nolock) on pph.pph_id = ppd.pph_id " +
                          "WHERE org_id = @orgID " +
                          "AND ppd.permissionSetID is not null)";

                var updateSql = "DECLARE @context_info varbinary(128) " +
                                "SET @context_info = CAST('bypass trigger' AS varbinary(128)) " +
                                "SET CONTEXT_INFO @context_info " +
                                "DECLARE @orgID int = (SELECT org_id FROM organization o (nolock) WHERE o.Org_XrefID = " + remapRequest.OldOrgXRefId + ") " +
                                "UPDATE dbo.Permission_ProfileHdr " +
                                "SET SecurityGroupId = NULL, PermissionProfileId = NULL, SecurityScopeId = NULL, pph_lastupdate = getdate() " +
                                "WHERE pph_id in ( " +
                                "SELECT pph_id " +
                                "FROM dbo.permission_profilehdr (nolock) " +
                                "WHERE org_id = @orgID " +
                                "AND( " +
                                "SecurityGroupId IS NOT NULL " +
                                "OR PermissionProfileId IS NOT NULL " +
                                "OR SecurityScopeId IS NOT NULL " +
                                ") " +
                                "AND pph_deleted = 0 " +
                                ") " +
                                "UPDATE dbo.permission_profileDetails " +
                                "SET permissionSetID = NULL, ppd_lastupdate = getdate() " +
                                "WHERE ppd_id in ( " +
                                "SELECT ppd_id " +
                                "FROM dbo.permission_profilehdr pph (nolock) " +
                                "JOIN dbo.permission_profiledetails ppd (nolock) on pph.pph_id = ppd.pph_id " +
                                "WHERE org_id = @orgID " +
                                "AND ppd.permissionSetID IS NOT NULL) " +
                                "UPDATE dbo.organization " +
                                "SET org_xrefid = " + remapRequest.NewOrgXRefId + ", org_lastupdate = getdate() " +
                                "WHERE org_id = @orgID GO";

                

                string commandText = selectSql;


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        DataSet ds = new DataSet();
                        using (SqlDataAdapter da = new SqlDataAdapter(command))
                        {
                            da.Fill(ds);
                        }

                        logger.Log(HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(ds)),connectionString);
                        //serilize into json and save the initial state before remap
                        orgId = int.Parse(ds.Tables[0].Rows[0]["org_id"].ToString());
                    }
                    //update 
                    commandText = updateSql;
                    using (SqlCommand sqlCommand = new SqlCommand(commandText, connection))
                    {
                        //int updatedRows = sqlCommand.ExecuteNonQuery();
                        //logger.Log("# of Updated rows Permission/PermissionProfiles" + updatedRows, connectionString);
                    }

                    //turn on permission sync
                    using (SqlCommand cmd = new SqlCommand("TurnOnPermissionSyncing", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@OrgIds ", orgId));
                        cmd.Parameters.Add(new SqlParameter("@TestMode ", 0));
                        //int updatedRows = cmd.ExecuteNonQuery();
                        //logger.Log("# of Updated rows by TurnOnPermissionSyncing" + updatedRows, connectionString);
                    }

                }

                //get users of old and new orgxrefids from idm before the move
                using (var client = new System.Net.Http.HttpClient())
                {
                    
                    var data = await client.GetStringAsync(productServiceUrl);
                    var account = JsonConvert.DeserializeObject<List<Account>>(data);
                    var ipAddress = account.FirstOrDefault().Node.Address;
                    var port = account.FirstOrDefault().Service.Port;

                    // get users for old and new orgs
                    var usersUrl = "http://" + ipAddress + ":" + port + "/api/users?organizationId=" + remapRequest.OldOrgXRefId;
                    var userData = await client.GetStringAsync(usersUrl);
                    var usersOldOrg = JsonConvert.DeserializeObject<UserList>(userData);
                    logger.Log(HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(usersOldOrg)), connectionString);

                    usersUrl = "http://" + ipAddress + ":" + port + "/api/users?organizationId=" + remapRequest.NewOrgXRefId;
                    userData = await client.GetStringAsync(usersUrl);
                    var usersNewOrg = JsonConvert.DeserializeObject<UserList>(userData);
                    logger.Log(HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(usersNewOrg)), connectionString);
                 
                    // get ipAddress and port for product access Service
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", remapRequest.BearerToken);

                    var product = "" + remapRequest.Envoirment == "PRODUCTION" ? string.Empty: remapRequest.Envoirment;  
                    // merger users for absence mgmt - to debug not workig in stage---
                    var url = "http://" + ipAddress + ":" + port + "/api/users/organizations/merge?fromOrg=" + remapRequest.OldOrgXRefId + "&toOrg=" + remapRequest.NewOrgXRefId + "&productId=AbsMgmt" + product;

                    //var response = await client.PostAsync(url, null);
                    //logger.Log("Response from Merge IDM endpoint " + response.StatusCode, connectionString);
                    //email.Send(remapRequest.Email, true,connectionString);

                    // check if ta exists if yes 
                    //if (false)
                    //{
                    //    url = "http://" + ipAddress + ":" + port + "/api/users/organizations/merge?fromOrg=" +
                    //          remapRequest.OldOrgXRefId + "&toOrg = " + remapRequest.NewOrgXRefId + "&productId=ta" + product ;
                    //    response = await client.PostAsync(url, null);
                    //}
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.ToString(), connectionString);
                email.Send(remapRequest.Email, false, connectionString);
                return false;
            }

            return true;
        }
    }
}
