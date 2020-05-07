using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameHost.Injection;

namespace GameHost
{
    /// <summary>
    /// An instance represent a part of the application.
    /// It could be a client instance, or a server instance.
    /// </summary>
    public class Instance
    {
        private static ConcurrentBag<Instance> _Instances = new ConcurrentBag<Instance>();

        /// <summary>
        /// The name of the instance
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The context of the instance
        /// </summary>
        public Context Context { get; private set; }

        private void SetNameAndContext(string name, Context ctx)
        {
            Name    = name;
            Context = ctx;
        }

        public static IEnumerable<Instance> GetInstanceWithName(string name)
        {
            return _Instances.Where(i => i.Name == name);
        }

        public static T CreateInstance<T>(string name, Context ctx)
            where T : Instance, new()
        {
            var instance = new T();
            instance.SetNameAndContext(name, ctx);

            _Instances.Add(instance);
            return instance;
        }
    }
}
