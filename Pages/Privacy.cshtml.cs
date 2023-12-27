using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodgroveDemo.Helpers;

namespace WoodgroveDemo.Pages;

public class PrivacyModel : PageModel
{
    private TelemetryClient _telemetry;

    public PrivacyModel(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }

    public void OnGet()
    {
        // Send telemetry from this web app to Application Insights.
        AppInsightsHelper.TrackPage(_telemetry, this.Request);
    }
}

