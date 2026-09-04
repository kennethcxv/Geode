using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>What a pickable thing at the counter is. The station never guesses from a name.</summary>
    public enum CheckoutTargetKind { Piece, Tender, DrawerWell, TerminalKey, Card, Bag, Terminal }

    /// <summary>
    /// A pickable at the counter: the goods, the customer's laid-down cash, a drawer well, a terminal key, the offered
    /// card, the packed carrier. Carries its own payload so the station reads a typed target instead of a string.
    /// </summary>
    public sealed class CheckoutTarget : MonoBehaviour
    {
        public CheckoutTargetKind Kind;
        public string Payload;          // piece uid, well denomination key, key action
        public float Denom;
        public Renderer[] Highlight;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private bool _lit;

        public void SetHighlight(bool on)
        {
            if (_lit == on || Highlight == null) return;
            _lit = on;
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in Highlight)
            {
                if (r == null) continue;
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                {
                    r.GetPropertyBlock(_mpb, i);
                    _mpb.SetColor(EmissionId, on ? new Color(0.25f, 0.45f, 0.22f) : Color.black);
                    r.SetPropertyBlock(_mpb, i);
                }
            }
        }

        public static CheckoutTarget Attach(GameObject go, CheckoutTargetKind kind, string payload = "", float denom = 0f, Vector3? boxSize = null, Vector3? boxCentre = null)
        {
            // ?? is a trap on UnityEngine.Object: a missing component is a fake-null the operator treats as present
            var t = go.GetComponent<CheckoutTarget>();
            if (t == null) t = go.AddComponent<CheckoutTarget>();
            t.Kind = kind;
            t.Payload = payload;
            t.Denom = denom;
            t.Highlight = go.GetComponentsInChildren<Renderer>();
            if (boxSize.HasValue)
            {
                var box = go.GetComponent<BoxCollider>();
                if (box == null) box = go.AddComponent<BoxCollider>();
                box.size = boxSize.Value;
                box.center = boxCentre ?? Vector3.zero;
                box.isTrigger = true;
                box.enabled = true;
            }
            return t;
        }
    }
}
