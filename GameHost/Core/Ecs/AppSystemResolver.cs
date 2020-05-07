using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GameHost.Core.Applications;

namespace GameHost.Core.Ecs
{
    public static class AppSystemResolver
    {
        public static void ResolveFor<TApplication>(List<Type> foundTypes)
        {
            var sw = new Stopwatch();
            sw.Start();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetCustomAttribute<AllowAppSystemResolvingAttribute>() != null)
                    ResolveFor<TApplication>(asm, foundTypes);
            }
            sw.Stop();
        }

        public static void ResolveFor<TApplication>(Assembly assembly, List<Type> foundTypes)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract
                    && !type.ContainsGenericParameters
                    && type.GetCustomAttribute<InjectSystemToWorldAttribute>() != null)
                {
                    var restrict = type.GetCustomAttribute<RestrictToApplicationAttribute>();
                    if (restrict == null || restrict.IsValid<TApplication>())
                    {
                        foundTypes.Add(type);
                        Console.WriteLine($"Adding system to application {{{typeof(TApplication).Name}}}: {type.Name}");
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class AllowAppSystemResolvingAttribute : Attribute
    {
    }
}
