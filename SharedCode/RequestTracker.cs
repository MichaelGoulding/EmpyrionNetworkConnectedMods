using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPMConnector;

namespace SharedCode
{
    internal class RequestTracker
    {
        private ushort _nextAvailableId = 12340;
        private Dictionary<ushort, object/*TaskCompletionSource<T>*/> _taskCompletionSourcesById = new Dictionary<ushort, object>();

        internal (ushort, Task<T>) GetNewTaskCompletionSource<T>()
        {
            if (_nextAvailableId == ushort.MaxValue)
            {
                _nextAvailableId = 12340;
            }

            var newId = _nextAvailableId++;

            var taskCompletionSource = new TaskCompletionSource<T>();
            _taskCompletionSourcesById[newId] = taskCompletionSource;

            return (newId, taskCompletionSource.Task);
        }

        internal bool TryHandleEvent(ModProtocol.Package p)
        {
            bool trackingIdFound = _taskCompletionSourcesById.ContainsKey(p.seqNr);

            if (trackingIdFound)
            {
                object taskCompletionSource = _taskCompletionSourcesById[p.seqNr];
                _taskCompletionSourcesById.Remove(p.seqNr);

                if (p.cmd == Eleon.Modding.CmdId.Event_Error)
                {
                    Eleon.Modding.ErrorInfo eInfo = (Eleon.Modding.ErrorInfo)p.data;
                    System.Reflection.MethodInfo setException = taskCompletionSource.GetType().GetMethod("SetException");
                    setException.Invoke(taskCompletionSource, new[] { new Exception(eInfo.ToString()) });
                }
                else
                {
                    System.Reflection.MethodInfo setResult = taskCompletionSource.GetType().GetMethod("SetResult");
                    setResult.Invoke(taskCompletionSource, new[] { p.data });
                }
            }

            return trackingIdFound;
        }
    }
}
