using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GameHost.Applications;

namespace GameHost.Core.Ecs
{
    public static class AppSystemResolver
    {
        public static void ResolveFor<TApplication>(List<Type> foundTypes, Func<Type, bool> isSystemValid = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetCustomAttribute<AllowAppSystemResolvingAttribute>() != null)
                    ResolveFor<TApplication>(asm, foundTypes, isSystemValid);
            }

            sw.Stop();
        }

        public static void ResolveFor<TApplication>(Assembly assembly, List<Type> foundTypes, Func<Type, bool> isSystemValid = null)
        {
            isSystemValid ??= t =>
            {
                var attr = t.GetCustomAttribute<RestrictToApplicationAttribute>();
                return attr == null || attr.IsValid<TApplication>();
            };

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract
                    && !type.ContainsGenericParameters
                    && type.GetCustomAttribute<InjectSystemToWorldAttribute>() != null
                    && type.GetCustomAttribute<DontInjectSystemToWorldAttribute>() == null)
                {
                    if (isSystemValid(type))
                    {
                        foundTypes.Add(type);
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class AllowAppSystemResolvingAttribute : Attribute
    {
    }

    public class RestrictToApplicationAttribute : Attribute
    {
        public Type[] ApplicationTypes;

        public RestrictToApplicationAttribute(params Type[] applicationTypes)
        {
            ApplicationTypes = applicationTypes;
            foreach (var type in ApplicationTypes)
                if (!IsValid(type))
                    throw new InvalidOperationException($"{type} is not valid.");
        }

        public bool IsValid(Type type)
        {
            return typeof(IApplication).IsAssignableFrom(type) && ApplicationTypes.Contains(type);
        }

        public bool IsValid<T>()
        {
            return IsValid(typeof(T));
        }
    }
}