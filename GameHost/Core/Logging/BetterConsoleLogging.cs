using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Core.Logging
{
    public class BetterConsoleLogging : ILoggerProvider
    {
        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new AsyncProcessZLogger(categoryName, new BetterConsoleLoggingProcessor());
        }
    }

    public class BetterConsoleLoggingProcessor : IAsyncLogProcessor
    {
        private ZLoggerOptions options = new ZLoggerOptions { };
        
        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Post(IZLoggerEntry log)
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow}, {log.LogInfo.LogLevel}, {log.LogInfo.CategoryName}] {log.FormatToString(options, null)}");
            }
            finally
            {
                log.Return();
            }
        }
    }
}
