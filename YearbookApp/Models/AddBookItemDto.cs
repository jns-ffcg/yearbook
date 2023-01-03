using Newtonsoft.Json;

public class AddBookItemDto
{
    [JsonProperty("name")]
    public string Name { get; set; }

}