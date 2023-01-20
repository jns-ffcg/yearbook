using System.Collections.Generic;
using System.Text.Json.Serialization;
using GraphQL.SystemTextJson;
using Newtonsoft.Json;

namespace YearbookApp
{
    public class MyGraphQLRequest
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public string Variables { get; set; }
    }
}