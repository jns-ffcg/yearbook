using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using Microsoft.Azure.Cosmos;
using System.Linq;
using System.Threading.Tasks;

namespace YearbookApp
{
    public class YearbookQuery : ObjectGraphType<object>
    {
        public YearbookQuery(CosmosClient cosmosClient)
        {
            Field<ListGraphType<BookType>>("books").Resolve(context =>
            {
                var container = cosmosClient.GetContainer("Yearbook", "Books");
                return container.GetItemLinqQueryable<Book>(true);
            });

            Field<BookType>("book").Argument<IdGraphType>("id").ResolveAsync(async context =>
            {
                var id = context.GetArgument<string>("id");
                var container = cosmosClient.GetContainer("Yearbook", "Books");
                Book book = await container.ReadItemAsync<Book>(id, new PartitionKey(id));
                return book;
            });
        }
    }
}