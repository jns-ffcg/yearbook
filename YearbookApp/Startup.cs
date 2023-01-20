using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.DI;
using GraphQL.Validation.Complexity;
using GraphQL.MicrosoftDI;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


[assembly: FunctionsStartup(typeof(YearbookApp.Startup))]
namespace YearbookApp
{
    public class Startup : FunctionsStartup
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appSettings.json", true)
            .AddEnvironmentVariables()
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(s =>
            {
                var connectionString = Configuration["ConnectionString"];
                Console.WriteLine(connectionString);
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "Please specify a valid ConnectionString in the appSettings.json file or your Azure Functions Settings.");
                }

                return new CosmosClientBuilder(connectionString)
                    .Build();
            });

            // builder.Services.AddSingleton<IDocumentExecuter>(sp => new DocumentExecuter(
            //    sp.GetRequiredService<IDocumentBuilder>(),
            //    sp.GetRequiredService<IDocumentValidator>()));

            builder.Services.AddGraphQL(b => b
                .AddSchema<YearbookSchema>()
                .AddGraphTypes(typeof(YearbookSchema).Assembly));
        }
    }
}