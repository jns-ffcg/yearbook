using Newtonsoft.Json;

public class DefaultResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }

    public DefaultResponse(string message)
    {
        Message = message;
    }
}