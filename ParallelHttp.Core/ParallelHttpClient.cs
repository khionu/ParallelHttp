using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHttp.Core
{
    public class ParallelHttpClient
    {
        private readonly HttpClient _httpClient;

        public ParallelHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<ParallelHttpResponse[]> AwaitRequests(int maxConcurrency, params ParallelHttpRequest[] requests)
        {
            var sema = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            
            return Task.WhenAll(requests.Select(async x =>
            {
                return await SendAsync(sema, x).ConfigureAwait(false);
            }));
        }

        public Task FireRequests(int maxConcurrency, params ParallelHttpRequest[] requests)
        {
            var sema = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            
            return Task.WhenAll(requests.Select<ParallelHttpRequest, Task>(async x =>
            {
                await SendAsync(sema, x).ConfigureAwait(false);
            }));
        }

        internal async Task<ParallelHttpResponse> SendAsync(SemaphoreSlim semaphoreSlim, ParallelHttpRequest req)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var result = await _httpClient.SendAsync(req.Message, req.CancellationToken).ConfigureAwait(false);

                var res = new ParallelHttpResponse()
                {
                    Reference = req.Reference,
                    ResponseMessage = result,
                };
                
                req.Callback?.Invoke(res);

                return res;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}