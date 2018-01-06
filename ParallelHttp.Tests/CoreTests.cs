using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ParallelHttp.Core;
using RichardSzalay.MockHttp;
using Xunit;

namespace ParallelHttp.Tests
{
    public class CoreTests
    {
        private const string UnitTestUrlString = "http://localhost/xunit"; 

        [Fact]
        public void OnCallbackException_PassExceptionToEvent()
        {
            // Arrange
            
            const string thisShouldBeCaughtAndHandled = "This should be caught and handled";
            
            var testRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString),
            };

            Task Callback(ParallelHttpResponse response) => throw new Exception(thisShouldBeCaughtAndHandled);

            var testRequest = new ParallelHttpRequest(testRequestMessage, Callback);

            var mockHttpMessageHandler = new RichardSzalay.MockHttp.MockHttpMessageHandler();
                
            mockHttpMessageHandler.When(HttpMethod.Get, UnitTestUrlString)
                .Respond("application/json", "{ }");

            var mockHttpClient = mockHttpMessageHandler.ToHttpClient();
            
            var testClient = new ParallelHttpClient(1, mockHttpClient);

            var exceptionHandled = false;

            testClient.ExceptionInHttpRequest += (s, e) => { exceptionHandled = true; };
            
            // Act
            
            testClient.Enqueue(testRequest);

            Task.Delay(500).GetAwaiter().GetResult();
            
            // Assert
            
            Assert.True(exceptionHandled, "Exception is never thrown");
        }

        [Fact]
        public void OnFullLoad_SemaphoresHold()
        {
            // Arrange
            
            const int concurrency = 3;
            
            var blockingRequestMessage_first = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=4000"),
            };
            
            var blockingRequestMessage_second = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=4050"),
            };
            
            var sequence_zero = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=100"),
            };
            
            var sequence_one = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=175"),
            };
            
            var sequence_two = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=350"),
            };
            
            var sequence_three = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(UnitTestUrlString + "?timeout=575"),
            };

            var returnOrder = new List<object>();

            Task Callback(ParallelHttpResponse res)
            {
                returnOrder.Add(res.Reference);
                
                return Task.CompletedTask;
            };
            
            var requestSet = new ParallelHttpRequest[6];
            
            requestSet[0] = new ParallelHttpRequest(blockingRequestMessage_first, Callback);
            requestSet[1] = new ParallelHttpRequest(blockingRequestMessage_second, Callback);
            requestSet[2] = new ParallelHttpRequest(sequence_zero, Callback);
            requestSet[3] = new ParallelHttpRequest(sequence_one, Callback);
            requestSet[4] = new ParallelHttpRequest(sequence_two, Callback);
            requestSet[5] = new ParallelHttpRequest(sequence_three, Callback);

            var expectedOrder = new object[6];

            expectedOrder[0] = requestSet[2].Reference;
            expectedOrder[1] = requestSet[3].Reference;
            expectedOrder[2] = requestSet[4].Reference;
            expectedOrder[3] = requestSet[5].Reference;
            expectedOrder[4] = requestSet[0].Reference;
            expectedOrder[5] = requestSet[1].Reference;
            
            var mockHttpMessageHandler = new RichardSzalay.MockHttp.MockHttpMessageHandler();

            mockHttpMessageHandler.When(HttpMethod.Get, UnitTestUrlString + "*")
                .With(x =>
                {
                    var query = x.RequestUri.Query;
                    if (!query.StartsWith("timeout="))
                        return false;

                    query = query.Replace("timeout=", "");
                    if (!int.TryParse(query, out var timeout))
                        return false;

                    Task.Delay(timeout).GetAwaiter().GetResult();
                    return true;
                })
                .Respond("application/json", "{ }");
            
            var client = new ParallelHttpClient(3, mockHttpMessageHandler.ToHttpClient());
            
            // Act
            
            client.Enqueue(requestSet);
            
            Task.Delay(5000).GetAwaiter().GetResult();
            
            // Assert

            var i = 0;
            
            Assert.All(returnOrder, x =>
            {
                Assert.Same(x, expectedOrder[i]);
                i++;
            });
        }
    }
}