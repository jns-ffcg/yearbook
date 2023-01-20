using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace YearbookApp
{
    public class StravaAuth
    {
        [JsonProperty("client_id")]
        public int ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("grant_type")]
        public string GrantType { get; set; }
    }


    public class StravaActivitesForAthleteQueueItem
    {
        [JsonProperty("athlete")]
        public int Athelete { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public static class StravaFunction
    {
        [FunctionName(nameof(Auth))]
        public static async Task<IActionResult> Auth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "strava/auth/{code}")] HttpRequest req,
            string code,
            [CosmosDB(databaseName: "Yearbook", containerName: "Atheletes", Connection = "ConnectionString")] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            HttpClient httpClient = new();
            var url = "https://www.strava.com/oauth/token";

            var data = new StravaAuth() { ClientId = 100210, ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET"), Code = code, GrantType = "authorization_code" };


            var jsonData = JsonConvert.SerializeObject(data);
            var contentData = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, contentData);
            var stringData = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {

                AtheleteAuthItem atheleteAuth = new();

                var result = JsonConvert.DeserializeObject<object>(stringData);
                return new OkObjectResult(result);
            }
            return new OkObjectResult(new DefaultResponse("No..."));
        }

        [FunctionName(nameof(Activities))]
        public static async Task<IActionResult> Activities(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "strava/activites/{code}")] HttpRequest req,
            string code,
            ILogger log)
        {

            // Configure OAuth2 access token for authorization: strava_oauth
            Configuration.Default.AccessToken = code;
            var apiInstance = new ActivitiesApi();

            try
            {
                var before = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var after = (int)DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
                // Get Athlete Stats
                List<SummaryActivity> result = await apiInstance.GetLoggedInAthleteActivitiesAsync(before, after);
                return new OkObjectResult(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception when calling AthletesApi.getStats: {e.Message}");
                var response = new DefaultResponse("No...");
                return new OkObjectResult(response);
            }
        }

        [ServiceBusAccount("SERVICEBUS_CONNECTION")]
        [FunctionName(nameof(GetActivitiesForAthlete))]
        public static async Task GetActivitiesForAthlete(
            [ServiceBus("yearbook-dev-servicebus-queue-strava-activities")] IAsyncCollector<dynamic> outputServiceBus2,
            [ServiceBusTrigger("yearbook-dev-servicebus-queue-strava-activities")] StravaActivitesForAthleteQueueItem queueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log
        )
        {
            log.LogInformation($"Fetch activities for athlete: {queueItem.Athelete} - {queueItem.Count}");
            if (queueItem.Count > 5)
            {
                log.LogInformation($"Done!");
                return;
            }
            StravaActivitesForAthleteQueueItem newQueueItem = new();
            newQueueItem.Athelete = queueItem.Athelete;
            newQueueItem.Count = queueItem.Count + 1;
            await outputServiceBus2.AddAsync(newQueueItem);
        }
    }
}