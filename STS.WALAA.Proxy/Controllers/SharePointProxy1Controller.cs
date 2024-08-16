using System;
using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using STS.WALAA.Models;
using STS.WALAA.Proxy.Security;
using STS.WALAA.Utils;

namespace STS.WALAA.Controllers;

[ApiController]
//[Route("[controller]")]
public class SharePointProxy1Controller(
    ILogger<SharePointProxy1Controller> logger,
    IOptions<ProxySettings> proxySettingsOptions,
    AES256 aes256
) : ControllerBase
{
    private ProxySettings ProxySettings
    {
        get
        {
            if (proxySettingsOptions == null ||
                proxySettingsOptions.Value == null ||
                string.IsNullOrEmpty(proxySettingsOptions.Value.Domain) ||
                string.IsNullOrEmpty(proxySettingsOptions.Value.SharepointBaseUrl))
            {
                throw new Exception("Error while read configurations");
            }

            return proxySettingsOptions.Value;
        }
    }

    [AcceptVerbs("GET")]
    [Route("{*path}")]
    public async Task<IActionResult> GetAsync(string path, [FromQuery] Dictionary<string, string> queryParams)
    {
        if (string.IsNullOrEmpty(path))
        {
            logger.LogError("Empty API Url");
            return ProxyResponse.OnFailure(HttpStatusCode.BadRequest, "Empty API Url");
        }

        var sharePointApiEndpoint = new Uri(new Uri(ProxySettings.SharepointBaseUrl), HttpUtility.UrlDecode(path)).ToString(); // $"{ProxySettings.SharepointBaseUrl}/{HttpUtility.UrlDecode(path)}";

        //sharePointApiEndpoint = $"{sharePointApiEndpoint}{Request.QueryString}&sharepointrandom={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        try
        {
            var credential = new NetworkCredential()
            {
                UserName = ProxySettings.Username,
                Password = aes256.Decrypt(ProxySettings.Password),
                Domain = ProxySettings.Domain
            };

            using var ntlmClient = new NtlmClient(credential, ProxySettings);
            await ntlmClient.AuthenticateAsync(sharePointApiEndpoint);
            if (!ntlmClient.IsAuthenticated)
            {
                return ProxyResponse.OnFailure(HttpStatusCode.Unauthorized, $"not authorized");
            }

            return await new NtlmProxy(ntlmClient, Request, logger)
                .SendRequestAsync(sharePointApiEndpoint, queryParams, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogError(ex, "Authentication failed");
            return ProxyResponse.OnFailure(HttpStatusCode.Unauthorized, $"Authentication failed: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error occurred while proxying request to {Url}", sharePointApiEndpoint);
            return ProxyResponse.OnFailure(HttpStatusCode.BadGateway, $"Network error occurred while proxying request: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while proxying request to {Url}", sharePointApiEndpoint);
            return ProxyResponse.OnFailure(HttpStatusCode.InternalServerError, $"Error occurred while proxying request: {ex.Message}");
        }
    }


}