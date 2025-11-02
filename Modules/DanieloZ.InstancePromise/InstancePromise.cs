using System;
using System.Collections;
using UnityEngine;

namespace DanieloZ.InstancePromise
{
    /// <summary>
    /// Promise для получения экземпляра SingletonManager
    /// Позволяет подписаться на появление экземпляра, если он еще не создан
    /// </summary>
    /// <typeparam name="T">Тип SingletonManager</typeparam>
    public class InstancePromise<T> where T : MonoBehaviour
    {
        private T _instance;
        private bool _isResolved;
        private Action<T> _onResolved;

        /// <summary>
        /// Проверяет, был ли промис выполнен
        /// </summary>
        public bool IsResolved => _isResolved;

        /// <summary>
        /// Получает экземпляр, если он доступен
        /// </summary>
        public T Instance => _instance;

        /// <summary>
        /// Создает новый промис
        /// </summary>
        public InstancePromise()
        {
            _isResolved = false;
        }

        /// <summary>
        /// Подписывается на получение экземпляра
        /// Если экземпляр уже доступен, callback вызывается немедленно
        /// </summary>
        /// <param name="callback">Функция обратного вызова</param>
        public InstancePromise<T> Then(Action<T> callback)
        {
            if (_isResolved && _instance != null)
            {
                // Если промис уже выполнен, вызываем callback немедленно
                callback?.Invoke(_instance);
            }
            else
            {
                // Иначе добавляем в очередь
                _onResolved += callback;
            }
            return this;
        }

        /// <summary>
        /// Выполняет промис с экземпляром
        /// </summary>
        /// <param name="instance">Экземпляр для разрешения промиса</param>
        public void Resolve(T instance)
        {
            if (_isResolved)
            {
                Debug.LogWarning($"[InstancePromise] Промис для типа {typeof(T).Name} уже был выполнен");
                return;
            }

            _instance = instance;
            _isResolved = true;

            // Вызываем все подписанные callback'и
            _onResolved?.Invoke(_instance);
            
            // Очищаем подписки
            _onResolved = null;
        }

        /// <summary>
        /// Получает экземпляр асинхронно через корутину
        /// </summary>
        /// <param name="callback">Callback с экземпляром</param>
        /// <returns>Корутина</returns>
        public IEnumerator GetInstanceAsync(Action<T> callback)
        {
            if (_isResolved && _instance != null)
            {
                callback?.Invoke(_instance);
                yield break;
            }

            bool completed = false;
            T result = null;

            Then(instance =>
            {
                result = instance;
                completed = true;
            });

            // Ожидаем разрешения промиса
            yield return new WaitUntil(() => completed);

            callback?.Invoke(result);
        }

        /// <summary>
        /// Получает экземпляр асинхронно с таймаутом
        /// </summary>
        /// <param name="callback">Callback с экземпляром</param>
        /// <param name="timeoutSeconds">Таймаут в секундах</param>
        /// <returns>Корутина</returns>
        public IEnumerator GetInstanceAsync(Action<T> callback, float timeoutSeconds)
        {
            if (_isResolved && _instance != null)
            {
                callback?.Invoke(_instance);
                yield break;
            }

            bool completed = false;
            T result = null;
            float elapsedTime = 0f;

            Then(instance =>
            {
                result = instance;
                completed = true;
            });

            // Ожидаем разрешения промиса или таймаута
            while (!completed && elapsedTime < timeoutSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!completed)
            {
                Debug.LogError($"[InstancePromise] Таймаут ожидания экземпляра {typeof(T).Name} ({timeoutSeconds}s)");
            }

            callback?.Invoke(result);
        }

        /// <summary>
        /// Сбрасывает промис (для повторного использования)
        /// </summary>
        public void Reset()
        {
            _instance = null;
            _isResolved = false;
            _onResolved = null;
        }
    }
}

