using System.Net;
using System.Text;

namespace CarLookup.Tests;

/// <summary>
/// Serves canned responses so the vPIC client can be tested without touching the network,
/// while recording the request URIs so tests can assert which endpoint was called.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<Uri> Requests { get; } = [];

    public static StubHttpMessageHandler ReturningJson(string json) =>
        new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    public static StubHttpMessageHandler ReturningStatus(HttpStatusCode statusCode) =>
        new(_ => new HttpResponseMessage(statusCode));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);

        return Task.FromResult(_responder(request));
    }
}
