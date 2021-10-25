using System;
using revghost.Injection;
using revghost.Injection.Dynamic;

namespace revghost.Utility;

public enum HostLogLevel
{
    Info,
    Warn,
    Error
}

public delegate void HostLoggingOutput(HostLogLevel level, string line, string source, string theme);

public class HostLogger
{
    public static HostLoggingOutput Output { get; set; } = (level, line, source, theme) =>
    {
        if (string.IsNullOrEmpty(source))
            source = "global";
        if (string.IsNullOrEmpty(theme))
            theme = ":";

        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{level}|{source}|{theme}: {line}");
    };

    public readonly string SourceName;

    public HostLogger(string sourceName)
    {
        SourceName = sourceName;
    }

    public void Info<T>(T str, string theme = "") => Output.Info(str.ToString(), SourceName, theme);
    public void Warn<T>(T str, string theme = "") => Output.Warn(str.ToString(), SourceName, theme);
    public void Error<T>(T str, string theme = "") => Output.Error(str.ToString(), SourceName, theme);
}

public static class HostLoggingOutputExtension
{
    public static void Info<T>(this HostLoggingOutput output, T str, string sourceName = "", string theme = "")
    {
        output(HostLogLevel.Info, str.ToString(), sourceName, theme);
    }

    public static void Warn<T>(this HostLoggingOutput output,T str, string sourceName = "", string theme = "")
    {
        output(HostLogLevel.Warn, str.ToString(), sourceName, theme);
    }

    public static void Error<T>(this HostLoggingOutput output, T str, string sourceName = "", string theme = "")
    {
        output(HostLogLevel.Error, str.ToString(), sourceName, theme);
    }
}

public class TransientHostLogger : DynamicDependency<HostLogger>
{
    public override HostLogger CreateT<TContext>(TContext context)
    {
        var sourceName = string.Empty;
        if (context.TryGet(out object source))
        {
            sourceName = source.GetType().Namespace + source.GetType().Name;
        }

        return new HostLogger(sourceName);
    }
}