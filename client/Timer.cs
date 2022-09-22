using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Client.Entities;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading.Tasks;

namespace Client
{
    public static class Timer
    {
        [FunctionName("ReceiveInstructionsTimer")]
        public static async Task Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                string apiKey = Utility.GetEnvironmentVariable("APIKey");
                string ackuri = Utility.GetEnvironmentVariable("ACKUri");
                string uri = Utility.GetEnvironmentVariable("SSEUri");
                string ServerName = Utility.GetEnvironmentVariable("DBServerName");
                string DatabaseName = Utility.GetEnvironmentVariable("DatabaseName");

                var correlationId = Guid.NewGuid().ToString();
                log.LogInformation($"START: GenerateToken");
                var jwtToken = Utility.GenerateToken(log);
                log.LogInformation($"END: GenerateToken");
                log.LogInformation($"START: GetInstructions");
                var instructions = Utility.GetInstructions(uri, apiKey, jwtToken, correlationId, log);
                log.LogInformation($"END: GetInstructions - RAW MESSAGE: {instructions.ToString()}");

                JToken instructionsJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<JToken>(instructions);
                var dispatchGroup = instructionsJSON.First["dispatchGroupName"].ToString();
                var sequenceNumber = instructionsJSON.First["sequenceNumber"].ToString();
                correlationId = instructionsJSON.First["correlationId"].ToString();
                log.LogInformation($"SEQUENCE NUMBER : {sequenceNumber}");
                log.LogInformation($"DISPATCH GROUP: {dispatchGroup}");

                var dipatchInstructions = Utility.CreateInstructionsList(instructionsJSON, dispatchGroup);
                bool ifAckExist = false;

                string connectionString = "Server=" + ServerName + ";Initial Catalog=" + DatabaseName + ";Persist Security Info=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");

                log.LogInformation($"START: DB ACCESS");
                if (DataAccess.IfAutoAck(connectionString, accessToken, sequenceNumber, dispatchGroup, ref ifAckExist))
                {
                    ACK ack = new ACK();
                    ack.ackType = DataAccess.getACKType(connectionString, accessToken, dipatchInstructions);
                    ack.dispatchGroupName = dispatchGroup;
                    ack.sequenceNumber = sequenceNumber;
                    log.LogInformation($"START: SENDING ACK");
                    Utility.SendAck(ack, ackuri, apiKey, jwtToken, correlationId);
                    log.LogInformation($"ACKTYPE SENT: {ack.ackType}");
                    var dispatchAck = Utility.CreateDispatchAck(ack, correlationId);
                    DataAccess.DBInsertAck(connectionString, accessToken, dispatchAck);
                }
                else
                {
                    if (!ifAckExist)
                    {
                        ACK ack = new ACK();
                        ack.ackType = "ACK";
                        ack.dispatchGroupName = dispatchGroup;
                        ack.sequenceNumber = sequenceNumber;
                        log.LogInformation($"START: SENDING ACK");
                        Utility.SendAck(ack, ackuri, apiKey, jwtToken, correlationId);
                        log.LogInformation($"ACKTYPE SENT: {ack.ackType}");
                    }
                    else
                    {
                        log.LogInformation($"ACKTYPE SENT: NO ACK SENT. ACK ALREADY EXIST.");
                    }
                }

                DataAccess.DBInsertInsctructions(connectionString, accessToken, dipatchInstructions);

                log.LogInformation($"INSTRUCTIONS PROCESSED SUCCESSFULLY.");

            }
            catch (Exception e)
            {
                log.LogInformation($"ERROR: {e.Message}");
            }
        }
    }
}
