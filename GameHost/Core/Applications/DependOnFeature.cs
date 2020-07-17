namespace GameHost.Applications
{
	public abstract class DependOnFeature
	{
		public abstract bool IsFeatureValid(IFeature feature);
		protected abstract void Attach(IFeature feature);
		protected abstract void Detach(IFeature feature);
	}
}