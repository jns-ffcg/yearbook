using System.Collections;
using System.Text;
using InvoiceProcessor.Tests.Unit.TestCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Moq;
using Newtonsoft.Json;

namespace yearbook_tests;

public class BookTriggerTest
{
    [Fact]
    public async void FindAllBooks_shall_fetch_all_books()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmosResponse = new List<BookItem>();
        var book1 = new BookItem();
        book1.Id = "qwe";
        book1.Name = "Bok 1";
        book1.Pages = 22;
        cosmosResponse.Add(book1);

        request.Setup(x => x.ContentType).Returns("application/json");
        var response = (OkObjectResult)await YearbookApp.BookTrigger.FindAllBooks(request.Object, cosmosResponse, log.Object);

        var expected = @"[{""id"":""qwe"",""name"":""Bok 1"",""pages"":22}]";

        Assert.Single(response.ContentTypes);
        Assert.Equal("application/json", response.ContentTypes.ToList<string>()[0]);
        var books = (List<BookItem>)response.Value;
        string asJson = JsonConvert.SerializeObject(books);
        Assert.Equal(expected, asJson);
    }

    [Fact]
    public async void AddBook_shall_respond_with_400_if_body_has_invalid_json()
    {
        var cosmos = new Mock<IAsyncCollector<dynamic>>();
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{hoopla}"));
        request.Setup(x => x.Body).Returns(stream);
        var response = (BadRequestObjectResult)await YearbookApp.BookTrigger.AddBook(request.Object, cosmos.Object, log.Object);
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        var expectedJson = @"{""message"":""Invalid JSON""}";
        Assert.Equal(expectedJson, JsonConvert.SerializeObject(response.Value));
    }

    public static IEnumerable<object[]> GetDataForNameTest =>
        new List<object[]>
        {
            new object[] { "{hoopla}" , "Invalid JSON"},
            new object[] { @"{}", "Invalid body - missing 'name'" },
            new object[] { @"{""name"": ""q""}", "Invalid body - 'name' must be at least 5 characters long" },
    };

    [Theory]
    [MemberData(nameof(GetDataForNameTest))]
    public async void AddBook_shall_respond_with_400_if_body_is_incorrect(string payload, string expectedMessage)
    {
        var cosmos = new Mock<IAsyncCollector<dynamic>>();
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var payloadAsJson = JsonConvert.SerializeObject(payload);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        request.Setup(x => x.Body).Returns(stream);
        var response = await YearbookApp.BookTrigger.AddBook(request.Object, cosmos.Object, log.Object);
        Assert.IsType<BadRequestObjectResult>(response);
        var responseAsBadRequestObject = (BadRequestObjectResult)response;

        Assert.Equal(StatusCodes.Status400BadRequest, responseAsBadRequestObject.StatusCode);
        var expectedJson = $"{{\"message\":\"{expectedMessage}\"}}";
        Assert.Equal(expectedJson, JsonConvert.SerializeObject(responseAsBadRequestObject.Value));
    }

    [Fact]
    public async void AddBook_shall_add_a_book_in_cosmos_db()
    {
        var cosmos = new AsyncCollector<dynamic>();
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var payload = new
        {
            name = "Ny bok",
        };
        var payloadAsJson = JsonConvert.SerializeObject(payload);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payloadAsJson));
        request.Setup(x => x.Body).Returns(stream);
        Assert.Equal(0, cosmos.Items.Count);
        var response = (OkObjectResult)await YearbookApp.BookTrigger.AddBook(request.Object, cosmos, log.Object);
        Assert.IsType<CreateItemResponse>(response.Value);
        var createItemResponse = (CreateItemResponse)response.Value;

        Guid guidResult;
        Assert.True(Guid.TryParse(createItemResponse.Id, out guidResult));

        Assert.Equal(1, cosmos.Items.Count);
        dynamic storedItem = (dynamic)cosmos.Items.First();
        var name = storedItem.GetType().GetProperty("name").GetValue(storedItem, null);
        var pages = storedItem.GetType().GetProperty("pages").GetValue(storedItem, null);
        Assert.Equal("Ny bok", name);
        Assert.Equal(22, pages);
    }


    public class BookTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new BookItem { Name = "Bok 1", Pages = 20 } };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [Theory]
    [ClassData(typeof(BookTestData))]
    public async void FindBook_shall_respond_with_200_if_book_with_id_is_found(BookItem bookFromComos)
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmosResponse = new BookItem();

        request.Setup(x => x.ContentType).Returns("application/json");
        var response = (OkObjectResult)await YearbookApp.BookTrigger.FindBook(request.Object, bookFromComos, log.Object);
        Assert.Equal(200, response.StatusCode);
        var book = (BookItem)response.Value;
        Assert.Equal("Bok 1", book.Name);
    }

    [Fact]
    public async void FindBook_shall_respond_with_404_if_book_with_id_is_not_found()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        BookItem? cosmosResponse = null;
        request.Setup(x => x.ContentType).Returns("application/json");
        var response = (NotFoundObjectResult)await YearbookApp.BookTrigger.FindBook(request.Object, cosmosResponse, log.Object);
        Assert.Equal(404, response.StatusCode);
        var notFoundResponse = (DefaultResponse)response.Value;
        Assert.Equal("Book with ID not found", notFoundResponse.Message);
    }

    [Fact]
    public async void UpdateBook_shall_update_book_with_new_values()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmos = new AsyncCollector<dynamic>();
        var bookFromCosmos = new BookItem { Name = "Existing book", Pages = 123 };
        var payload = new
        {
            name = "Book with update name",
            pages = 999
        };
        var payloadAsJson = JsonConvert.SerializeObject(payload);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payloadAsJson));
        request.Setup(x => x.Body).Returns(stream);
        var response = (OkObjectResult)await YearbookApp.BookTrigger.UpdateBook(request.Object, "aa-bb-cc", bookFromCosmos, cosmos, log.Object);
        Assert.Equal(200, response.StatusCode);
        Assert.IsType<DefaultResponse>(response.Value);
        var responseBody = (DefaultResponse)response.Value;
        Assert.Equal("Book successfully updated", responseBody.Message);
        Assert.Single(cosmos.Items);
    }

    [Fact]
    public async void UpdateBook_shall_return_404_if_no_book_with_id_is_found()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmos = new AsyncCollector<dynamic>();
        BookItem? bookFromCosmos = null;
        var payload = new
        {
            name = "Book with update name",
            pages = 999
        };
        var id = "aa-bb-cc";
        var payloadAsJson = JsonConvert.SerializeObject(payload);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payloadAsJson));
        request.Setup(x => x.Body).Returns(stream);
        var response = await YearbookApp.BookTrigger.UpdateBook(request.Object, id, bookFromCosmos, cosmos, log.Object);
        Assert.IsType<NotFoundObjectResult>(response);
        var notFound = (NotFoundObjectResult)response;
        Assert.Equal(404, notFound.StatusCode);
        var responseBody = (DefaultResponse)notFound.Value;
        Assert.Equal("No book found with provided ID", responseBody.Message);
    }

    [Fact]
    public async void DeleteBook_shall_return_404_if_no_book_with_id_is_found()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmosClient = new Mock<CosmosClient>();
        BookItem? bookFromCosmos = null;
        var id = "aa-bb-cc";
        var response = await YearbookApp.BookTrigger.DeleteBook(request.Object, id, bookFromCosmos, cosmosClient.Object, log.Object);
        Assert.IsType<NotFoundObjectResult>(response);
        var notFound = (NotFoundObjectResult)response;
        Assert.Equal(404, notFound.StatusCode);
        var responseBody = (DefaultResponse)notFound.Value;
        Assert.Equal("No book found with provided ID", responseBody.Message);
    }

    [Fact]
    public async void DeleteBook_return_ok_when_book_is_deleted()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var cosmosContainer = new Mock<Container>();
        cosmosContainer.Setup(m => m.DeleteItemAsync<object>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()));
        var cosmosClient = new Mock<CosmosClient>();
        cosmosClient.Setup(x => x.GetContainer("Yearbook", "Books")).Returns(cosmosContainer.Object);
        var bookFromCosmos = new BookItem { Name = "Existing book", Pages = 123 };
        var id = "aa-bb-cc";
        var response = await YearbookApp.BookTrigger.DeleteBook(request.Object, id, bookFromCosmos, cosmosClient.Object, log.Object);
        Assert.IsType<OkObjectResult>(response);
        var ok = (OkObjectResult)response;
        Assert.Equal(200, ok.StatusCode);
        var responseBody = (DefaultResponse)ok.Value;
        Assert.Equal("Book successfully deleted", responseBody.Message);
    }
}
