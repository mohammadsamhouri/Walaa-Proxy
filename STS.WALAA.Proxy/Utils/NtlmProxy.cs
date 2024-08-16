using Microsoft.AspNetCore.Mvc;

using STS.WALAA.Controllers;
using STS.WALAA.Models;
using System.Net.Mime;
using System.Text;

namespace STS.WALAA.Utils;

public class NtlmProxy(
    NtlmClient ntlmClient,
    HttpRequest httpRequest,
    ILogger<SharePointProxy1Controller> logger
)
{
    private HttpRequestMessage CreateProxyRequest(string url, Dictionary<string, string> queryParams, string? requestBody = null)
    {
        var uri = new Uri(url);

        var method = new HttpMethod(httpRequest.Method);

        var contentType = httpRequest.ContentType;

        var request = new HttpRequestMessage(method, uri);

        var baseUri = new UriBuilder(new Uri(url))
        {
            Scheme = uri.Scheme,
            Host = uri.Host,
            Port = uri.Port,
        }.Uri;

        foreach (var header in httpRequest.Headers)
        {
            var headerKey = header.Key;
            var headerValues = header.Value;

            if (headerKey.Trim().Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                headerKey.Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else if (headerKey.Trim().Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Accept.ParseAdd(headerValues);
            }
            else if (headerKey.Trim().Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Host = uri.Host;
            }
            else if (headerKey.Trim().Equals("Referer", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Referrer = baseUri;
            }
            else if (headerKey.Trim().Equals("Origin", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation("Origin", baseUri.AbsoluteUri);
            }
            else
            {
                foreach (var value in headerValues)
                {
                    if (!request.Headers.TryAddWithoutValidation(headerKey, value))
                    {
                        logger.LogError("Failed to add header '{headerKey}' with value '{value}'", headerKey, value);
                        // return ProxyResponse.OnFailure(HttpStatusCode.BadRequest, $"Failed to add header '{headerKey}' with value '{value}'");
                    }
                }
            }
        }

        if ((method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch) && requestBody != null)
        {
            //    string requestBody;
            //    using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8);
            //    requestBody = await reader.ReadToEndAsync();

            request.Content = contentType != null ?
                new StringContent(requestBody, Encoding.UTF8, contentType) :
                new StringContent(requestBody, Encoding.UTF8);
        }

        return request;
    }

    public async Task<IActionResult> SendRequestAsync(string url, Dictionary<string, string> queryParams, object? requestBody = null)
    {
        var request = CreateProxyRequest(Uri.UnescapeDataString(url), queryParams, (string?)requestBody);

        HttpResponseMessage response = await ntlmClient.SendRequestAsync(request);
        

        request.Dispose();


        var responseContent = await HandleProxyResponseAsync(url, await response.Content.ReadAsStreamAsync(), response);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Request to {url} failed with status code {statusCode}.", request.RequestUri, response.StatusCode);
            if (responseContent is ContentResult result)
            {
                //return ProxyResponse.OnFailure(response.StatusCode, $"Request to {request.RequestUri} failed with status code {response.StatusCode}", result.Content);
                return new ContentResult
                {
                    Content = result.Content,
                    ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json",
                    StatusCode = (int?)response.StatusCode,
                };
            }

            return ProxyResponse.OnFailure(response.StatusCode, $"Request to {request.RequestUri} failed with status code {response.StatusCode}");
        }

        return responseContent;
    }

    private async Task<IActionResult> HandleProxyResponseAsync(string url, Stream contentStream, HttpResponseMessage response)
    {
        string contentType = response.Content.Headers?.ContentType?.MediaType ?? "";
        if (contentType == "application/pdf" || url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            contentType == "image/jpeg" || url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            contentType == "image/png" || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            contentType == "image/gif" || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
            contentType == "image/bmp" || url.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/msword" || url.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || url.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/vnd.ms-excel" || url.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || url.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/vnd.ms-powerpoint" || url.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase) ||
            contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation" || url.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
        {
            using var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream);
            var contentBytes = memoryStream.ToArray();

            contentType = string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType;

            return new FileContentResult(contentBytes, contentType);
        }
        else
        {
            return ProxyResponse.OnFailure((System.Net.HttpStatusCode)StatusCodes.Status404NotFound, $"file not found");
        }
    }
}
