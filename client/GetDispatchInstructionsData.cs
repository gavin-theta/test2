using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Client.Entities;
using Microsoft.Azure.Services.AppAuthentication;
using System.Text;

namespace Client
{
    public static class GetDispatchInstructionsData
    {
        [FunctionName("GetDispatchInstructionsData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            List<DIData> _diData = new List<DIData>();
            bool isCSVoutput = false;

            try
            {
                var startDate = DateTime.Parse(Utility.GetHeader(req.Headers, "StartDate").ToString());
                var endDate = DateTime.Parse(Utility.GetHeader(req.Headers, "EndDate").ToString());
                if (req.Headers.ContainsKey("CSV"))
                {
                    isCSVoutput = bool.Parse(Utility.GetHeader(req.Headers, "CSV").ToString());
                }
                if (startDate >= endDate)
                {
                    throw new Exception("End Date must be greater than Start Date.");
                }

                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");

                _diData = DataAccess.getInstructionsData(startDate, endDate, accessToken, log);

                if (isCSVoutput)
                {
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(_diData);
                    string csv = Utility.JsonToCSV(jsonString);
                    byte[] bytes = Encoding.ASCII.GetBytes(csv);
                    return new FileContentResult(bytes, "text/csv");
                }
                else
                {
                    return new OkObjectResult(_diData);
                }

            }
            catch (Exception e)
            {
                var errorMessage = e.Message;
                log.LogError("Error: {0}", errorMessage);
                if (errorMessage.Contains("not recognized as a valid Boolean"))
                {
                    errorMessage = "CSV header can have only true/false value.";
                }
                return new BadRequestObjectResult(errorMessage);
            }
        }
    }
}
