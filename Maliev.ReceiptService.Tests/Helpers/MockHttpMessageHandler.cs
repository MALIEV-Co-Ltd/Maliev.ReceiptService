using System.Net;

namespace Maliev.ReceiptService.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsyncFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc)
    {
        _sendAsyncFunc = sendAsyncFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _sendAsyncFunc(request, cancellationToken);
    }

    public static MockHttpMessageHandler Create(HttpStatusCode statusCode, HttpContent? content = null)
    {
        return new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                response.Content = content;
            }
            return Task.FromResult(response);
        });
    }
}
