using System;

namespace GameHost.Revolution
{
	internal class FilterReference
	{
		public Filter Filter;
		public int    Referenced;

		public FilterReference(Filter filter)
		{
			this.Filter = filter;
		}
	}

	internal class RemoveFilterReferenceOnDispose : IDisposable
	{
		public FilterReference FilterReference;

		public RemoveFilterReferenceOnDispose(FilterReference filterFilterReference)
		{
			filterFilterReference.Referenced++;

			this.FilterReference = filterFilterReference;
		}

		public void Dispose()
		{
			FilterReference.Referenced--;
			FilterReference = null;
		}
	}
}