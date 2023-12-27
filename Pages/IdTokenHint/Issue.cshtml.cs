using System.Net;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodgroveDemo.Helpers;
using System.Net.Http.Headers;
using System.Text;
using WoodgroveDemo.Models.Manifest;
using WoodgroveDemo.Models.Issuance;
using WoodgroveDemo.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace WoodgroveDemo.Pages.IdTokenHint;
[IgnoreAntiforgeryToken(Order = 1001)]
public class IssueModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private IMemoryCache _cache;
    private TelemetryClient _telemetry;

    // UI elements
    public Settings _settings { get; set; }

    [BindProperty]
    public string FirstName { get; set; }
    [BindProperty]
    public string LastName { get; set; }
    [BindProperty]
    public string Photo { get; set; }

    public IssueModel(TelemetryClient telemetry, IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _telemetry = telemetry;

        // Load the settings of this demo
        _settings = new Settings(configuration, "IdTokenHint", false);
    }



    public void OnGet(string firstName = "", string lastName = "")
    {
        // Send telemetry from this web app to Application Insights.
        AppInsightsHelper.TrackPage(_telemetry, this.Request);

        if ((!string.IsNullOrEmpty(firstName)) && !(string.IsNullOrEmpty(lastName)))
        {
            LastName = lastName;
            FirstName = firstName;
        }

        // Get the credential manifest and deserialize
        _settings.ManifestContent = RequestHelper.GetCredentialManifest(_settings.ManifestUrl, _httpClientFactory, _cache, _settings.UseCache);
        Manifest manifest = Manifest.Parse(_settings.ManifestContent);

        _settings.CardDetails = manifest.Display;
        _settings.ManifestContent = manifest.ToHtml();
    }
}

