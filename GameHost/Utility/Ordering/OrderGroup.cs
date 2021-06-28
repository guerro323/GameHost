using System;
using System.Collections.Generic;
using Collections.Pooled;

namespace GameHost.Utility
{
	public class OrderGroup<T>
	{
		private Dictionary<Type, T>    typeToObject = new();
		private Dictionary<T, Updater> updaterMap   = new();

		public readonly Updater Main = new();

		public bool TryGet<TTarget>(out TTarget target, out Updater updater)
			where TTarget : T
		{
			if (typeToObject.TryGetValue(typeof(TTarget), out var targetU))
			{
				target = (TTarget)targetU;
				return TryGet(target, out updater);
			}

			target  = default;
			updater = default;
			return false;
		}

		public bool TryGet(T value, out Updater updater)
		{
			return updaterMap.TryGetValue(value, out updater);
		}

		public Updater GetOrCreate(T value)
		{
			if (!typeToObject.TryGetValue(value.GetType(), out _))
				typeToObject[value.GetType()] = value;

			if (!updaterMap.TryGetValue(value, out var updater))
				updaterMap[value] = updater = new Updater();

			return updater;
		}

		public void Build(PooledList<T> result)
		{
			foreach (var (_, updater) in updaterMap)
			{
				if (updater.Constraints.Count == 0)
				{
					Main.Interior.Right.Attaches.Add(updater);
					continue;
				}

				foreach (var constraint in updater.Constraints)
				{
					constraint.Attaches.Add(updater);
				}
			}

			static void addInner(Updater updater, PooledList<T> list)
			{
				void addConstraintGroup(ConstraintGroup group)
				{
					foreach (var left in group.Left.Attaches)
					{
						
					}
				}
				
				addConstraintGroup(updater.Interior);
				addConstraintGroup(updater.Exterior);
			}

			addInner(Main, result);

			foreach (var (_, updater) in updaterMap)
			{
				addInner(updater, result);
			}
		}
	}
}