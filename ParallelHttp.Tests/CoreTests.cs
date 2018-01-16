using System;
using System.Collections.Generic;
using System.Linq;
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

            var mockHttpMessageHandler = new MockHttpMessageHandler();
                
            mockHttpMessageHandler.When(HttpMethod.Get, UnitTestUrlString)
                .Respond("application/json", "{ }");

            var mockHttpClient = mockHttpMessageHandler.ToHttpClient();
            
            var testClient = new ParallelHttpClient(mockHttpClient, 1);

            var exceptionHandled = false;

            testClient.ExceptionInHttpRequest += (s, e) => { exceptionHandled = true; return Task.CompletedTask; };
            
            // Act
            
            testClient.AwaitRequests(testRequest).GetAwaiter().GetResult();
            
            // Assert
            
            Assert.True(exceptionHandled, "Exception is never thrown");
        }

        [Fact]
        public void OnLargeRequests_IsAsync()
        {
            // Arrange

            var requests = new List<ParallelHttpRequest>();
            var responses = new List<int>();

            for (var i = 0; i < 8; i++)
            {
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(UnitTestUrlString + $"/{i}"),
                };
                
                var parallelReq = new ParallelHttpRequest(httpReq, Callback, i);
                
                requests.Add(parallelReq);
            }
            
            var mockHttpMessageHandler = new MockHttpMessageHandler();

            mockHttpMessageHandler.When(UnitTestUrlString + '*')
                .Respond(Response);
            
            var testClient = new ParallelHttpClient(mockHttpMessageHandler.ToHttpClient());
            
            // Act

            testClient.AwaitRequests(3, requests.ToArray()).GetAwaiter().GetResult();
            
            // Assert

            var actual = responses.ToArray();

            var shouldNotBe = responses.OrderBy(x => x).ToArray();
            
            Assert.NotEqual(shouldNotBe, actual);
            
            // Misc

            Task Callback(ParallelHttpResponse res)
            {
                responses.Add((int)res.Reference);
                return Task.CompletedTask;
            }
            
            async Task<HttpResponseMessage> Response(HttpRequestMessage msg)
            {
                var reference = int.Parse(msg.RequestUri.PathAndQuery.Last().ToString()); 

                await Task.Delay(reference % 3 == 0 ? 4000 : 200);
                
                responses.Add(reference);
                
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}