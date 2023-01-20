using Newtonsoft.Json;

public class Book
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("pages")]
    public int Pages { get; set; }

}