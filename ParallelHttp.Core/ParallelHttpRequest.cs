using System.Net.Http;
using System.Threading;

namespace ParallelHttp.Core
{
    public class ParallelHttpRequest
    {
        public object Reference { get; internal set; }
        public HttpRequestMessage Message { get; internal set; }
        public ParallelHttpClient.RequestCallback Callback { get; internal set; }
        public CancellationToken? CancellationToken { get; internal set; }

        public static ParallelHttpRequest From(HttpRequestMessage message, ParallelHttpClient.RequestCallback callback,
            object reference = null, CancellationToken? cancellationToken = null)
        {
            return new ParallelHttpRequest
            {
                Reference = reference ?? new object(),
                Message = message,
                Callback = callback,
                CancellationToken = cancellationToken
            };
        }
    }
}