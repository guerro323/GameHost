using System;
using System.Collections.Generic;
using GameHost.Core.Ecs.Passes;
using GameHost.Injection;

namespace GameHost.Core.Ecs
{
	public class SystemCollection : IDisposable
	{
		private OrderedList<object>      systemList;
		private Dictionary<Type, object> systemMap;

		private OrderedList<PassRegisterBase> availablePasses;

		public IReadOnlyCollection<object> SystemList => systemList.Elements;
		public IReadOnlyCollection<PassRegisterBase> Passes => availablePasses.Elements;

		public readonly Context         Ctx;
		public readonly WorldCollection WorldCollection;

		public SystemCollection(Context context, WorldCollection worldCollection)
		{
			Ctx             = context;
			WorldCollection = worldCollection;

			systemList = new OrderedList<object>();
			systemMap  = new Dictionary<Type, object>(64);

			availablePasses = new OrderedList<PassRegisterBase>();

			systemList.OnDirty += () =>
			{
				foreach (var register in availablePasses)
					register.RegisterCollectionAndFilter(systemList);
			};
		}

		public PassRegisterBase ExecutingRegister { get; private set; }

		private List<object>           disposedToUnregister = new List<object>();
		private List<PassRegisterBase> passToLoop           = new List<PassRegisterBase>();
		public void LoopPasses()
		{
			disposedToUnregister.Clear();
			foreach (var system in systemList)
			{
				if (system is AppObject appObject
				    && appObject.IsDisposed)
				{
					disposedToUnregister.Add(appObject);
				}
			}
			
			foreach (var toUnregister in disposedToUnregister)
				Unregister(toUnregister);
			
			passToLoop.Clear();
			foreach (var register in availablePasses)
				passToLoop.Add(register);
			
			foreach (var register in passToLoop)
			{
				if (register.ManualTrigger)
					continue;
					
				ExecutingRegister = register;
				register.Trigger();
			}
		}

		public void AddPass(PassRegisterBase pass, Type[] updateAfter, Type[] updateBefore)
		{
			availablePasses.Add(pass, updateAfter, updateBefore);
			pass.RegisterCollectionAndFilter(systemList);
		}

		private void RemakeLoop<T>(ref List<T> originalList, ref bool isDirty)
			where T : class
		{
			if (!isDirty)
				return;

			var systemToReorder = new List<object>(originalList);
			originalList.Clear();

			// kinda slow?
			foreach (var obj in systemList.Elements)
			{
				if (systemToReorder.Contains(obj))
					originalList.Add((T) obj);
			}

			isDirty = false;
		}

		public bool TryGet(Type type, out object obj)
		{
			return systemMap.TryGetValue(type, out obj);
		}

		public bool TryGet<T>(out T obj)
		{
			var success = TryGet(typeof(T), out var nonGenObj);
			obj = (T) nonGenObj;
			return success;
		}

		public T GetOrCreate<T>(Func<WorldCollection, T> createFunction) where T : class, IWorldSystem
		{
			return GetOrCreate(createFunction, OrderedList.GetBefore(typeof(T)), OrderedList.GetAfter(typeof(T)));
		}

		public T GetOrCreate<T>(Func<WorldCollection, T> createFunction, Type[] updateBefore, Type[] updateAfter)
			where T : class, IWorldSystem
		{
			if (systemMap.TryGetValue(typeof(T), out var obj))
				return (T) obj;

			obj = createFunction(WorldCollection);
			//new InjectPropertyStrategy(Ctx, true).Inject(obj);
			Add(obj, updateBefore, updateAfter);
			return (T) obj;
		}

		public object GetOrCreate(Type type)
		{
			if (systemMap.TryGetValue(type, out var obj))
				return obj;

			obj = Activator.CreateInstance(type, args: WorldCollection);
			//new InjectPropertyStrategy(Ctx, true).Inject(obj);
			Add(obj, OrderedList.GetBefore(obj.GetType()), OrderedList.GetAfter(obj.GetType()));
			return obj;
		}

		public void ForceSystemOrder(object obj, Type[] updateBefore, Type[] updateAfter)
		{
			if (!systemMap.ContainsKey(obj.GetType()))
				throw new InvalidOperationException("The system does not exit in world database.");
			systemList.Set(obj, updateBefore, updateAfter);
		}

		public void Add(object obj, Type[] updateBefore, Type[] updateAfter)
		{
			if (systemMap.TryGetValue(obj.GetType(), out var prev))
				Unregister(prev);
			
			systemMap[obj.GetType()] = obj;
			systemList.Set(obj, updateAfter, updateBefore);
			Ctx.Register(obj);
		}

		public void Unregister(object obj)
		{
			Console.WriteLine("unregister " + obj);
			if (systemMap.TryGetValue(obj.GetType(), out var currInMap) && currInMap == obj)
			{
				systemMap.Remove(obj.GetType());
				systemList.Remove(obj);
				Ctx.Unregister(obj);
			}
		}

		public void Dispose()
		{
			disposedToUnregister.Clear();
			
			foreach (var sys in systemList)
			{
				if (sys is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			systemList.Clear();
			systemList = null;
			
			systemMap.Clear();
			systemMap = null;
			
			availablePasses.Clear(); 
			passToLoop.Clear();

			ExecutingRegister = null;
		}
	}
}