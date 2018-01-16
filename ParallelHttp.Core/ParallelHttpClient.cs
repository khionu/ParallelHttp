﻿using System;
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
            finally
            {
                semaphoreSlim?.Release();
            }
        }
    }
}