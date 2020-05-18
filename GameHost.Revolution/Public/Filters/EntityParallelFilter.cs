using System;
using System.Threading.Tasks;
using DefaultEcs;

namespace GameHost.Revolution.Public.Filters
{
	public abstract class EntityParallelFilter : Filter
	{
		public override bool OnSerializerCall(ReadOnlySpan<Entity> entities, Span<bool> invalids, Span<bool> valids)
		{
			for (var i = 0; i < entities.Length; i++)
			{
				SetValidity(in entities[i], ref invalids[i], ref valids[i]);
			}

			return true;
		}

		protected abstract void SetValidity(in Entity entity, ref bool invalid, ref bool valid);
	}
}