using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHttp.Core
{
    public class ParallelHttpClient
    {
        private readonly SemaphoreSlim _globalSema;
        private readonly HttpClient _httpClient;

        public event Func<object, ExceptionInHttpRequestEventArgs, Task> ExceptionInHttpRequest;

        public ParallelHttpClient(HttpClient httpClient, int maxGlobalConcurrency = 0)
        {
            if (maxGlobalConcurrency < 0) throw new ArgumentOutOfRangeException(nameof(maxGlobalConcurrency));
            
            _httpClient = httpClient;

            if (maxGlobalConcurrency != 0) _globalSema = new SemaphoreSlim(maxGlobalConcurrency, maxGlobalConcurrency);
        }

        public Task<ParallelHttpResponse[]> AwaitRequests(params ParallelHttpRequest[] requests)
            => AwaitRequests(-1, requests);
        public Task<ParallelHttpResponse[]> AwaitRequests(int maxConcurrency, params ParallelHttpRequest[] requests)
        {
            var sema = maxConcurrency > -1 ? new SemaphoreSlim(maxConcurrency, maxConcurrency) : null;

            return Task.WhenAll(requests.Select(async x => await SendAsync(sema, x).ConfigureAwait(false)));
        }

        public Task FireRequests(params ParallelHttpRequest[] requests)
            => FireRequests(-1, requests);
        public Task FireRequests(int maxConcurrency, params ParallelHttpRequest[] requests)
        {
            var sema = maxConcurrency > -1 ? new SemaphoreSlim(maxConcurrency, maxConcurrency) : null;

            return Task.WhenAll(requests.Select(async x => { await SendAsync(sema, x).ConfigureAwait(false); }));
        }

        internal async Task<ParallelHttpResponse> SendAsync(SemaphoreSlim semaphoreSlim, ParallelHttpRequest req)
        {
            if (semaphoreSlim != null) await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            if (_globalSema != null) await _globalSema.WaitAsync().ConfigureAwait(false);

            try
            {
                var result = await _httpClient.SendAsync(req.Message, req.CancellationToken).ConfigureAwait(false);

                var res = new ParallelHttpResponse
                {
                    Reference = req.Reference,
                    ResponseMessage = result
                };

                req.Callback?.Invoke(res);

                return res;
            }
            catch (Exception ex)
            {
                var args = new ExceptionInHttpRequestEventArgs()
                {
                    Exception = ex,
                    Reference = req.Reference,
                };

                if (ExceptionInHttpRequest != null) await ExceptionInHttpRequest(this, args).ConfigureAwait(false);
            }
            finally
            {
                semaphoreSlim?.Release();
                _globalSema?.Release();
            }

            return null;
        }
    }
}