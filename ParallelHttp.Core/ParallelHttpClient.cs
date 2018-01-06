using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHttp.Core
{
    public class ParallelHttpClient
    {
        public delegate Task RequestCallback(ParallelHttpResponse response);

        private readonly HttpClient _httpClient;

        private readonly ConcurrentQueue<ParallelHttpRequest> _requests
            = new ConcurrentQueue<ParallelHttpRequest>();

        private readonly SemaphoreSlim _sema;

        private Task _backgroundWorker;

        public ParallelHttpClient(int maxParallelRequests)
            : this(maxParallelRequests, new HttpClient())
        {
        }

        public ParallelHttpClient(int maxParallelRequests, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _sema = new SemaphoreSlim(maxParallelRequests, maxParallelRequests);

            _backgroundWorker = Task.CompletedTask;
        }

        public void Enqueue(params ParallelHttpRequest[] requests)
        {
            foreach (var r in requests)
                _requests.Enqueue(r);

            if (!_backgroundWorker.IsCompleted)
                return;

            (_backgroundWorker = DequeueLoopAsync()).Start();
        }

        private async Task DequeueLoopAsync()
        {
            while (_requests.TryDequeue(out var request))
            {
                await _sema.WaitAsync();

                try
                {
                    HttpResponseMessage result;

                    if (request.CancellationToken.HasValue)
                        result = await _httpClient.SendAsync(request.Message, request.CancellationToken.Value);
                    else
                        result = await _httpClient.SendAsync(request.Message);


                    var response = new ParallelHttpResponse
                    {
                        Reference = request.Reference,
                        ResponseMessage = result
                    };

                    await request.Callback.Invoke(response);
                }
                finally
                {
                    _sema.Release();
                }
            }
        }
    }
}