using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models.AdminApi;
public class AdminApiResponse
{
    public List<Value> value { get; set; }

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
    /// Deserialize a JSON string into AdminApiResponse object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static AdminApiResponse Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<AdminApiResponse>(JsonString);
    }
}

public class Value
{
    public string id { get; set; }
    public string status { get; set; }
    public string issuedAtTimestamp { get; set; }
}