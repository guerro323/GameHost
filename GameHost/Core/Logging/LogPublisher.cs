using System;
using System.Threading.Tasks;
using DefaultEcs;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Core.Logging
{
    public class LogPublisher : IPublisher, ILoggerProvider
    {
        private IPublisher parent;

        public LogPublisher()
        {
            // i'm kinda lazy
            parent = new World();
        }

        public void Dispose()
        {
            parent.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new AsyncProcessZLogger(categoryName, new Processor {publisher = this});
        }

        public IDisposable Subscribe<T>(MessageHandler<T> action)
        {
            return parent.Subscribe<T>();
        }

        public void Publish<T>(in T message)
        {
            parent.Publish(in message);
        }

        public class Processor : IAsyncLogProcessor
        {
            private ZLoggerOptions options = new ZLoggerOptions();
            public  IPublisher     publisher;

            public ValueTask DisposeAsync()
            {
                return default;
            }

            public void Post(IZLoggerEntry log)
            {
                try
                {
                    publisher.Publish(log);
                }
                finally
                {
                    log.Return();
                }
            }
        }
    }
}
