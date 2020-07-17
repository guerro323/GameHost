using System;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Injection;

namespace GameHost.Core.Features
{
	public static class FeatureDependencyStrategy
	{
		public static FeatureDependencyStrategy<T> Absolute<T>(WorldCollection worldCollection) 
			where T : IFeature
		{
			return new FeatureDependencyStrategy<T>(worldCollection, f => f.GetType() == typeof(T));
		}

		public static FeatureDependencyStrategy<T> AssignableTo<T>(WorldCollection worldCollection) 
			where T : IFeature
		{
			return new FeatureDependencyStrategy<T>(worldCollection, f => f is T);
		}
	}

	public class FeatureDependencyStrategy<T> : IDependencyStrategy where T : IFeature
	{
		private readonly Func<IFeature, bool> featureIsValid;
		private readonly WorldCollection      worldCollection;

		public FeatureDependencyStrategy(WorldCollection worldCollection, Func<IFeature, bool> featureIsValid)
		{
			this.worldCollection = worldCollection;
			this.featureIsValid  = featureIsValid;
		}

		public object ResolveNow(Type type)
		{
			return new GetFeature<T>(worldCollection, featureIsValid);
		}

		public Func<object> GetResolver(Type type)
		{
			return () => ResolveNow(type);
		}
	}
}