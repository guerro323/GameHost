namespace GameHost.Core.Applications
{
    public interface IAppEvent
    {
    }

    public interface IReceiveAppEvent<in T> where T : IAppEvent
    {
        void OnEvent(T t);
    }

    public interface IDataEvent
    {
    }

    public interface IReceiveDataEvent<T> where T : IDataEvent
    {
        void OnEvent(ref T t);
    }
}
