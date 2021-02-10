using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Layouts
{
	/// <summary>
	/// A layout represent information about how an input should be processed with which keys.
	/// </summary>
	public abstract class InputLayoutBase
	{
		public readonly string                     Id;
		public          ReadOnlyMemory<CInput> Inputs { get; protected set; }

		public InputLayoutBase(string id)
		{
			this.Id = id;
			Inputs  = Array.Empty<CInput>();
		}
	}

	public class InputActionLayouts : Dictionary<string, InputLayoutBase>
	{
		public InputActionLayouts()
		{
		}

		public InputActionLayouts(InputActionLayouts original) : base(original)
		{

		}

		public InputActionLayouts(IEnumerable<InputLayoutBase> layouts)
		{
			foreach (var layout in layouts)
				Add(layout);
		}

		public void Add(InputLayoutBase layout) => Add(layout.Id, layout);

		public bool TryGetOrDefault(string currentLayout, out InputLayoutBase layout)
		{
			if (currentLayout != null && TryGetValue(currentLayout, out layout))
				return true;
			foreach (var value in Values)
			{
				layout = value;
				return true;
			}

			layout = null;
			return false;
		}
	}

	public struct InputCurrentLayout
	{
		public string Id;
	}
}