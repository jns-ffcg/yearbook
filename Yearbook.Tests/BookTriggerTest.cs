using System.Text;
using InvoiceProcessor.Tests.Unit.TestCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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
        Assert.Equal("Ok", response.Value);
        Assert.Equal(1, cosmos.Items.Count);
        dynamic storedItem = (dynamic)cosmos.Items.First();
        var name = storedItem.GetType().GetProperty("name").GetValue(storedItem, null);
        var pages = storedItem.GetType().GetProperty("pages").GetValue(storedItem, null);
        Assert.Equal("Ny bok", name);
        Assert.Equal(22, pages);

    }
}
