using GeodeEmpire.Core;
using UnityEngine;

namespace GeodeEmpire.Retail
{
    /// <summary>Both shop entrances remain usable by the owner and departing customers after closing.</summary>
    public sealed class ShopEntranceDoor : MonoBehaviour
    {
        public Transform Leaf;
        public float OpenAngle = -95f;
        public float ApproachDistance = 1.6f;
        private Quaternion _closed;
        private float _open;
        private void Awake() { if (Leaf != null) _closed = Leaf.localRotation; }
        private void Update()
        {
            if (Leaf == null) return;
            float range = ApproachDistance * ApproachDistance;
            var owner = GameSession.Instance != null ? GameSession.Instance.Controller : null;
            bool near = owner != null && Near(owner.transform.position, range);
            var shop = RetailShop.Instance;
            if (!near && shop != null)
                foreach (var customer in shop.Customers)
                    if (customer != null && Near(customer.transform.position, range)) { near = true; break; }
            _open = Mathf.MoveTowards(_open, near ? 1f : 0f, Time.deltaTime * 2f);
            Leaf.localRotation = _closed * Quaternion.Euler(0f, OpenAngle * Mathf.SmoothStep(0f, 1f, _open), 0f);
        }
        private bool Near(Vector3 position, float range)
        {
            var delta = position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude < range;
        }
    }
}
