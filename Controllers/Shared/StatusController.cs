using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Models;

namespace WoodgroveDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class StatusController : ControllerBase
{

    private readonly IConfiguration _configuration;

    private readonly ILogger<CallbackController> _logger;
    private IMemoryCache _cache;


    public StatusController(ILogger<CallbackController> logger, IConfiguration configuration, IMemoryCache cache)
    {
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
    }

    [AllowAnonymous]
    [HttpGet("/api/status")]
    public Status Get()
    {
        // Get the current user's state ID from the user's session
        string state = this.HttpContext.Session.GetString("state");

        // If the state object was not found, return an error message
        if (string.IsNullOrEmpty(state))
        {
            return new Status
            {
                RequestStateId = "",
                RequestStatus = "error",
                Message = Constants.ErrorMessages.STATE_ID_NOT_FOUND
            };
        }

        // Try to read the request status object from the global cache using the state ID key
        if (_cache.TryGetValue(state, out string requestState))
        {
            try
            {
                Status status = Status.Parse(requestState);
                status.RequestStateId = state;

                // Process the status of the request
                status = this.HandleStatus(status);
                return status;
            }
            catch (Exception ex)
            {
                return new Status
                {
                    RequestStateId = "",
                    RequestStatus = "error",
                    Message = Constants.ErrorMessages.STATE_ID_CANNOT_DESERIALIZE + ex.Message
                };
            }
        }
        else
        {
            // If the request status object was not found in globle cach, return an error message
            return new Status
            {
                RequestStateId = state,
                RequestStatus = "error",
                Message = Constants.ErrorMessages.STATE_OBJECT_NOT_FOUND
            };
        }
    }

    private Status HandleStatus(Status status)
    {
        switch (status.RequestStatus)
        {
            case Constants.RequestStatus.REQUEST_CREATED:
                status.Message = Constants.RequestStatusMessage.REQUEST_CREATED;
                break;
            case Constants.RequestStatus.REQUEST_RETRIEVED:
                status.Message = Constants.RequestStatusMessage.REQUEST_RETRIEVED;
                break;
            case Constants.RequestStatus.ISSUANCE_ERROR:
                status.Message = Constants.RequestStatusMessage.ISSUANCE_ERROR;
                break;
            case Constants.RequestStatus.ISSUANCE_SUCCESSFUL:
                status.Message = Constants.RequestStatusMessage.ISSUANCE_SUCCESSFUL;
                break;
            case Constants.RequestStatus.PRESENTATION_ERROR:
                status.Message = Constants.RequestStatusMessage.ISSUANCE_ERROR;
                break;
            case Constants.RequestStatus.PRESENTATION_VERIFIED:
                status.Message = Constants.RequestStatusMessage.PRESENTATION_VERIFIED;
                break;
            // TBD add the claims to render
            //     callback = JsonConvert.DeserializeObject<CallbackEvent>(reqState["callback"].ToString());
            //     JObject resp = JObject.Parse(JsonConvert.SerializeObject(new
            //     {
            //         status = requestStatus,
            //         message = Constants.RequestStatusMessage.PRESENTATION_VERIFIED,
            //         type = callback.verifiedCredentialsData[0].type,
            //         claims = callback.verifiedCredentialsData[0].claims,
            //         subject = callback.subject,
            //         payload = callback.verifiedCredentialsData,
            //     }, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
            //     {
            //         NullValueHandling = NullValueHandling.Ignore
            //     }));
            //     if (null != callback.receipt && null != callback.receipt.vp_token)
            //     {
            //         JObject vpToken = GetJsonFromJwtToken(callback.receipt.vp_token[0]);
            //         JObject vc = GetJsonFromJwtToken(vpToken["vp"]["verifiableCredential"][0].ToString());
            //         resp.Add(new JProperty("jti", vc["jti"].ToString()));
            //     }
            //     if (!string.IsNullOrWhiteSpace(callback.verifiedCredentialsData[0].expirationDate))
            //     {
            //         resp.Add(new JProperty("expirationDate", callback.verifiedCredentialsData[0].expirationDate));
            //     }
            //     if (!string.IsNullOrWhiteSpace(callback.verifiedCredentialsData[0].issuanceDate))
            //     {
            //         resp.Add(new JProperty("issuanceDate ", callback.verifiedCredentialsData[0].issuanceDate));
            //     }
            //     result = resp;
            //     break;
            // TBD
            // case Constants.RequestStatus.SELFIE_TAKEN:
            //     callback = JsonConvert.DeserializeObject<CallbackEvent>(reqState["callback"].ToString());
            //     result = JObject.FromObject(new { status = requestStatus, message = "Selfie taken", photo = callback.photo });
            //     break;
            default:
                status.RequestStatus = Constants.RequestStatus.INVALID_REQUEST_STATUS;

                // TBD add the request status
                status.Message = Constants.RequestStatusMessage.INVALID_REQUEST_STATUS;
                break;
        }

        return status;
    }
}