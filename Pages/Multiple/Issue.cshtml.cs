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

namespace WoodgroveDemo.Pages.Multiple
{
    public class IssueModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private IMemoryCache _cache;
        private TelemetryClient _telemetry;

        // UI elements
        public Settings _settings { get; set; }

        public IssueModel(TelemetryClient telemetry, IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _telemetry = telemetry;

            // Load the settings of this demo
            _settings = new Settings(configuration, "Multiple", false);
        }

        public void OnGet()
        {
            // Send telemetry from this web app to Application Insights.
            AppInsightsHelper.TrackPage(_telemetry, this.Request);

            // Get the credential manifest and deserialize
            _settings.ManifestContent = RequestHelper.GetCredentialManifest(_settings.ManifestUrl, _httpClientFactory, _cache, _settings.UseCache);
            Manifest manifest = Manifest.Parse(_settings.ManifestContent);
            _settings.CardDetails = manifest.Display;
            _settings.ManifestContent = manifest.ToHtml();
        }
    }
}


