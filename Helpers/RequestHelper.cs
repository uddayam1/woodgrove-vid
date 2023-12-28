
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Models;
using WoodgroveDemo.Models.Issuance;
using WoodgroveDemo.Models.Manifest;
using WoodgroveDemo.Models.Presentation;

namespace WoodgroveDemo.Helpers;

public class RequestHelper
{
    public static string GetRequestHostName(HttpRequest request)
    {
        string scheme = "https";// : this.Request.Scheme;
        string originalHost = request.Headers["x-original-host"];
        string hostname = "";
        if (!string.IsNullOrEmpty(originalHost))
            hostname = string.Format("{0}://{1}", scheme, originalHost);
        else hostname = string.Format("{0}://{1}", scheme, request.Host);
        return hostname;
    }

    public static bool IsMobile(HttpRequest request)
    {
        string userAgent = request.Headers["User-Agent"];
        return (userAgent.Contains("Android") || userAgent.Contains("iPhone"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="manifestUrl"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="cache"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string GetCredentialManifest(string manifestUrl, IHttpClientFactory httpClientFactory, IMemoryCache cache, bool useCache)
    {
        string returnValue = string.Empty;

        // Check the required manifest Url parameter
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            throw new Exception("Manifest missing in config file");
        }

        // Try to read the manifest from the cache using its URL key
        if (!cache.TryGetValue(manifestUrl, out returnValue))
        {
            Console.WriteLine("Loading the manifest from Microsoft Entra ID");

            var client = httpClientFactory.CreateClient();
            HttpResponseMessage res = client.GetAsync(manifestUrl).Result;
            string response = res.Content.ReadAsStringAsync().Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"HTTP status {(int)res.StatusCode} retrieving manifest from URL {manifestUrl}");
            }

            ManifestToken manifestObj = ManifestToken.Parse(response);

            manifestObj.token = manifestObj.token.Replace("_", "/").Replace("-", "+").Split(".")[1];
            manifestObj.token = manifestObj.token.PadRight(4 * ((manifestObj.token.Length + 3) / 4), '=');
            returnValue = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(manifestObj.token));
            cache.Set(manifestUrl, returnValue, DateTimeOffset.Now.AddMinutes(60));
        }
        else
        {
            Console.WriteLine("Successfully read the manifest from the cache");
        }

        return returnValue;
    }

    /// <summary>
    /// Create an issuance request
    /// </summary>
    /// <param name="settings">The app settings configuration object</param>
    /// <param name="httpRequest">The HTTP request</param>
    /// <returns></returns>
    public static IssuanceRequest CreateIssuanceRequest(
        Settings settings,
        HttpRequest httpRequest,
        bool includePIN)
    {
        IssuanceRequest request = new IssuanceRequest()
        {
            includeQRCode = settings.UX.IncludeQRCode,
            authority = settings.EntraID.DidAuthority,
            registration = new Models.Issuance.Registration()
            {
                clientName = settings.UX.ClientName,
                purpose = settings.UX.Purpose
            },
            callback = new Models.Issuance.Callback()
            {
                url = settings.Api.URL(httpRequest),
                state = Guid.NewGuid().ToString(),
                headers = new Dictionary<string, string>() { { "api-key", settings.Api.ApiKey } }
            },
            type = settings.CredentialType,
            manifest = settings.ManifestUrl,
            pin = null
        };

        // If the purpose is empty string, change it to null
        if (request.registration.purpose == "")
        {
            request.registration.purpose = null;
        }

        int issuancePinCodeLength = settings.UX.IssuancePinCodeLength;

        // If pincode is required, set it up in the request
        if (includePIN && issuancePinCodeLength > 0 && (!RequestHelper.IsMobile(httpRequest)))
        {
            int pinCode = RandomNumberGenerator.GetInt32(1, int.Parse("".PadRight(issuancePinCodeLength, '9')));

            SetPinCode(request, string.Format("{0:D" + issuancePinCodeLength.ToString() + "}", pinCode));
        }

        // Set the experstion date to 60 days
        request.expirationDate = $"{Convert.ToDateTime(DateTime.Now.AddDays(60)).ToString("yyyy-MM-dd")}T23:59:59.000Z";
        return request;
    }

    /// <summary>
    /// Internal fuction used to set a PIN code for the issuance request 
    /// </summary>
    /// <param name="request">Issuance request object</param>
    /// <param name="pinCode">The PIN code to add</param>
    /// <returns></returns>
    private static IssuanceRequest SetPinCode(IssuanceRequest request, string pinCode = null)
    {
        if (string.IsNullOrWhiteSpace(pinCode))
        {
            request.pin = null;
        }
        else
        {
            request.pin = new Pin()
            {
                length = pinCode.Length,
                value = pinCode
            };
        }
        return request;
    }

    /// <summary>
    /// This method creates a presentation request object instance
    /// </summary>
    /// <param name="settings">The app settings configuration object</param>
    /// <param name="httpRequest">The HTTP request</param>
    /// <param name="acceptedIssuers">Array of accepted issuers</param>
    /// <returns>PresentationRequest</returns>
    public static PresentationRequest CreatePresentationRequest(
            Settings settings,
            HttpRequest httpRequest,
            string[] acceptedIssuers = null,
            bool faceCheck = false)
    {
        PresentationRequest request = new PresentationRequest()
        {
            includeQRCode = settings.UX.IncludeQRCode,
            authority = settings.EntraID.DidAuthority,
            registration = new Models.Presentation.Registration()
            {
                clientName = settings.UX.ClientName,
                purpose = settings.UX.Purpose
            },
            callback = new Models.Presentation.Callback()
            {
                url = settings.Api.URL(httpRequest),
                state = Guid.NewGuid().ToString(),
                headers = new Dictionary<string, string>() { { "api-key", settings.Api.ApiKey } }
            },
            includeReceipt = settings.UX.IncludeReceipt,
            requestedCredentials = new List<RequestedCredential>(),
        };
        if ("" == request.registration.purpose)
        {
            request.registration.purpose = null;
        }

        List<string> okIssuers;
        if (acceptedIssuers == null)
        {
            okIssuers = new List<string>(new string[] { settings.EntraID.DidAuthority });
        }
        else
        {
            okIssuers = new List<string>(acceptedIssuers);
        }
        bool allowRevoked = settings.UX.AllowRevoked;
        bool validateLinkedDomain = settings.UX.ValidateLinkedDomain;
        AddRequestedCredential(request, settings.CredentialType, okIssuers, allowRevoked, validateLinkedDomain, faceCheck);
        return request;
    }

    private static PresentationRequest AddRequestedCredential(PresentationRequest request,
                    string credentialType,
                    List<string> acceptedIssuers,
                    bool allowRevoked = false,
                    bool validateLinkedDomain = true,
                    bool faceCheck = false)
    {
        request.requestedCredentials.Add(new RequestedCredential()
        {
            type = credentialType,
            acceptedIssuers = (null == acceptedIssuers ? new List<string>() : acceptedIssuers),
            configuration = new Configuration()
            {
                validation = new Validation()
                {
                    allowRevoked = allowRevoked,
                    validateLinkedDomain = validateLinkedDomain
                }
            }
        });

        // Face check validation
        if (faceCheck)
        {
            // Receipt is not supported while doing faceCheck
            request.includeReceipt = false;

            request.requestedCredentials[0].configuration.validation.faceCheck = new FaceCheck()
            {
                sourcePhotoClaimName = "photo",
                matchConfidenceThreshold = 70
            };
        }



        return request;
    }
}