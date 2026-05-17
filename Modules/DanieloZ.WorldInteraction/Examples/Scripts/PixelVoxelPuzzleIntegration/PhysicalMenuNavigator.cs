using Sirenix.OdinInspector;
using UnityEngine;

namespace PixelLust.PixelVoxelPuzzle
{
    public sealed class PhysicalMenuNavigator : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Vector2Int selection;
        [SerializeField, ReadOnly] private int confirmCount;
        [SerializeField, Min(1)] private int columns = 3;
        [SerializeField, Min(1)] private int rows = 3;

        public Vector2Int Selection => selection;

        public void Move(Vector2Int direction)
        {
            selection.x = Mathf.Clamp(selection.x + direction.x, 0, columns - 1);
            selection.y = Mathf.Clamp(selection.y + direction.y, 0, rows - 1);
        }

        public void Confirm()
        {
            confirmCount++;
        }

        public void Cancel()
        {
        }
    }
}
