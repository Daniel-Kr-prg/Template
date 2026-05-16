using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(WorldDraggable))]
    public class ShadowController : MonoBehaviour
    {
        [Header("Shadow Settings")]
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private float shadowFalloffDistance = 1.5f;
        [SerializeField] private float shadowBaseScale = 0.5f;
        [SerializeField] private float shadowMinOpacity = 0.2f;

        private WorldDraggable draggable;
        private GameObject shadowQuad;
        private Renderer shadowRenderer;
        private int shadowColorProperty;

        private void Awake()
        {
            draggable = GetComponent<WorldDraggable>();
            if (draggable != null)
            {
                draggable.PickedUp += OnPickedUp;
                draggable.Released += OnReleased;
            }
        }

        private void Start()
        {
            CreateShadowQuad();
        }

        private void Update()
        {
            if (draggable == null || !draggable.IsHeld || shadowQuad == null)
                return;

            UpdateShadow();
        }

        private void CreateShadowQuad()
        {
            if (shadowQuad != null)
                return;

            shadowQuad = new GameObject("Shadow");
            shadowQuad.transform.SetParent(transform.parent, false);

            var meshFilter = shadowQuad.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateQuadMesh();

            shadowRenderer = shadowQuad.AddComponent<MeshRenderer>();
            if (shadowMaterial != null)
            {
                shadowRenderer.material = new Material(shadowMaterial);
                shadowColorProperty = Shader.PropertyToID("_Color");
            }

            shadowQuad.SetActive(false);
        }

        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "ShadowQuad";

            var vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(-0.5f, 0, 0.5f)
            };

            var triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2
            };

            var uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            return mesh;
        }

        private void UpdateShadow()
        {
            var ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out var hit, shadowFalloffDistance))
            {
                shadowQuad.SetActive(true);
                shadowQuad.transform.position = hit.point + hit.normal * 0.01f;
                shadowQuad.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                // Масштабировать тень по дистанции
                var distance = hit.distance;
                var scale = 1f - (distance / shadowFalloffDistance);
                shadowQuad.transform.localScale = Vector3.one * scale * shadowBaseScale;

                // Обновить прозрачность
                if (shadowRenderer != null && shadowRenderer.material != null)
                {
                    var color = shadowRenderer.material.GetColor(shadowColorProperty);
                    color.a = Mathf.Lerp(shadowMinOpacity, 1f, scale);
                    shadowRenderer.material.SetColor(shadowColorProperty, color);
                }
            }
            else
            {
                shadowQuad.SetActive(false);
            }
        }

        private void OnPickedUp(WorldDraggable draggable)
        {
            if (shadowQuad != null)
                shadowQuad.SetActive(true);
        }

        private void OnReleased(WorldDraggable draggable)
        {
            if (shadowQuad != null)
                shadowQuad.SetActive(false);
        }

        private void OnDestroy()
        {
            if (shadowQuad != null)
                Destroy(shadowQuad);

            if (draggable != null)
            {
                draggable.PickedUp -= OnPickedUp;
                draggable.Released -= OnReleased;
            }
        }
    }
}
