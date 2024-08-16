using STS.WALAA.Models;
using System.Net;

namespace STS.WALAA.Utils;

public class NtlmClient : IDisposable
{
    public NetworkCredential Credential { get; private set; }

    private readonly ProxySettings proxySettings;
    private readonly HttpClient client;
    private bool disposed = false;

    public bool IsAuthenticated { get; private set; }

    public NtlmClient(NetworkCredential credential, ProxySettings proxySettings)
    {
        Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        this.proxySettings = proxySettings;
        client = CreateHttpClient();
    }

    private HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        Credentials = Credential,
        UseDefaultCredentials = false,
        PreAuthenticate = true,
        UseCookies = true,
        AllowAutoRedirect = true,

        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
    });

    public async Task<HttpResponseMessage> AuthenticateAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var xx = uri.GetLeftPart(UriPartial.Authority);
        var response = await client.GetAsync(uri);

        IsAuthenticated = response.IsSuccessStatusCode;

        return response;
    }

    public async Task<HttpResponseMessage> AuthenticateAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return await AuthenticateAsync(new Uri(url));
    }

    public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        if (!IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.RequestUri == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ObjectDisposedException.ThrowIf(disposed, nameof(request));

        return await client.SendAsync(request);
    }

    public bool IsDisposed => disposed;

    public void Dispose()
    {
        client?.Dispose();
        disposed = true;

        GC.SuppressFinalize(this);
    }
}