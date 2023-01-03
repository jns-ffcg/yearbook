using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace YearbookApp
{
    public static class BookTrigger
    {
        [FunctionName("Find_all_books")]
        public static async Task<IActionResult> FindAllBooks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "book")] HttpRequest req,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString", PartitionKey = "foobar")] IEnumerable<BookItem> books,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var response = new OkObjectResult(books);
            response.ContentTypes.Add("application/json");
            return response;
        }

        [FunctionName("Add_book")]
        public static async Task<IActionResult> AddBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "book")] HttpRequest req,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString", PartitionKey = "foobar")] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("AddBook triggered by http request");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            AddBookItemDto book;
            try
            {
                book = JsonConvert.DeserializeObject<AddBookItemDto>(requestBody);
            }
            catch (Exception e)
            {
                log.LogInformation(e.ToString());
                var defaultResponse = new DefaultResponse("Invalid JSON");
                return new BadRequestObjectResult(defaultResponse);
            }

            if (book.Name == null)
            {
                var defaultResponse = new DefaultResponse("Invalid body - missing 'name'");
                return new BadRequestObjectResult(defaultResponse);
            }
            else if (book.Name.Length < 5)
            {
                var defaultResponse = new DefaultResponse("Invalid body - 'name' must be at least 5 characters long");
                return new BadRequestObjectResult(defaultResponse);
            }
            Console.WriteLine(book.Name);

            await documentsOut.AddAsync(new
            {
                id = System.Guid.NewGuid().ToString(),
                name = book.Name,
                pages = 22
            });

            var response = new OkObjectResult("Ok");
            response.ContentTypes.Add("application/json");
            return response;
        }
    }
}
