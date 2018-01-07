using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHttp.Core
{
    public delegate Task RequestCallback(ParallelHttpResponse response);
    
    public class ParallelHttpRequest
    {
        public object Reference { get; internal set; }
        public HttpRequestMessage Message { get; internal set; }
        public RequestCallback Callback { get; internal set; }
        public CancellationToken CancellationToken { get; internal set; }
        
        public ParallelHttpRequest(HttpRequestMessage message, RequestCallback callback,
            object reference = default, CancellationToken cancellationToken = default)
        {
            Reference = reference ?? new object();
            Message = message;
            Callback = callback;
            CancellationToken = cancellationToken;
        }
    }
}