using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class World3DUIObjectPool : MonoBehaviour
    {
        [SerializeField] private string poolKey = "World3DUI";
        [SerializeField] private Component prefab;
        [SerializeField, Min(0)] private int initialSize = 4;
        [SerializeField] private Transform parent;

        private void Awake()
        {
            Register();
        }

        [ContextMenu("Register")]
        public void Register()
        {
            if (prefab == null || PoolingManager.Instance == null || PoolingManager.Instance.HasPool(poolKey))
            {
                return;
            }

            var poolParent = parent != null ? parent : PoolingManager.Instance.GenerateContainer(poolKey);
            var pool = new Pool<Component>(
                () =>
                {
                    var instance = Instantiate(prefab, poolParent);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                initialSize,
                onGet: item => item.gameObject.SetActive(true),
                onReturn: item => item.gameObject.SetActive(false));

            PoolingManager.Instance.RegisterPool(poolKey, pool);
        }

        public Component Get()
        {
            return PoolingManager.Instance != null ? PoolingManager.Instance.GetFromPool<Component>(poolKey) : null;
        }

        public void Return(Component item)
        {
            if (PoolingManager.Instance != null && item != null)
            {
                PoolingManager.Instance.ReturnToPool(poolKey, item);
            }
        }
    }
}
