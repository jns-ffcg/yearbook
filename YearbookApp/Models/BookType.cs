using GraphQL.Types;

namespace YearbookApp
{
    public class BookType : ObjectGraphType<Book>
    {
        public BookType()
        {
            Field(c => c.Id).Name("Id").Description("Unique id of the book");
            Field(c => c.Name).Name("Name").Description("Name of book");
            Field(c => c.Pages).Name("Pages").Description("Pages in the book");
        }

    }
}