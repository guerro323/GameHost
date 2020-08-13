using System.Collections;
using System.Collections.Generic;

namespace GameHost.Core.Ecs.Passes
{
	public abstract class PassRegisterBase
	{
		public abstract IList RegisteredObjects { get; }

		public void RegisterCollectionAndFilter(IEnumerable<object> objects)
		{
			OnRegisterCollectionAndFilter(objects);
		}

		public void Trigger()
		{
			OnTrigger();
		}

		protected abstract void OnTrigger();
		protected abstract void OnRegisterCollectionAndFilter(IEnumerable<object> collection);
	}

	public abstract class PassRegisterBase<TActOn> : PassRegisterBase
	{
		public override IList RegisteredObjects => GetObjects();

		protected List<TActOn> finalObjects = new List<TActOn>();
		protected List<TActOn> temporaryObjects = new List<TActOn>();

		protected override void OnRegisterCollectionAndFilter(IEnumerable<object> collection)
		{
			temporaryObjects.Clear();
			foreach (var obj in collection)
			{
				if (obj is TActOn actOn)
					temporaryObjects.Add(actOn);
			}
		}

		public List<TActOn> GetObjects()
		{
			if (temporaryObjects.Count == 0)
				return finalObjects;

			finalObjects.Clear();
			finalObjects.AddRange(temporaryObjects);
			temporaryObjects.Clear();

			return finalObjects;
		}
	}
}