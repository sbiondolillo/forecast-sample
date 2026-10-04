namespace Specs;

/// <summary>Answers every request from a canned answer, and records the requests.</summary>
public sealed class StubHandler : HttpMessageHandler
{
    private readonly Lock _gate = new();
    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>The answer to give to each request. Throws <see cref="HttpRequestException"/> by default.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage> Answer { get; set; } = _ => throw new HttpRequestException("No answer.");

    /// <summary>The requests that the handler got, as <c>GET https://...</c>.</summary>
    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_gate)
            {
                return [.. _requests.Select(r => $"{r.Method} {r.RequestUri?.AbsoluteUri}")];
            }
        }
    }

    /// <summary>A 200 answer with a JSON body.</summary>
    public static HttpResponseMessage Json(string body) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _requests.Add(request);
        }

        return Task.FromResult(Answer(request));
    }
}
