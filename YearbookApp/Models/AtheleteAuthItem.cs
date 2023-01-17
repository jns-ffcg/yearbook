using Newtonsoft.Json;

public class AtheleteAuthItem
{
    [JsonProperty("id")]
    public int AtheleteId;

    [JsonProperty("expires_at")]
    public int ExpiresAt;

    [JsonProperty("expires_in")]
    public int ExpiresIn;

    [JsonProperty("refresh_token")]
    public string RefreshToken;

    [JsonProperty("access_token")]
    public string AccessToken;
}