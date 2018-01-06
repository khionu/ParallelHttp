using System.Net.Http;

namespace ParallelHttp.Core
{
    public class ParallelHttpResponse
    {
        public object Reference { get; internal set; }
        public HttpResponseMessage ResponseMessage { get; internal set; }
    }
}