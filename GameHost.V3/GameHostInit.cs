using System;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.V3.Injection;
using GameHost.V3.Module;
using GameHost.V3.Module.Systems;

namespace GameHost.V3
{
    public static class GameHostInit
    {
        public static HostRunner Launch<TModule>(Action<HostRunnerScope> configureScope, Func<HostRunnerScope, TModule> getModule)
            where TModule : HostModule
        {
            var runner = new HostRunner();
            configureScope(runner.Scope);
            
            var entryModule = runner.AddEntryModule();

            // Create our module once the entry module dependencies are completed
            entryModule.Dependencies.OnFinal(() =>
            {
                if (runner.Scope.Context.TryGet(out ModuleManager manageModuleSystem)
                    && runner.Scope.Context.TryGet(out World world))
                {
                    var module = manageModuleSystem.GetOrCreate(
                        HostModule.GetModuleGroupName(typeof(TModule)),
                        HostModule.GetModuleName(typeof(TModule))
                    );

                    module.Set(new LoadModuleList { sc => getModule(sc) });

                    var request = world.CreateEntity();
                    request.Set(new RequestLoadModule("root", module));
                }
                else
                    throw new InvalidOperationException(
                        "ManageModuleSystem should have been created (form GameHostEntryModule)"
                    );
            });

            return runner;
        }
    }
}