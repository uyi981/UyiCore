using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UyiCore.Observer
{
    /// <summary>
    /// Marker interface for all event data.
    /// </summary>
    public interface IEventData { }

    public static class Observer<TEvent> where TEvent : Enum
    {
        // Single dictionary for all events
        private static readonly Dictionary<TEvent, Delegate> _eventTable = new();

        #region Add Listeners

        public static void AddListener(TEvent eventType, UnityAction callback)
        {
            AddInternal(eventType, callback);
        }

        public static void AddListener<TData>(TEvent eventType, UnityAction<TData> callback) where TData : IEventData
        {
            AddInternal(eventType, callback);
        }

        public static void AddListener(TEvent eventType, UnityAction<object> callback)
        {
            AddInternal(eventType, callback);
        }

        private static void AddInternal(TEvent eventType, Delegate callback)
        {
            if (_eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                if (existingDelegate.GetType() == callback.GetType())
                {
                    _eventTable[eventType] = Delegate.Combine(existingDelegate, callback);
                }
                else
                {
                    LogTypeMismatch(eventType, existingDelegate);
                }
            }
            else
            {
                _eventTable[eventType] = callback;
            }
        }

        #endregion

        #region Remove Listeners

        public static void RemoveListener(TEvent eventType, UnityAction callback)
        {
            RemoveInternal(eventType, callback);
        }

        public static void RemoveListener<TData>(TEvent eventType, UnityAction<TData> callback) where TData : IEventData
        {
            RemoveInternal(eventType, callback);
        }

        public static void RemoveListener(TEvent eventType, UnityAction<object> callback)
        {
            RemoveInternal(eventType, callback);
        }

        private static void RemoveInternal(TEvent eventType, Delegate callback)
        {
            if (_eventTable.TryGetValue(eventType, out var existingDelegate) && existingDelegate.GetType() == callback.GetType())
            {
                var newDelegate = Delegate.Remove(existingDelegate, callback);
                if (newDelegate == null)
                    _eventTable.Remove(eventType);
                else
                    _eventTable[eventType] = newDelegate;
            }
        }

        #endregion

        #region Emit Events

        public static void Emit(TEvent eventType)
        {
            if (_eventTable.TryGetValue(eventType, out var del) && del is UnityAction action)
            {
                SafeInvoke(() => action.Invoke(), eventType);
            }
            else
            {
                Debug.LogWarning($"[Observer<{typeof(TEvent).Name}>] No listeners for event {eventType}");
            }
        }

        public static void Emit<TData>(TEvent eventType, TData data) where TData : IEventData
        {
            if (_eventTable.TryGetValue(eventType, out var del) && del is UnityAction<TData> action)
            {
                SafeInvoke(() => action.Invoke(data), eventType);
            }
            else
            {
                Debug.LogWarning($"[Observer<{typeof(TEvent).Name}>] No listeners for event {eventType}");
            }
        }

        public static void Emit(TEvent eventType, object data)
        {
            if (_eventTable.TryGetValue(eventType, out var del) && del is UnityAction<object> action)
            {
                if (data is object[] arr && arr.Length > 1)
                {
                    Debug.LogWarning($"[Observer<{typeof(TEvent).Name}>] Event '{eventType}' passed object[] with {arr.Length} elements to UnityAction<object> listener. Consider using IEventData instead.");
                }

                SafeInvoke(() => action.Invoke(data), eventType);
            }
            else
            {
                Debug.LogWarning($"[Observer<{typeof(TEvent).Name}>] No listeners for event {eventType}");
            }
        }

        private static void SafeInvoke(Action invokeAction, TEvent eventType)
        {
            try
            {
                invokeAction();
            }
            catch (Exception ex)
            {
                Debug.LogException(new Exception($"[Observer<{typeof(TEvent).Name}>] Error invoking event {eventType}", ex));
            }
        }

        #endregion

        #region Data Retrieval

        public static bool HasListeners(TEvent eventType)
        {
            return _eventTable.ContainsKey(eventType);
        }

        public static int GetListenerCount(TEvent eventType)
        {
            if (_eventTable.TryGetValue(eventType, out var del))
                return del?.GetInvocationList().Length ?? 0;
            return 0;
        }

        public static List<TEvent> GetAllActiveEvents()
        {
            return new List<TEvent>(_eventTable.Keys);
        }

        #endregion

        #region Event Management

        public static void RemoveAllListeners(TEvent eventType)
        {
            _eventTable.Remove(eventType);
        }

        public static void ClearAll()
        {
            _eventTable.Clear();
        }

        #endregion

        #region Debugging & Utilities

        public static void DebugLogAllEvents()
        {
            foreach (var kvp in _eventTable)
            {
                Debug.Log($"[Observer<{typeof(TEvent).Name}>] Event: {kvp.Key}, Listeners: {GetListenerCount(kvp.Key)}");
            }
        }

        private static void LogTypeMismatch(TEvent eventType, Delegate existingDelegate)
        {
            Debug.LogError($"[Observer<{typeof(TEvent).Name}>] Attempted to add listener with a different signature to event {eventType}. Existing type: {existingDelegate.GetType()}");
        }

        #endregion
    }
}
