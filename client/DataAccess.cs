using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Client.Entities;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Client
{
    public static class DataAccess
    {

        public static void DBInsertInsctructions(string connectionString, string token, List<DispatchInstruction> instructionslist)
        {
            try
            {
                SqlConnection sqlCon = new SqlConnection(connectionString);
                sqlCon.AccessToken = token;

                using (sqlCon)
                {
                    SqlCommand sql_cmnd = new SqlCommand("WTPApp.InsertWTPInstructions", sqlCon);
                    sql_cmnd.CommandType = CommandType.StoredProcedure;
                    var instructionsParam =
                        new SqlParameter("@InstructionList", SqlDbType.Structured)
                        {
                            TypeName = "dbo.WTPInstructionType",
                            Value = GetSqlDataRecordsInstructionList(instructionslist)
                        };
                    sql_cmnd.Parameters.Add(instructionsParam);

                    sqlCon.Open();
                    sql_cmnd.ExecuteNonQuery();
                    sqlCon.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public static void DBInsertAck(string connectionString, string token, DispatchAck dack)
        {
            try
            {
                SqlConnection sqlCon = new SqlConnection(connectionString);
                sqlCon.AccessToken = token;
                using (sqlCon)
                {
                    SqlCommand sql_cmnd = new SqlCommand("WTPApp.InsertWTPAck", sqlCon);
                    sql_cmnd.CommandType = CommandType.StoredProcedure;
                    var dispatchACKparam =
                        new SqlParameter("@DisPatchAck", SqlDbType.Structured)
                        {
                            TypeName = "dbo.WTPDispatchAckType",
                            Value = GetSqlDataRecordsAck(dack)
                        };
                    sql_cmnd.Parameters.Add(dispatchACKparam);

                    sqlCon.Open();
                    sql_cmnd.ExecuteNonQuery();
                    sqlCon.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool IfAutoAck(string connectionString, string token, string seqNumber, string dispatchGroup, ref bool ifAckExist)
        {
            bool ifAutoAck = false;

            string ackQuery = "SELECT ACK.WTPDispatchAckID from [WTPApp].[WTPDispatchAck] ACK INNER JOIN [WTPApp].[WTPDispatchGroup] DG ON DG.DispatchGroup = ACK.DispatchGroup WHERE ACK.sequenceNumber = @SeqNumber AND DG.[Description] = @dispatchGroup";
            string lookupQuery = "SELECT t1.[value] FROM [WTPApp].[WTPLookup] t1 WHERE t1.LookupKey = 'AUTOACK' AND t1.LookupType = 'PROGRAM-PARAMETER' AND t1.ProgramCode = 'IL'";

            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                connection.AccessToken = token;

                using (connection)
                {
                    using (SqlCommand command = new SqlCommand(ackQuery, connection))
                    {
                        command.Parameters.AddWithValue("@SeqNumber", seqNumber);
                        command.Parameters.AddWithValue("@dispatchGroup", dispatchGroup);
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                ifAckExist = true;
                            }
                        }
                    }
                    if (!ifAckExist)
                    {
                        using (SqlCommand command2 = new SqlCommand(lookupQuery, connection))
                        {
                            using (SqlDataReader reader2 = command2.ExecuteReader())
                            {
                                if (reader2.HasRows)
                                    while (reader2.Read())
                                    {
                                        if (reader2["value"].ToString().ToUpper() == "Y")
                                        {
                                            ifAutoAck = true;
                                        }
                                        else
                                        {
                                            ifAutoAck = false;
                                        }

                                    }
                            }
                        }
                    }

                }
                return ifAutoAck;
            }
            catch
            {
                throw;
            }
        }

        public static string getACKType(string connectionString, string token, List<DispatchInstruction> instructionslist)
        {
            string ackType = "ACKA";
            string contractRefQuery = "SELECT t1.[ContractValueMW] FROM [WTPApp].[WTPContractReference] t1 \n" +
                                        "INNER JOIN[WTPApp].[WTPDispatchGroup] DG ON DG.DispatchGroup = t1.DispatchGroup \n" +
                                        "WHERE t1.[BlockCode] = @blockCode \n" +
                                        "AND DG.[Description]= @dispatchGroup \n" +
                                        "AND t1.[PrimaryDispatchTypeCode] = @primaryDispatchType \n" +
                                        "AND @dispatchTime >=  t1.[EffectiveStartDate] \n" +
                                        "AND @dispatchTime <= COALESCE(t1.[EffectiveEndDate],'2099-12-31')";
            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                connection.AccessToken = token;
                using (connection)
                {
                    connection.Open();
                    foreach (var instruction in instructionslist)
                    {
                        using (SqlCommand command = new SqlCommand(contractRefQuery, connection))
                        {
                            command.Parameters.AddWithValue("@blockCode", instruction.BlockCode);
                            command.Parameters.AddWithValue("@dispatchGroup", instruction.DispatchGroupName);
                            command.Parameters.AddWithValue("@primaryDispatchType", instruction.PrimaryDispatchTypeCode);
                            command.Parameters.AddWithValue("@dispatchTime", instruction.dispatchTime);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                    while (reader.Read())
                                    {
                                        var ContractValueMW = reader["ContractValueMW"] ?? 0;
                                        if ((decimal)ContractValueMW < instruction.dispatchValue)
                                            ackType = "ACKQ";
                                    }
                            }
                        }
                    }
                }

                return ackType;
            }
            catch
            {
                throw;
            }
        }

        public static List<SqlDataRecord> GetSqlDataRecordsInstructionList(List<DispatchInstruction> instructions)
        {
            try
            {
                List<SqlDataRecord> datatable = new List<SqlDataRecord>();
                SqlMetaData[] sqlMetaData = new SqlMetaData[19];
                sqlMetaData[0] = new SqlMetaData("DispatchGroupName", SqlDbType.VarChar, 30);
                sqlMetaData[1] = new SqlMetaData("PrimaryDispatchTypeCode", SqlDbType.VarChar, 30);
                sqlMetaData[2] = new SqlMetaData("NodeCode", SqlDbType.VarChar, 30);
                sqlMetaData[3] = new SqlMetaData("BlockCode", SqlDbType.VarChar, 30);
                sqlMetaData[4] = new SqlMetaData("correlationId", SqlDbType.VarChar, 100);
                sqlMetaData[5] = new SqlMetaData("sequenceNumber", SqlDbType.Int);
                sqlMetaData[6] = new SqlMetaData("dispatchEndpointOwner", SqlDbType.VarChar, 100);
                sqlMetaData[7] = new SqlMetaData("isUserResend", SqlDbType.Int);
                sqlMetaData[8] = new SqlMetaData("messageSentTime", SqlDbType.DateTime);
                sqlMetaData[9] = new SqlMetaData("traderCode", SqlDbType.VarChar, 30);
                sqlMetaData[10] = new SqlMetaData("dispatchValue", SqlDbType.Decimal, 18, 2);
                sqlMetaData[11] = new SqlMetaData("dispatchTime", SqlDbType.DateTime);
                sqlMetaData[12] = new SqlMetaData("dispatchIssueTime", SqlDbType.DateTime);
                sqlMetaData[13] = new SqlMetaData("AuditDateCreated", SqlDbType.DateTime);
                sqlMetaData[14] = new SqlMetaData("AuditUserCreated", SqlDbType.VarChar, 100);
                sqlMetaData[15] = new SqlMetaData("AuditProcessCreated", SqlDbType.VarChar, 100);
                sqlMetaData[16] = new SqlMetaData("AuditDateUpdated", SqlDbType.DateTime);
                sqlMetaData[17] = new SqlMetaData("AuditUserUpdated", SqlDbType.VarChar, 100);
                sqlMetaData[18] = new SqlMetaData("AuditProcessUpdated", SqlDbType.VarChar, 100);

                foreach (var instruction in instructions)
                {
                    SqlDataRecord row = new SqlDataRecord(sqlMetaData);
                    row.SetValues(instruction.DispatchGroupName,
                                    instruction.PrimaryDispatchTypeCode,
                                    instruction.NodeCode ?? (object)DBNull.Value,
                                    instruction.BlockCode ?? (object)DBNull.Value,
                                    instruction.correlationId,
                                    instruction.sequenceNumber,
                                    instruction.dispatchEndpointOwner,
                                    instruction.isUserResend,
                                    instruction.messageSentTime,
                                    instruction.traderCode,
                                    instruction.dispatchValue,
                                    instruction.dispatchTime,
                                    instruction.dispatchIssueTime,
                                    instruction.AuditDateCreated,
                                    instruction.AuditUserCreated,
                                    instruction.AuditProcessCreated,
                                    instruction.AuditDateUpdated,
                                    instruction.AuditUserUpdated ?? (object)DBNull.Value,
                                    instruction.AuditProcessUpdated ?? (object)DBNull.Value
                                    );
                    datatable.Add(row);
                }

                return datatable;
            }
            catch
            {
                throw;
            }
        }

        public static List<SqlDataRecord> GetSqlDataRecordsAck(DispatchAck ack)
        {
            try
            {
                List<SqlDataRecord> datatable = new List<SqlDataRecord>();
                SqlMetaData[] sqlMetaData = new SqlMetaData[10];
                sqlMetaData[0] = new SqlMetaData("DispatchGroup", SqlDbType.VarChar, 30);
                sqlMetaData[1] = new SqlMetaData("ackType", SqlDbType.VarChar, 30);
                sqlMetaData[2] = new SqlMetaData("sequenceNumber", SqlDbType.Int);
                sqlMetaData[3] = new SqlMetaData("AuditDateCreated", SqlDbType.DateTime);
                sqlMetaData[4] = new SqlMetaData("AuditUserCreated", SqlDbType.VarChar, 100);
                sqlMetaData[5] = new SqlMetaData("AuditProcessCreated", SqlDbType.VarChar, 100);
                sqlMetaData[6] = new SqlMetaData("AuditDateUpdated", SqlDbType.DateTime);
                sqlMetaData[7] = new SqlMetaData("AuditUserUpdated", SqlDbType.VarChar, 100);
                sqlMetaData[8] = new SqlMetaData("AuditProcessUpdated", SqlDbType.VarChar, 100);
                sqlMetaData[9] = new SqlMetaData("correlationId", SqlDbType.VarChar, 100);

                SqlDataRecord row = new SqlDataRecord(sqlMetaData);
                row.SetValues(ack.DispatchGroup,
                                ack.ackType ?? (object)DBNull.Value,
                                ack.sequenceNumber,
                                ack.AuditDateCreated,
                                ack.AuditUserCreated,
                                ack.AuditProcessCreated,
                                ack.AuditDateUpdated,
                                ack.AuditUserUpdated ?? (object)DBNull.Value,
                                ack.AuditProcessUpdated ?? (object)DBNull.Value,
                                ack.correlationId ?? (object)DBNull.Value
                                );
                datatable.Add(row);

                return datatable;
            }
            catch
            {
                throw;
            }
        }

        public static List<DIData> getInstructionsData(DateTime startDate, DateTime endDate, string token, ILogger log)
        {
            try
            {
                List<DIData> datatable = new List<DIData>();
                string connectionString = Utility.GetSQLConnectionString();
                SqlConnection sqlCon = new SqlConnection(connectionString);
                sqlCon.AccessToken = token;
                using (sqlCon)
                {
                    using (SqlCommand command = new SqlCommand("WTPApp.GetDispatchInstructions", sqlCon))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                        sqlCon.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DIData di = new DIData()
                                {
                                    DispatchInstructionID = reader["DispatchInstructionID"].ToString(),
                                    SequenceNumber = reader["SequenceNumber"].ToString(),
                                    DispatchType = reader["DispatchType"].ToString(),
                                    DispatchDescription = reader["DispatchDescription"].ToString(),
                                    Block = reader["Block"].ToString(),
                                    DispatchValue = reader["DispatchValue"].ToString(),
                                    DispatchTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(reader["DispatchTime"].ToString()), TimeZoneInfo.Local).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    Received = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(reader["Received"].ToString()), TimeZoneInfo.Local).ToString("yyyy-MM-ddTHH:mm:ssZ")
                                };
                                datatable.Add(di);
                            }
                        }
                        sqlCon.Close();

                    return datatable;
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Error while fetching data from SQL: {0}", ex.Message);
                throw;
            }
        }

    }
}
