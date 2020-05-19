namespace GameHost.Entities
{
    /// <summary>
    /// Template systems provide a way to easily test your logic without making a system type. Ideal for scripting
    /// </summary>
    /*public sealed class TemplateSystem : IInitSystem, IUpdateSystem
    {
        public Action<TemplateSystem> OnInit;
        public Action<TemplateSystem> OnUpdate;

        private Dictionary<string, object> dataMap;

        void IInitSystem.OnInit(WorldCollection worldCollection)
        {
            dataMap = new Dictionary<string, object>();

            OnInit?.Invoke(this);
        }

        void IUpdateSystem.OnUpdate()
        {
            OnUpdate?.Invoke(this);
        }

        public T GetData<T>(string key)
        {
            return (T)dataMap[key];
        }

        public void SetData<T>(string key, T val)
        {
            dataMap[key] = val;
        }
    }*/
}
