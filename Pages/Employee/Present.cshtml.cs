using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Helpers;
using WoodgroveDemo.Models;
using WoodgroveDemo.Models.Manifest;
using WoodgroveDemo.Models.Presentation;

namespace WoodgroveDemo.Pages.Employee
{
    public class PresentModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private IMemoryCache _cache;
        private TelemetryClient _telemetry;

        // UI elements
        public Settings _settings { get; set; }

        public PresentModel(TelemetryClient telemetry, IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _telemetry = telemetry;

            // Load the settings of this demo
            _settings = new Settings(configuration,  "Employee", true);
        }

        public void OnGet()
        {
            // Send telemetry from this web app to Application Insights.
            AppInsightsHelper.TrackPage(_telemetry, this.Request);

            _settings.ManifestContent = RequestHelper.GetCredentialManifest(_settings.ManifestUrl, _httpClientFactory, _cache, _settings.UseCache);
            Manifest manifest = Manifest.Parse(_settings.ManifestContent);
            _settings.CardDetails = manifest.Display;
            _settings.ManifestContent = manifest.ToHtml();
        }
    }
}

