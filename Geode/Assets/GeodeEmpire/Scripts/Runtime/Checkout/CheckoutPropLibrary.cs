using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// The prefab registry that replaces Golf's path-string kit loader: stem -> prefab, serialized, so a missing model
    /// is a broken reference in the Editor rather than a prop that silently never shows.
    /// </summary>
    [CreateAssetMenu(fileName = "CheckoutPropLibrary", menuName = "Geode Empire/Checkout Prop Library")]
    public sealed class CheckoutPropLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string Stem; public GameObject Prefab; }
        public List<Entry> Entries = new List<Entry>();

        public GameObject PrefabFor(string stem)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].Stem == stem) return Entries[i].Prefab;
            return null;
        }

        public bool Has(string stem) => PrefabFor(stem) != null;

        /// <summary>Instantiate a kit prop. Collision proxies (COL_*) come hidden from the prefab; shadows are cast, not received.</summary>
        public GameObject Instantiate(string stem, Transform parent, float scale = 1f)
        {
            var prefab = PrefabFor(stem);
            if (prefab == null) throw new InvalidOperationException($"[CheckoutPropLibrary] no prefab registered for '{stem}'");
            var go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = stem;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public CheckoutRig Rig(string stem, Transform parent, float scale = 1f) => Instantiate(stem, parent, scale).GetComponent<CheckoutRig>();
    }
}
