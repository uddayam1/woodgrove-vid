
using System.Net;
using System.Security.Cryptography;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Models.Issuance;
using WoodgroveDemo.Models.Manifest;
using WoodgroveDemo.Models.Presentation;

namespace WoodgroveDemo.Helpers;

public class AppInsightsHelper
{

    public static void TrackPage(TelemetryClient Telemetry, HttpRequest request)
    {

        // For a page, check is route value
        if (request.RouteValues["page"] == null)
        {
            Telemetry.TrackPageView("Unknown");
            return;
        }

        // Get the page name in format of /area/page. 
        // The area may not exists for top level pages, such as the index and help.
        string PageRoute = request.RouteValues["page"].ToString();

        // Remove the slash at the beginning
        if (PageRoute.StartsWith("/"))
        {
            PageRoute = PageRoute.Remove(0, 1);
        }

        string[] parts = PageRoute.Split("/");

        PageViewTelemetry pageView = new PageViewTelemetry(PageRoute.Replace("/", "_"));

        // If the page name is under area, get the area and the page name
        // The area represents the verifiable credential scenario, while the page represents the action type; issue or present
        if (parts.Length > 1)
        {
            pageView.Properties.Add("Scenario", parts[0]);
            pageView.Properties.Add("Action", parts[1]);
        }

        // Get the web address from which the page has been requested
        if (request.Headers.ContainsKey("Referer"))
        {
            // Check the referer of the request
            string referer = request.Headers["Referer"].ToString();
            try
            {
                // Get the host name
                var uri = new System.Uri(referer);
                pageView.Properties.Add("Referral", uri.Host.ToLower());

                // Add the full URL
                pageView.Properties.Add("ReferralURL", referer);
            }
            catch (System.Exception ex)
            {
                pageView.Properties.Add("Referral", "Invalid");
            }
        }
        else
        {
            pageView.Properties.Add("Referral", "Unknown");
        }

        // Track the page view 
        Telemetry.TrackPageView(pageView);
    }
}