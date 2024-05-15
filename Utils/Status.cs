using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Utils.Status
{
    public class Status
    {
        [Function("StatusCheck")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "Status")] HttpRequest req)
        {
            return new OkObjectResult("Status: OK");
        }
    }

}