using System.Net.Http;
using System.Threading;

namespace ParallelHttp.Core
{
    public class ParallelHttpRequest
    {
        public object Reference { get; internal set; }
        public HttpRequestMessage Message { get; internal set; }
        public ParallelHttpClient.RequestCallback Callback { get; internal set; }
        public CancellationToken CancellationToken { get; internal set; }
        
        public ParallelHttpRequest(HttpRequestMessage message, ParallelHttpClient.RequestCallback callback,
            object reference = default, CancellationToken cancellationToken = default)
        {
            Reference = reference ?? new object();
            Message = message;
            Callback = callback;
            CancellationToken = cancellationToken;
        }
    }
}