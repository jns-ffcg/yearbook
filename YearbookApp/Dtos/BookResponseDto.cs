public class BookResponseDto
{
    public string name;
    public int pages;

    public BookResponseDto(BookItem book)
    {
        name = book.Name;
        pages = book.Pages;
    }
}