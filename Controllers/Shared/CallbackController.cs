using System.Net;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Helpers;
using WoodgroveDemo.Models;
using Microsoft.Extensions.Logging;

namespace WoodgroveDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class CallbackController : ControllerBase
{

    private readonly IConfiguration _configuration;
    private TelemetryClient _telemetry;
    private IMemoryCache _cache;
    protected readonly ILogger<CallbackController> _log;


    public CallbackController(TelemetryClient telemetry, IConfiguration configuration, IMemoryCache cache, ILogger<CallbackController> log)
    {
        _configuration = configuration;
        _cache = cache;
        _telemetry = telemetry;
        _log = log;
    }

    [AllowAnonymous]
    [HttpPost("/api/callback")]
    public async Task<ActionResult> HandleRequestCallback()
    {

        // Local variables
        PageViewTelemetry pageView = new PageViewTelemetry("Callback");
        bool rc = false;
        List<string> presentationStatus = new List<string>() { "request_retrieved", "presentation_verified", "presentation_error" };
        List<string> issuanceStatus = new List<string>() { "request_retrieved", "issuance_successful", "issuance_error" };
        List<string> selfieStatus = new List<string>() { "selfie_taken" };

        string state = "abcd", flow = "", body = "";

        try
        {
            // Get the requst body
            body = await new System.IO.StreamReader(this.Request.Body).ReadToEndAsync();

            _log.LogTrace("Reqeust body: " + body);

            // Parse the request body
            CallbackEvent callback = CallbackEvent.Parse(body);
            state = callback.state;

            // This endpoint is called by Microsoft Entra Verified ID which passes an API key.
            // Validate that the API key is valid.
            this.Request.Headers.TryGetValue("api-key", out var apiKey);
            if (_configuration["VerifiedID:ApiKey"] != apiKey)
            {
                return ErrorHandling(pageView, "Api-key wrong or missing", true, callback.state, callback.requestStatus);
            }

            // Add telemetry to the application insights
            pageView.Properties.Add("State", callback.state);
            pageView.Properties.Add("RequestId", callback.requestId);
            pageView.Properties.Add("RequestStatus", callback.requestStatus);

            // Get the current status from the cache and add the flow telemetry
            if (_cache.TryGetValue(callback.state, out string requestState))
            {
                Status currentStatus = Status.Parse(requestState);
                flow = currentStatus.Flow;
                pageView.Properties.Add("Flow", flow);
                pageView.Properties.Add("ExecutionTime", currentStatus.CalculateExecutionTime());
                pageView.Properties.Add("ExecutionSeconds", currentStatus.CalculateExecutionSeconds().ToString());
            }

            // Add the error message to the telemetry
            if (callback.requestStatus.Contains("_error"))
            {
                pageView.Properties.Add("Error", body);
                pageView.Properties.Add("Error_type", "Returned by Entra ID");
            }

            // Handle issuance, presentation adn selfie requests
            if (
                (presentationStatus.Contains(callback.requestStatus))
                || (issuanceStatus.Contains(callback.requestStatus))
                || selfieStatus.Contains(callback.requestStatus))
            {

                // Set the request status object into the global cache using the state ID key
                Status status = new Status()
                {
                    RequestStateId = callback.state,
                    RequestStatus = callback.requestStatus,
                    JsonPayload = body,
                    Flow = flow
                };

                // Add the indexed claim value to search and revoke the credential
                // Note, this code is relevant only to the gift card demo
                if (callback.requestStatus == Constants.RequestStatus.PRESENTATION_VERIFIED &&
                    callback.verifiedCredentialsData.Count == 1 && callback.verifiedCredentialsData[0].type.Contains(_configuration.GetSection("VerifiedID:RevokeCredentialsDemo:Type").Value))
                {
                    status.IndexedClaimValue = callback.verifiedCredentialsData[0].claims.id;

                    // In every Microsoft issued verifiable credential, there's a claim called credential Status indicates whether the credential is revoked.
                    // But to avoid a case where a user may reuse the card before Entra ID manages to complete the revolution, check its itnernal (in this app) status using the cache object
                    _cache.TryGetValue(status.IndexedClaimValue, out string credentialAppStatus);

                    if (credentialAppStatus == "revoked")
                    {
                        status.Message = "Certificate validation failed (Woodgrove app)";
                        status.RequestStatus = "presentation_error";
                        status.JsonPayload = "{ \"requestStatus\": \"presentation_error\", \"error\": { \"code\": \"tokenError\", \"message\": \"The presented verifiable credential with jti is revoked.\"}}";
                    }
                }

                // Add the status object to the cheace
                _cache.Set(callback.state, status.ToString(), DateTimeOffset.Now.AddMinutes(Constants.AppSettings.CACHE_EXPIRES_IN_MINUTES));

                _telemetry.TrackPageView(pageView);

                rc = true;
            }
            else
            {
                return ErrorHandling(pageView, $"Unknown request status '{callback.requestStatus}'", false, callback.state, callback.requestStatus);
            }

            if (!rc)
            {
                return ErrorHandling(pageView, body, false, callback.state, callback.requestStatus);
            }

            return new OkResult();
        }
        catch (Exception ex)
        {
            AppInsightsHelper.TrackError(_telemetry, this.Request, ex);
            return ErrorHandling(pageView, ex.Message, true, state);
        }
    }


    private BadRequestObjectResult ErrorHandling(PageViewTelemetry pageView, string errorMessage, bool internl, string state, string requestStatus = "")
    {

        // Add telemetry to the application insights
        pageView.Properties.Add("Error", errorMessage);

        if (internl)
            pageView.Properties.Add("Error_type", "Internal");
        else
            pageView.Properties.Add("Error_type", "Returned by Entra ID");

        // Track this page with the error
        _telemetry.TrackPageView(pageView);

        // Set the request status object into the global cache using the state ID key
        Status status = new Status()
        {
            RequestStateId = state,
            RequestStatus = requestStatus,
            JsonPayload = errorMessage
        };

        _cache.Set(state, status.ToString(), DateTimeOffset.Now.AddMinutes(Constants.AppSettings.CACHE_EXPIRES_IN_MINUTES));


        return BadRequest(new { error = "400", error_description = errorMessage });
    }

}