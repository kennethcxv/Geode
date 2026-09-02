using UnityEngine;

namespace GeodeEmpire.Workshop
{
    /// <summary>Marker for where a new/loaded player stands up in the workshop.</summary>
    public sealed class PlayerStart : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.25f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.9f, transform.forward * 0.6f);
        }
    }
}
