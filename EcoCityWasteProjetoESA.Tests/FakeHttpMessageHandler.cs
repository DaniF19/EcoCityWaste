using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EcoCityWasteProjetoESA.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = "[{\"lat\":\"38.5244\",\"lon\":\"-8.8882\"}]";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };

            return Task.FromResult(response);
        }
    }
}