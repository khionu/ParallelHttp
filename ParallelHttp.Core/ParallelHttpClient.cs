using System;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHttp.Core
{
    public class ParallelHttpClient
    {
        public delegate void ExceptionInHttpRequestEventHandler(object sender, ExceptionInHttpRequestEventArgs args);

        public delegate Task RequestCallback(ParallelHttpResponse response);

        private readonly HttpClient _httpClient;

        private readonly ConcurrentQueue<ParallelHttpRequest> _requests
            = new ConcurrentQueue<ParallelHttpRequest>();

        private readonly SemaphoreSlim _sema;

        private Task _backgroundWorker;

        public ParallelHttpClient(int maxParallelRequests, HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            
            _sema = new SemaphoreSlim(maxParallelRequests, maxParallelRequests);

            _backgroundWorker = Task.CompletedTask;
        }

        public event ExceptionInHttpRequestEventHandler ExceptionInHttpRequest;

        public void Enqueue(params ParallelHttpRequest[] requests)
        {
            foreach (var r in requests)
                _requests.Enqueue(r);

            if (!_backgroundWorker.IsCompleted)
                return;

            _backgroundWorker = DequeueLoopAsync();
        }

        private async Task DequeueLoopAsync()
        {
            while (_requests.TryDequeue(out var request))
            {
                await _sema.WaitAsync();

                try
                {
                    var result = await _httpClient.SendAsync(request.Message, request.CancellationToken);

                    var response = new ParallelHttpResponse
                    {
                        Reference = request.Reference,
                        ResponseMessage = result
                    };

                    await request.Callback.Invoke(response);
                }
                catch (Exception ex)
                {
                    ExceptionInHttpRequest?.Invoke(this, new ExceptionInHttpRequestEventArgs()
                    {
                        Reference = request.Reference,
                        Exception = ex,
                    });
                }
                finally
                {
                    _sema.Release();
                }
            }
        }
    }
}