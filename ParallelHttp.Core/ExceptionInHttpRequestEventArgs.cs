using System;

namespace ParallelHttp.Core
{
    public class ExceptionInHttpRequestEventArgs
    {
        public object Reference { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}