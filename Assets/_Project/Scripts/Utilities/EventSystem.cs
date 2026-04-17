// Path: Assets/_Project/Scripts/Utilities/EventSystem.cs
using System;
using System.Collections.Generic;

using Core;

using Project.Core;

using UnityEngine;

namespace Project.Utilities
{
    /// <summary>
    /// Simple decoupled event system with zero allocations.
    /// Supports generic event types and priority-based subscription.
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        private static EventSystem _instance;
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new Dictionary<Type, List<Subscription>>();
        private readonly Dictionary<Type, List<Action<object>>> _genericCallbacks = new Dictionary<Type, List<Action<object>>>();
        private readonly Queue<EventQueueItem> _eventQueue = new Queue<EventQueueItem>();
        private bool _isProcessingQueue;
        private readonly List<Subscription> _reusableBuffer = new List<Subscription>();

        private struct Subscription
        {
            public Delegate Callback;
            public int Priority;
            public object Owner;
        }

        private struct EventQueueItem
        {
            public Type EventType;
            public object EventData;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Self-registration into G
            if (G.Events == null)
            {
                G.Events = this;
                G.EnsureSystem(nameof(EventSystem), this);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                if (G.Events == this)
                    G.Events = null;
            }
        }

        private void Update()
        {
            // Process queued events at the end of the frame
            ProcessEventQueue();
        }

        #region Public API

        /// <summary>
        /// Subscribe to an event type with a callback.
        /// </summary>
        /// <typeparam name="T">Event type (must be a class or struct)</typeparam>
        /// <param name="callback">Action to invoke when event is triggered</param>
        /// <param name="priority">Higher priority callbacks are invoked first (default 0)</param>
        /// <param name="owner">Optional owner object for automatic unsubscription</param>
        public void Subscribe<T>(Action<T> callback, int priority = 0, object owner = null) where T : class
        {
            Type eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var list))
            {
                list = new List<Subscription>();
                _subscriptions[eventType] = list;
            }

            // Check for duplicate subscription
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Callback.Equals(callback) && list[i].Owner == owner)
                {
                    Debug.LogWarning($"[EventSystem] Duplicate subscription for {eventType.Name}");
                    return;
                }
            }

            var sub = new Subscription
            {
                Callback = callback,
                Priority = priority,
                Owner = owner
            };

            // Insert sorted by priority (higher first)
            int index = 0;
            while (index < list.Count && list[index].Priority >= priority)
                index++;
            list.Insert(index, sub);
        }

        /// <summary>
        /// Unsubscribe a specific callback.
        /// </summary>
        public void Unsubscribe<T>(Action<T> callback) where T : class
        {
            Type eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Callback.Equals(callback))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Unsubscribe all callbacks owned by a specific object.
        /// </summary>
        public void UnsubscribeAll(object owner)
        {
            foreach (var kvp in _subscriptions)
            {
                kvp.Value.RemoveAll(sub => sub.Owner == owner);
            }

            foreach (var kvp in _genericCallbacks)
            {
                kvp.Value.RemoveAll(callback => callback.Target == owner);
            }
        }

        /// <summary>
        /// Trigger an event immediately.
        /// </summary>
        public void Trigger<T>(T eventData) where T : class
        {
            Type eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var list))
                return;

            // Iterate over a copy to allow modifications during invocation
            var copy = new List<Subscription>(list);
            foreach (var sub in copy)
            {
                try
                {
                    ((Action<T>)sub.Callback)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventSystem] Exception in callback for {eventType.Name}: {ex}");
                }
            }
        }

        /// <summary>
        /// Queue an event to be processed at the end of the frame.
        /// </summary>
        public void Queue<T>(T eventData) where T : class
        {
            _eventQueue.Enqueue(new EventQueueItem
            {
                EventType = typeof(T),
                EventData = eventData
            });
        }

        /// <summary>
        /// Clear all subscriptions for a specific event type.
        /// </summary>
        public void Clear<T>() where T : class
        {
            Type eventType = typeof(T);
            _subscriptions.Remove(eventType);
            _genericCallbacks.Remove(eventType);
        }

        /// <summary>
        /// Clear all subscriptions (use with caution).
        /// </summary>
        public void ClearAll()
        {
            _subscriptions.Clear();
            _genericCallbacks.Clear();
            _eventQueue.Clear();
        }

        #endregion

        #region Generic Non‑typed Events (for dynamic usage)

        /// <summary>
        /// Subscribe to any event of type T using a generic Action<object>.
        /// Useful for dynamic event handling.
        /// </summary>
        public void SubscribeGeneric<T>(Action<object> callback) where T : class
        {
            Type eventType = typeof(T);
            if (!_genericCallbacks.TryGetValue(eventType, out var list))
            {
                list = new List<Action<object>>();
                _genericCallbacks[eventType] = list;
            }

            if (!list.Contains(callback))
                list.Add(callback);
        }

        /// <summary>
        /// Trigger generic callbacks for an event type.
        /// </summary>
        private void TriggerGeneric(Type eventType, object eventData)
        {
            if (_genericCallbacks.TryGetValue(eventType, out var list))
            {
                foreach (var callback in list)
                {
                    try
                    {
                        callback.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventSystem] Exception in generic callback for {eventType.Name}: {ex}");
                    }
                }
            }
        }

        #endregion

        #region Queue Processing

        private void ProcessEventQueue()
        {
            if (_isProcessingQueue || _eventQueue.Count == 0)
                return;

            _isProcessingQueue = true;

            // Process all events currently in the queue (new events added during processing will be handled next frame)
            int count = _eventQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var item = _eventQueue.Dequeue();
                ProcessEvent(item.EventType, item.EventData);
            }

            _isProcessingQueue = false;
        }

        private void ProcessEvent(Type eventType, object eventData)
        {
            // Trigger typed subscriptions
            if (_subscriptions.TryGetValue(eventType, out var list))
            {
                // Reuse buffer to avoid allocations
                _reusableBuffer.Clear();
                _reusableBuffer.AddRange(list);
                foreach (var sub in _reusableBuffer)
                {
                    try
                    {
                        sub.Callback.DynamicInvoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventSystem] Exception in queued callback for {eventType.Name}: {ex}");
                    }
                }
            }

            // Trigger generic callbacks
            TriggerGeneric(eventType, eventData);
        }

        #endregion

        #region Static Access (optional)

        public static EventSystem Instance => _instance;

        public static void SubscribeStatic<T>(Action<T> callback, int priority = 0, object owner = null) where T : class
        {
            if (_instance != null)
                _instance.Subscribe(callback, priority, owner);
            else
                Debug.LogWarning("[EventSystem] No instance available. Subscription ignored.");
        }

        public static void TriggerStatic<T>(T eventData) where T : class
        {
            if (_instance != null)
                _instance.Trigger(eventData);
        }

        public static void QueueStatic<T>(T eventData) where T : class
        {
            if (_instance != null)
                _instance.Queue(eventData);
        }

        #endregion

        #region Debug

        [ContextMenu("Print Subscription Counts")]
        private void PrintSubscriptionCounts()
        {
            Debug.Log($"[EventSystem] Total event types: {_subscriptions.Count}");
            foreach (var kvp in _subscriptions)
            {
                Debug.Log($"  {kvp.Key.Name}: {kvp.Value.Count} subscribers");
            }
        }

        #endregion
    }
}