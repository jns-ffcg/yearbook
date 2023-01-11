using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Cosmos;
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
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString")] IEnumerable<BookItem> books,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var response = new OkObjectResult(books);
            response.ContentTypes.Add("application/json");
            return response;
        }

        [FunctionName("Find_book")]
        public static async Task<IActionResult> FindBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "book/{id}")] HttpRequest req,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString", Id = "{id}", PartitionKey = "{id}")] BookItem book,
            ILogger log)
        {
            log.LogInformation($"Find book with id");
            if (book == null)
            {
                return new NotFoundObjectResult(new DefaultResponse("Book with ID not found"));
            }
            var response = new OkObjectResult(book);
            return response;
        }

        [FunctionName(nameof(AddBook))]
        public static async Task<IActionResult> AddBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "book")] HttpRequest req,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString")] IAsyncCollector<dynamic> documentsOut,
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

            var bookId = System.Guid.NewGuid().ToString();
            await documentsOut.AddAsync(new
            {
                id = bookId,
                name = book.Name,
                pages = 22,
            });

            CreateItemResponse createItemResponse = new();
            createItemResponse.Id = bookId;
            var response = new OkObjectResult(createItemResponse);
            response.ContentTypes.Add("application/json");
            return response;
        }

        [FunctionName("Update_book")]

        public static async Task<IActionResult> UpdateBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "book/{id}")] HttpRequest req,
            string id,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString", Id = "{id}", PartitionKey = "{id}")] BookItem bookFromCosmos,
            [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString")] IAsyncCollector<dynamic> documentsOut,
            ILogger log
            )
        {
            log.LogInformation($"Updating Book with id {id}");
            if (bookFromCosmos == null)
            {
                var notFoundResponse = new DefaultResponse("No book found with provided ID");
                return new NotFoundObjectResult(notFoundResponse);
            }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateBookItemDto book;
            try
            {
                book = JsonConvert.DeserializeObject<UpdateBookItemDto>(requestBody);
            }
            catch (Exception e)
            {
                log.LogInformation(e.ToString());
                var defaultResponse = new DefaultResponse("Invalid JSON");
                return new BadRequestObjectResult(defaultResponse);
            }

            await documentsOut.AddAsync(new
            {
                id = id,
                name = book.Name,
                pages = book.Pages,
            });

            var response = new OkObjectResult(new DefaultResponse("Book successfully updated"));
            return response;
        }

        [FunctionName(nameof(DeleteBook))]
        public static async Task<IActionResult> DeleteBook(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "book/{id}")] HttpRequest req,
           string id,
           [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString", Id = "{id}", PartitionKey = "{id}")] BookItem book,
           [CosmosDB(databaseName: "Yearbook", containerName: "Books", Connection = "ConnectionString")] CosmosClient client,
           ILogger log)
        {
            log.LogInformation($"Delete book with id {id}");
            if (book == null)
            {
                return new NotFoundObjectResult(new DefaultResponse("No book found with provided ID"));
            }
            var container = client.GetContainer("Yearbook", "Books");

            await container.DeleteItemAsync<BookItem>(id, new PartitionKey(id));
            var response = new OkObjectResult(new DefaultResponse("Book successfully deleted"));
            return response;
        }
    }
}
