﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.Core.Modules.Feature
{
	public class ModuleStorage : IStorage
	{
		private readonly IStorage parent;

		public ModuleStorage(IStorage parent)
		{
			this.parent = parent ?? throw new NullReferenceException(nameof(parent));
		}

		public string CurrentPath => parent.CurrentPath;

		public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
		{
			return parent.GetFilesAsync(pattern);
		}

		public Task<byte[]> GetFileContentAsync(string path)
		{
			// assembly byte code?
			return null;
		}

		public Task<IStorage> GetOrCreateDirectoryAsync(string path)
		{
			throw new InvalidOperationException("It's not possible to create directories with ModuleStorage");
		}
	}
}