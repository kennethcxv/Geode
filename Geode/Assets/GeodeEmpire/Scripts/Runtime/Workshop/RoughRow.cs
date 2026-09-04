using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// A row of unopened rough along a bench or a pallet. The reference workshop keeps its stock where it works,
    /// and a bare bench is the surest tell of a prototype; these are real generator specimens so the rock on the
    /// bench is the same rock the game makes. Scenery only: deterministic seeds, no colliders, no interaction,
    /// nothing in the save. Built one piece per frame because a specimen mesh costs a few milliseconds.
    /// </summary>
    public sealed class RoughRow : MonoBehaviour
    {
        public List<Transform> Slots = new List<Transform>();
        /// <summary>Longest side of a piece, in metres. Each slot draws a size between these.</summary>
        public float MinSize = 0.12f, MaxSize = 0.19f;
        public ulong Seed = 0x51F3A9C2D4E67B01UL;
        /// <summary>Washed rough (a piece that has been through the sink) rather than straight off the pallet.</summary>
        public bool Cleaned;

        private readonly List<GameObject> _built = new List<GameObject>();
        private bool _done;

        private void Start() => StartCoroutine(Build());

        private IEnumerator Build()
        {
            // the asset library arrives with the session; the row is scenery, so it can wait for it
            for (int i = 0; i < 240 && (GameSession.Instance == null || GameSession.Instance.Library == null); i++) yield return null;
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) yield break;
            var rng = new SeededRandom(Seed);
            for (int i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                if (slot == null) continue;
                ulong seed = rng.NextULong();
                var geology = SpecimenGenerator.Generate(seed);
                var host = new GameObject("Rough" + i);
                host.transform.SetParent(slot, false);
                var visual = host.AddComponent<SpecimenVisual>();
                visual.Build(geology, new SpecimenCondition { Cleaned = Cleaned ? 1f : rng.Range(0.05f, 0.3f), Rinsed = Cleaned, Opened = false }, lib);
                float radius = Mathf.Max(0.01f, visual.Geometry != null ? visual.Geometry.MaxRadius : 0.06f);
                float size = rng.Range(MinSize, MaxSize);
                host.transform.localScale = Vector3.one * (size / (radius * 2f));
                host.transform.localRotation = Quaternion.Euler(rng.Range(-20f, 20f), rng.Range(0f, 360f), rng.Range(-20f, 20f));
                host.transform.localPosition = new Vector3(0f, size * 0.44f, 0f);
                foreach (var c in host.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var r in host.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                _built.Add(host);
                yield return null;
            }
            _done = true;
        }

        public bool Ready => _done;
    }
}
