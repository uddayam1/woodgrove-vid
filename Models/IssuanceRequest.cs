using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models.Issuance;
public class IssuanceRequest
{
    public string authority { get; set; }
    public bool includeQRCode { get; set; }
    public Registration registration { get; set; }
    public Callback callback { get; set; }
    public string type { get; set; }
    public string manifest { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Pin pin { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string> claims { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string expirationDate { get; set; } // format "2024-10-20T14:52:39.043Z"

    /// <summary>
    /// Serialize this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Serialize this object into HTML string
    /// </summary>
    /// <returns></returns>
    public string ToHtml()
    {
        return this.ToString().Replace("\r\n", "<br>").Replace(" ", "&nbsp;");
    }

    /// <summary>
    /// Deserialize a JSON string into CallbackEvent object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static IssuanceRequest Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<IssuanceRequest>(JsonString);
    }
}

/// <summary>
/// Registration - used in both issuance and presentation to give the app a display name
/// </summary>
public class Registration
{
    public string clientName { get; set; }
    public string purpose { get; set; }
}

/// <summary>
/// Callback - defines where and how we want our callback.
/// url - points back to us
/// state - something we pass that we get back in the callback event. We use it as a correlation id
/// headers - any additional HTTP headers you want to pass to the VC Client API. 
/// The values you pass will be returned, as HTTP Headers, in the callback
public class Callback
{
    public string url { get; set; }
    public string state { get; set; }
    public Dictionary<string, string> headers { get; set; }
}

/// <summary>
/// Pin - if issuance involves the use of a pin code. The 'value' attribute is a string so you can have values like "0907"
/// </summary>
public class Pin
{
    public string value { get; set; }
    public int length { get; set; }
}
