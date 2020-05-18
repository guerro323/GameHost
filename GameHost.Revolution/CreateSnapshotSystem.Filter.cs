using System;
using System.Collections.Generic;

namespace GameHost.Revolution
{
	public partial class CreateSnapshostSystem
	{
		private Dictionary<Type, FilterReference> globalFilters = new Dictionary<Type, FilterReference>(16);

		public IDisposable AddFilter<TFilter>()
			where TFilter : Filter
		{
			if (globalFilters.TryGetValue(typeof(TFilter), out var filterReference))
				return new RemoveFilterReferenceOnDispose(filterReference);

			filterReference = new FilterReference(Activator.CreateInstance<TFilter>());
			return new RemoveFilterReferenceOnDispose(filterReference);
		}
	}
}