using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;

namespace YearbookApp
{
    public class Server
    {
        private ISchema schema { get; set; }

        public Server()
        {
            //         this.schema = Schema.For(@"
            //       type Book {
            //         id: ID
            //         name: String,
            //         pages: String
            //       }

            //       type Query {
            //           books: [Book]
            //       }
            //   ", _ =>
            //         {
            //             _.Types.Include<Query>();
            //         });

        }

        // public async Task<string> QueryAsync(string query)
        // {
        //     var result = await new DocumentExecuter().ExecuteAsync(_ =>
        //     {
        //         _.Schema = schema;
        //         _.Query = query;
        //     });

        //     if (result.Errors != null)
        //     {
        //         return result.Errors[0].Message;
        //     }
        //     else
        //     {
        //         var writer = new GraphQL.NewtonsoftJson.DocumentWriter();
        //         return "tjo...";
        //     }
        // }
    }
}
//   type Book {
//     id: ID
//     name: String,
//     pages: String
//   }

//   input BookInput {
//     name: String
//     id: ID
//   }

//   type Mutation {
//     addBook(input: BookInput): Book
//     updateBook(input: BookInput ): Book
//     removeBook(id: ID): String
//   }

//   type Query {
//       books: [Book]
//       book(id: ID): Book
//   }