using System;
using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace YearbookApp
{

    public class GraphQL
    {

        private YearbookSchema _schema;
        public GraphQL(YearbookSchema schema)
        {
            _schema = schema;
        }
        [FunctionName("graphql")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            Console.WriteLine(new SchemaPrinter(_schema).Print());
            string query = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(query);
            var request = new GraphQLSerializer().Deserialize<GraphQLRequest>(query);
            var json = await _schema.ExecuteAsync(_ =>
            {
                _.Query = request.Query;
                _.Variables = request.Variables;
            });
            return new OkObjectResult(json);
        }
    }
}