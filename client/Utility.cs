using System;
using Newtonsoft.Json.Linq;
using Client.Entities;
using System.Net;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Data;
using Newtonsoft.Json;
using CsvHelper;
using System.Globalization;

namespace Client
{
    public static class Utility
    {
        public static string GenerateToken(ILogger log)
        {
            try
            {
                var iss = "WEL.SSE.Client";
                var nbfDatetimeoffset = new DateTimeOffset(DateTime.UtcNow.AddSeconds(-60));
                var nbf = nbfDatetimeoffset.ToUnixTimeSeconds();
                var expDatetimeoffset = new DateTimeOffset(DateTime.UtcNow.AddSeconds(-60).AddSeconds(300));
                var exp = expDatetimeoffset.ToUnixTimeSeconds();
                var cert = GetCert(log);
                var claims = new Dictionary<string, object>()
                    {
                        { "nbf", nbf },
                        { "exp", exp },
                        { "iss", iss }
                    };
                var securityTokenDescriptor = new SecurityTokenDescriptor
                {
                    Claims = claims,
                    SigningCredentials = new X509SigningCredentials(cert)
                };
                var handler = new JsonWebTokenHandler();
                var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);
                return signedClientAssertion;
            }
            catch (Exception e)
            {
                log.LogInformation($"Error - GenerateToken -- {e.Message}");
                throw;
            }
        }

        public static X509Certificate2 GetCert(ILogger log)
        {
            try
            {

                string CERTIFICATE_NAME = GetEnvironmentVariable("CertName");
                string keyVaultUrl = GetEnvironmentVariable("KVUrl");
                log.LogInformation($"GenerateToken: Get Cert from KeyVault.");
                var keyVaultClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                var certificateSecret = keyVaultClient.GetSecretAsync(CERTIFICATE_NAME).GetAwaiter().GetResult().Value;
                var certificate = Convert.FromBase64String(certificateSecret.Value);
                var cert = new X509Certificate2(certificate, "", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
                return cert;
            }
            catch (Exception e)
            {
                log.LogInformation($"Error - GetCert -- {e.Message}");
                throw;
            }
        }

        public static string GetInstructions(string url, string apiKey, string jwtToken, string correlationId, ILogger log)
        {
            try
            {
                string instruction;
                var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                request.Headers.Add("Authorization", "Bearer " + jwtToken);
                request.Headers.Add("correlation-Id", correlationId);
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                log.LogInformation($"GetInstruction: Calling TransPower API.");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    instruction = reader.ReadToEnd();
                }
                return instruction;
            }
            catch (Exception e)
            {
                log.LogInformation($"Error - GetInstructions -- {e.Message}");
                throw;
            }
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name);
        }

        public static string GetHeader(IHeaderDictionary headers, string key)
        {
            try
            {
                headers.TryGetValue(key, out var value);
                return value.First();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Header '{0}' not present in the request.", key), ex);
            }
        }

        public static List<DispatchInstruction> CreateInstructionsList(JToken obj, string dispatchGroup)
        {
            List<DispatchInstruction> InstructionList = new List<DispatchInstruction>();
            try
            {
                foreach (var result in obj)
                {
                    JToken dispatch = result["interruptibleLoadDispatch"];
                    switch (dispatchGroup)
                    {
                        case "interruptibleload":
                            dispatch = result["interruptibleLoadDispatch"];
                            break;
                        case "energy":
                            dispatch = result["energyDispatch"];
                            break;
                        case "frequency":
                            dispatch = result["frequencyDispatch"];
                            break;
                        case "voltage":
                            dispatch = result["voltageDispatch"];
                            break;
                    }
                    foreach (var block in dispatch["blocks"])
                    {
                        foreach (var primaryValue in block["primaryValues"])
                        {
                            DispatchInstruction instruction = CreateInstruction(result, block, primaryValue, false);
                            InstructionList.Add(instruction);
                        }
                        foreach (var node in block["nodes"])
                        {
                            foreach (var primaryValue in node["primaryValues"])
                            {
                                DispatchInstruction instruction = CreateInstruction(result, node, primaryValue, true);
                                InstructionList.Add(instruction);
                            }
                        }
                    }
                    foreach (var node in dispatch["nodes"])
                    {
                        foreach (var primaryValue in node["primaryValues"])
                        {
                            DispatchInstruction instruction = CreateInstruction(result, node, primaryValue, true);
                            InstructionList.Add(instruction);
                        }
                    }
                }
                return InstructionList;
            }
            catch
            {
                throw;
            }
        }

        public static DispatchInstruction CreateInstruction(JToken result, JToken block, JToken primaryValue, bool isNode)
        {
            DispatchInstruction instruction = new DispatchInstruction();
            try
            {
                instruction.DispatchGroupName = result["dispatchGroupName"].ToString();
                instruction.correlationId = result["correlationId"].ToString();
                instruction.sequenceNumber = (int)result["sequenceNumber"];
                instruction.dispatchEndpointOwner = result["dispatchEndpointOwner"].ToString();
                instruction.isUserResend = (int)result["isUserResend"];
                instruction.messageSentTime = Convert.ToDateTime(result["messageSentTime"].ToString());
                if (isNode)
                {
                    instruction.NodeCode = block["name"].ToString();
                }
                else
                {
                    instruction.BlockCode = block["name"].ToString();
                }
                instruction.traderCode = block["traderCode"].ToString();
                instruction.PrimaryDispatchTypeCode = primaryValue["dispatchType"].ToString();
                instruction.dispatchValue = (decimal)primaryValue["dispatchValue"];
                instruction.dispatchTime = Convert.ToDateTime(primaryValue["dispatchTime"].ToString());
                instruction.dispatchIssueTime = Convert.ToDateTime(primaryValue["dispatchIssueTime"].ToString());
                instruction.AuditDateCreated = DateTime.Now;
                instruction.AuditUserCreated = "Test User";
                instruction.AuditProcessCreated = "Test Process";
                instruction.AuditDateUpdated = DateTime.Now;
                return instruction;
            }
            catch
            {
                throw;
            }
        }

        public static DispatchAck CreateDispatchAck(ACK ack, string correlationId)
        {
            DispatchAck dack = new DispatchAck();
            try
            {
                dack.DispatchGroup = ack.dispatchGroupName;
                dack.ackType = ack.ackType;
                dack.sequenceNumber = Int32.Parse(ack.sequenceNumber);
                dack.AuditDateCreated = DateTime.Now;
                dack.AuditUserCreated = "Test User";
                dack.AuditProcessCreated = "Test Process";
                dack.AuditDateUpdated = DateTime.Now;
                dack.correlationId = correlationId;
                return dack;
            }
            catch
            {
                throw;
            }
        }

        public static void SendAck(ACK ack, string ackuri, string apiKey, string jwtToken, string correlationId)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(ackuri);
                request.Method = "PUT";
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                request.Headers.Add("Authorization", "Bearer " + jwtToken);
                request.Headers.Add("Correlation-Id", correlationId);
                request.Headers.Add("Content-Type", "application/json");

                string jsonString = System.Text.Json.JsonSerializer.Serialize(ack);

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonString);
                }

                var response = (HttpWebResponse)request.GetResponse();
            }
            catch
            {

            }
        }

        public static string GetSQLConnectionString()
        {
            try
            {
                string sqlConnectionString = string.Empty;
                string ServerName = GetEnvironmentVariable("DBServerName");
                string DatabaseName = GetEnvironmentVariable("DatabaseName");

                sqlConnectionString = "Server=" + ServerName + ";Initial Catalog=" + DatabaseName + ";Persist Security Info=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

                return sqlConnectionString;
            }
            catch
            {
                throw;
            }
        }

        public static string JsonToCSV(string jsonContent)
        {
            StringWriter csvString = new StringWriter();
            using (var csv = new CsvWriter(csvString, CultureInfo.CurrentCulture))
            {
                using (var dt = JsonStringToTable(jsonContent))
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }
                    csv.NextRecord();

                    foreach (DataRow row in dt.Rows)
                    {
                        for (var i = 0; i < dt.Columns.Count; i++)
                        {
                            csv.WriteField(row[i]);
                        }
                        csv.NextRecord();
                    }
                }
            }
            return csvString.ToString();
        }
        public static DataTable JsonStringToTable(string jsonContent)
        {
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(jsonContent);
            return dt;
        }
    }
}
