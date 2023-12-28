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
using WoodgroveDemo.Models.Issuance;
using System.Net.Http.Headers;
using System.Text;

namespace WoodgroveDemo.Controllers.SelfIssued;

[ApiController]
[Route("[controller]")]
public class IssueController : ControllerBase
{
    protected readonly IConfiguration _Configuration;
    protected TelemetryClient _Telemetry;
    protected IMemoryCache _Cache;
    protected Settings _Settings { get; set; }
    protected readonly IHttpClientFactory _HttpClientFactory;
    public ResponseToClient _Response { get; set; } = new ResponseToClient();

    public IssueController(TelemetryClient telemetry, IConfiguration configuration, IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
        _Configuration = configuration;
        _Cache = cache;
        _Telemetry = telemetry;
        _HttpClientFactory = httpClientFactory;

        // Load the settings of this demo
        _Settings = new Settings(configuration, "SelfIssued", false);
    }

    [AllowAnonymous]
    [HttpPost("/api/SelfIssued/Issue")]
    public async Task<ResponseToClient> Post()
    {
        // Send telemetry from this web app to Application Insights.
        AppInsightsHelper.TrackApi(_Telemetry, this.Request);

        // Clear session
        this.HttpContext.Session.Clear();

        try
        {
            // Create an issuance request object
            IssuanceRequest request = RequestHelper.CreateIssuanceRequest(_Settings, this.Request, false);

            // Serialize the request object to JSON string format
            _Response.RequestPayload = request.ToString();

            // Prepare the HTTP request with the Bearer access token and the request body
            var client = _HttpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await MsalAccessTokenHandler.AcquireToken(_Settings, _Cache));

            // Call the Microsoft Entra ID request endpoint
            HttpResponseMessage response = await client.PostAsync(
                _Settings.RequestUrl,
                new StringContent(_Response.RequestPayload, Encoding.UTF8, "application/json"));

            // Serialize the request object to HTML format
            _Response.RequestPayload = request.ToHtml();

            // Read the response content
            _Response.ResponseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Created)
            {
                IssuanceResponse issuanceResponse = IssuanceResponse.Parse(_Response.ResponseBody);
                _Response.ResponseBody = issuanceResponse.ToHtml();
                _Response.QrCodeUrl = issuanceResponse.url;

                // Add the state ID to the user's session object 
                this.HttpContext.Session.SetString("state", request.callback.state);

                // Add the global cache with the request status
                Status status = new Status()
                {
                    RequestStateId = request.callback.state,
                    RequestStatus = Constants.RequestStatus.REQUEST_CREATED,
                    Flow = "Issuance"
                };

                _Cache.Set(request.callback.state, status.ToString(), DateTimeOffset.Now.AddMinutes(Constants.AppSettings.CACHE_EXPIRES_IN_MINUTES));
            }
            else
            {
                _Response.ErrorMessage = _Response.ResponseBody;
                _Response.ErrorUserMessage = ResponseError.Parse(_Response.ResponseBody).GetUserMessage();
            }
        }
        catch (Exception ex)
        {
            AppInsightsHelper.TrackError(_Telemetry, this.Request, ex);
            _Response.ErrorMessage = ex.Message;
        }

        return _Response;
    }
}
