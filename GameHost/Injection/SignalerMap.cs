using System;
using System.Collections.Generic;
using GameHost.Core.Applications;
using GameHost.Event;

namespace GameHost.Injection
{
    public class SignalerMap
    {
        private abstract class Signal
        {
            public int Version;
        }

        private class SignalAppData<T> : Signal
            where T : IAppEvent
        {
            public List<IReceiveAppEvent<T>> Receivers = new List<IReceiveAppEvent<T>>();
        }

        private class SignalDataData<T> : Signal
            where T : IDataEvent
        {
            public List<IReceiveDataEvent<T>> Receivers = new List<IReceiveDataEvent<T>>();
        }

        private Dictionary<Type, Signal> signalMap = new Dictionary<Type, Signal>(32);

        public int Version { get; set; }

        public void SignalApp<T>(in T data, IEnumerable<object> objects)
            where T : IAppEvent
        {
            var useCurrentList = signalMap.TryGetValue(typeof(T), out var signal) && signal.Version == Version;
            if (useCurrentList)
            {
                foreach (var obj in ((SignalAppData<T>)signal).Receivers)
                    obj.OnEvent(data);

                return;
            }

            signal         ??= new SignalAppData<T>();
            signal.Version =   Version;

            var signalGen = (SignalAppData<T>)signal;

            foreach (var obj in objects)
            {
                if (obj is IReceiveAppEvent<T> receive)
                    signalGen.Receivers.Add(receive);
            }
            
            signalMap[typeof(T)] = signal;
            SignalApp(in data, objects);
        }

        public void SignalData<T>(ref T data, IEnumerable<object> objects)
            where T : IDataEvent
        {
            var useCurrentList = signalMap.TryGetValue(typeof(T), out var signal) && signal.Version == Version;
            if (useCurrentList)
            {
                foreach (var obj in ((SignalDataData<T>)signal).Receivers)
                    obj.OnEvent(ref data);

                return;
            }

            signal         ??= new SignalDataData<T>();
            signal.Version =   Version;

            var signalGen = (SignalDataData<T>)signal;

            foreach (var obj in objects)
            {
                if (obj is IReceiveDataEvent<T> receive)
                    signalGen.Receivers.Add(receive);
            }
            
            signalMap[typeof(T)] = signal;
            SignalData(ref data, objects);
        }
    }
}
