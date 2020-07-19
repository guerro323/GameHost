namespace GameHost.Core.Modules
{
	public struct GameHostModuleDescription
	{
		public string DisplayName;
		public string NameId;
		public string Author;

		public bool IsNameIdValid()
		{
			return !(NameId.Contains('/')
			         || NameId.Contains('\\')
			         || NameId.Contains('?')
			         || NameId.Contains(':')
			         || NameId.Contains('|')
			         || NameId.Contains('*')
			         || NameId.Contains('<')
			         || NameId.Contains('>'));
		}
	}
}