using System;

namespace GameHost.V3.Utility
{
    public static class IValueDisposableExtensions
    {
        /// <summary>
        /// Give an intent of a disposable value to be boxed to an IDisposable
        /// </summary>
        /// <remarks>It is the same as calling '(IDisposable) value' but without the IDE shouting at you, and without seeing remarks about boxing</remarks>
        public static IDisposable IntendedBox<T>(this T disposable)
            where T : IDisposable
        {
            return disposable;
        }
    }
}