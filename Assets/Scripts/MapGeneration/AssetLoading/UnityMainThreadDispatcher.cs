using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace OfficeMice.MapGeneration.AssetLoading
{
    /// <summary>
    /// Simple main thread dispatcher for Unity operations.
    /// Ensures that callbacks are executed on the main thread.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        private static UnityMainThreadDispatcher _instance = null;
        
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    
                    if (_instance == null)
                    {
                        var go = new GameObject("UnityMainThreadDispatcher");
                        _instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            while (_executionQueue.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }
        
        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">Action to execute</param>
        public void Enqueue(Action action)
        {
            if (action == null)
                return;
            
            _executionQueue.Enqueue(action);
        }
        
        /// <summary>
        /// Enqueues an action to be executed on the main thread and waits for completion.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <returns>Coroutine that can be yielded</returns>
        public IEnumerator EnqueueAndWait(Action action)
        {
            var completed = false;
            Exception exception = null;
            
            Enqueue(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed = true;
                }
            });
            
            while (!completed)
            {
                yield return null;
            }
            
            if (exception != null)
            {
                throw exception;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}