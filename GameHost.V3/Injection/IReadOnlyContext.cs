using System;

namespace GameHost.V3.Injection
{
    public interface IReadOnlyContext
    {
        bool TryGet(Type type, out object obj);
    }

    public static class ReadOnlyContextExtensions
    {
        public static bool TryGet<TContext, T>(this TContext ctx, out T obj)
            where TContext : IReadOnlyContext
        {
            if (ctx.TryGet(typeof(T), out var result))
            {
                obj = (T) result;
                return true;
            }

            obj = default;
            return false;
        }
    }
}