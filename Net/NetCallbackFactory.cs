using System;
using System.Collections.Generic;
using com.yoyo.avatar.protocol;
using Modules.AppDebug;

namespace Managers.Net
{
    public class NetCallbackFactory
    {
        private readonly Dictionary<int,Action<Protocol>> _callbackDictionary=new Dictionary<int, Action<Protocol>>();

        public void RegisterHandler(int code,Action<Protocol> callbackHandler)
        {
            if (_callbackDictionary.TryGetValue(code, out var eventAction))
            {
                var eventList = eventAction.GetInvocationList();
                if (Array.IndexOf(eventList, callbackHandler) > -1)
                {
                    DebugLog.LogWarning("add an existing handler :"+code);
                    return;
                }
            }
            else
            {
                _callbackDictionary.Add(code,null);
            }
            _callbackDictionary[code] += callbackHandler;
        }

        public void UnRegisterHandler(int code, Action<Protocol> callbackHandler)
        {
            if (!_callbackDictionary.TryGetValue(code, out _)) return;
            if (Array.IndexOf(_callbackDictionary[code].GetInvocationList(), callbackHandler) < 0)
            {
                DebugLog.LogWarning("remove an unexisted handler:"+code);
                return;
            }
            _callbackDictionary[code] -= callbackHandler;
            if (_callbackDictionary[code]==null||_callbackDictionary[code].GetInvocationList().Length==0)
            {
                _callbackDictionary.Remove(code);
            }
        }

        public Action<Protocol> GetOneHandler(int code)
        {
            _callbackDictionary.TryGetValue(code, out var handler);
            return handler;
        }
    }
}