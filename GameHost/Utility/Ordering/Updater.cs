using System.Collections.Generic;
using Collections.Pooled;

namespace GameHost.Utility
{
	public class Updater
	{
		/// <summary>
		/// Updaters in this constraint will not leak outside of this updater (grouped)
		/// </summary>
		public readonly ConstraintGroup Interior = new();

		/// <summary>
		/// Updaters in this constraints will leak outside of this updater (not grouped)
		/// </summary>
		public readonly ConstraintGroup Exterior = new();

		public readonly PooledList<Constraint> Constraints = new();
	}

	public class ConstraintGroup
	{
		public readonly Constraint Left  = new();
		public readonly Constraint Right = new();
	}

	public class Constraint
	{
		public readonly PooledList<Updater> Attaches = new();
	}
}