using System;
using System.Linq;

namespace GameHost.Core.Applications
{
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
            return type.IsSubclassOf(typeof(ApplicationHostBase)) && ApplicationTypes.Contains(type);
        }

        public bool IsValid<T>()
        {
            return IsValid(typeof(T));
        }
    }
}
