using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models.Manifest;
public class ManifestToken
{
    public string token { get; set; }

    /// <summary>
    /// Serialize this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Deserialize a JSON string into ManifestToken object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static ManifestToken Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<ManifestToken>(JsonString);
    }
}

public class Manifest
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("display")]
    public Display Display { get; set; }

    [JsonPropertyName("input")]
    public Input Input { get; set; }

    [JsonPropertyName("iss")]
    public string Iss { get; set; }

    [JsonPropertyName("iat")]
    public long Iat { get; set; }

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
    /// Deserialize a JSON string into Manifest object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static Manifest Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<Manifest>(JsonString);
    }
}

public partial class Display
{
    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    [JsonPropertyName("contract")]
    public Uri Contract { get; set; }

    [JsonPropertyName("card")]
    public Card Card { get; set; }

    [JsonPropertyName("consent")]
    public Consent Consent { get; set; }

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public partial class Card
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("issuedBy")]
    public string IssuedBy { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; }

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; }

    [JsonPropertyName("logo")]
    public Logo Logo { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public partial class Logo
{
    [JsonPropertyName("uri")]
    public Uri Uri { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public partial class Claims
{
    [JsonPropertyName("vc.credentialSubject.firstName")]
    public VcCredentialSubjectStName VcCredentialSubjectFirstName { get; set; }

    [JsonPropertyName("vc.credentialSubject.lastName")]
    public VcCredentialSubjectStName VcCredentialSubjectLastName { get; set; }
}

public partial class VcCredentialSubjectStName
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }
}

public partial class Consent
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }
}

public partial class Input
{
    [JsonPropertyName("credentialIssuer")]
    public Uri CredentialIssuer { get; set; }

    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }

    [JsonPropertyName("attestations")]
    public Attestations Attestations { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public partial class Attestations
{
    [JsonPropertyName("idTokens")]
    public IdToken[] IdTokens { get; set; }
}

public partial class IdToken
{
    [JsonPropertyName("id")]
    public Uri Id { get; set; }

    [JsonPropertyName("encrypted")]
    public bool Encrypted { get; set; }

    [JsonPropertyName("claims")]
    public Claim[] Claims { get; set; }

    [JsonPropertyName("required")]
    public bool IdTokenRequired { get; set; }

    [JsonPropertyName("configuration")]
    public Uri Configuration { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; }
}

public partial class Claim
{
    [JsonPropertyName("claim")]
    public string ClaimClaim { get; set; }

    [JsonPropertyName("required")]
    public bool ClaimRequired { get; set; }

    [JsonPropertyName("indexed")]
    public bool Indexed { get; set; }
}
