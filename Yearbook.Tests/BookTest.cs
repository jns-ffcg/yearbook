public class BookTest
{
    [Fact]
    public void Shall_create_a_book()
    {
        var book = new BookItem();
        book.Name = "book name";
        Assert.Equal("book name", book.Name);
    }

    [Fact]
    public void Shall_create_book_with_default_pages()
    {
        var book = new BookItem();
        book.Pages = 20;
        Assert.Equal(20, book.Pages);
    }
}