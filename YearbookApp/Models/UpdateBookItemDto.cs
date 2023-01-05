using Newtonsoft.Json;

public class UpdateBookItemDto
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("pages")]
    public int Pages { get; set; }

}