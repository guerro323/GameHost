using System;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Core.Bindables;
using GameHost.Core.IO;
using GameHost.Injection;
using GameHost.IO;

namespace GameHost.Core.Modding
{
    public class CModule
    {
        public readonly Entity  Source;
        public readonly Context Ctx;

        /// <summary>
        /// Get the mod storage
        /// </summary>
        public readonly Bindable<IStorage> Storage;

        public readonly DllStorage DllStorage;

        private object bindableProtection = new object();

        public CModule(Entity source, Context ctxParent, SModuleInfo original)
        {
            if (!original.IsNameIdValid())
                throw new InvalidOperationException($"The mod '{original.NameId}' has invalid characters!");

            Source     = source;
            Ctx        = new Context(ctxParent);
            Storage    = new Bindable<IStorage>(protection: bindableProtection);
            DllStorage = new DllStorage(GetType().Assembly);

            var strategy = new ContextBindingStrategy(Ctx, true);
            var storage  = strategy.Resolve<IStorage>();

            if (storage == null)
                throw new NullReferenceException(nameof(storage));

            storage.GetOrCreateDirectoryAsync($"ModuleData/{original.NameId}").ContinueWith(OnRequiredDirectoryFound);
        }

        private void OnRequiredDirectoryFound(Task<IStorage> task)
        {
            if (task.Result == null)
                return;

            Storage.EnableProtection(false, bindableProtection);
            Storage.Value = task.Result;
            Storage.EnableProtection(true, bindableProtection);
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class ModuleDescriptionAttribute : Attribute
    {
        public readonly string DisplayName, Author;
        public readonly Type ModuleType;

        public ModuleDescriptionAttribute(string displayName, string author)
        {
            DisplayName = displayName;
            Author      = author;
            ModuleType  = null;
        }
        
        public ModuleDescriptionAttribute(string displayName, string author, Type moduleType)
        {
            DisplayName = displayName;
            Author      = author;
            ModuleType   = moduleType;
        }

        public bool IsValid => ModuleType?.IsSubclassOf(typeof(CModule)) == true;
    }

    public struct SModuleInfo
    {
        public string DisplayName;
        public string NameId;
        public string Author;

        public bool IsNameIdValid()
        {
            return !(NameId.Contains('/')
                     || NameId.Contains('\\')
                     || NameId.Contains('?')
                     || NameId.Contains(':')
                     || NameId.Contains('|')
                     || NameId.Contains('*')
                     || NameId.Contains('<')
                     || NameId.Contains('>'));
        }
    }
}
