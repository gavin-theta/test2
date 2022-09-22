using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Web.Http;

namespace Client
{
    public static class GetJWTToken
    {
        [FunctionName("GetJWTToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var jwtToken = Utility.GenerateToken(log);
                return new OkObjectResult(jwtToken);
            }
            catch(Exception e)
            {
                log.LogInformation($"Error - GetJWTToken -- {e.Message}");
                return new BadRequestErrorMessageResult(e.Message);
            }
           
        }
    }
}
