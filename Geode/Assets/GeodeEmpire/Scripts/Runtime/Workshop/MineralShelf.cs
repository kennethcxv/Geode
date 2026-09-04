using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.UI;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// The reference row along the appraisal bench: one cut and washed example of every mineral family the
    /// player has met, on a labelled base. The pieces are real specimens from the generator, built from the
    /// same deterministic seed the collection page uses, so the row on the bench and the plate in the tablet
    /// are the same rock. They are scenery — not interactable, not in the save — and they appear as the
    /// encyclopedia fills, which is what makes the bench worth walking past.
    /// </summary>
    public sealed class MineralShelf : MonoBehaviour
    {
        public List<Transform> Slots = new List<Transform>();
        public Font LabelFont;
        public Material LabelMaterial;
        /// <summary>Longest side a reference piece is allowed on its base, in metres.</summary>
        public float PieceSize = 0.15f;

        private readonly List<GameObject> _built = new List<GameObject>();
        private readonly List<MineralId> _shown = new List<MineralId>();
        private Coroutine _job;

        private void Start()
        {
            var s = GameSession.Instance;
            if (s != null) { s.StateChanged += Refresh; s.Loaded += Refresh; }
            Refresh();
        }

        private void OnDestroy()
        {
            var s = GameSession.Instance;
            if (s != null) { s.StateChanged -= Refresh; s.Loaded -= Refresh; }
        }

        private void Refresh()
        {
            var st = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (st == null || Slots.Count == 0) return;
            var want = new List<MineralId>();
            foreach (var e in st.Encyclopedia)
            {
                if (e.Found <= 0) continue;
                want.Add(e.Mineral);
                if (want.Count >= Slots.Count) break;
            }
            if (Same(want)) return;
            _shown.Clear(); _shown.AddRange(want);
            if (_job != null) StopCoroutine(_job);
            _job = StartCoroutine(Build(want));
        }

        private bool Same(List<MineralId> want)
        {
            if (want.Count != _shown.Count) return false;
            for (int i = 0; i < want.Count; i++) if (want[i] != _shown[i]) return false;
            return true;
        }

        /// <summary>One piece per frame: a specimen mesh costs a few milliseconds and the row is scenery.</summary>
        private IEnumerator Build(List<MineralId> want)
        {
            foreach (var go in _built) if (go != null) Destroy(go);
            _built.Clear();
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) yield break;
            for (int i = 0; i < want.Count && i < Slots.Count; i++)
            {
                var slot = Slots[i];
                if (slot == null) continue;
                ulong seed = SpecimenThumbnailer.Instance.SeedFor(want[i]);
                if (seed == 0UL) continue;
                var geology = SpecimenGenerator.Generate(seed);
                var host = new GameObject("Reference_" + want[i]);
                host.transform.SetParent(slot, false);
                var visual = host.AddComponent<SpecimenVisual>();
                visual.Build(geology, new SpecimenCondition { Cleaned = 1f, Rinsed = true, Opened = true }, lib);
                visual.SetCrackState(null, null, 0f, 0.3f);
                if (visual.TopHalf != null) visual.TopHalf.gameObject.SetActive(false);
                // every family is shown at the same size on the bench: this is a reference row, not a size chart
                float radius = Mathf.Max(0.01f, visual.Geometry != null ? visual.Geometry.MaxRadius : 0.06f);
                float scale = PieceSize / (radius * 2f);
                host.transform.localScale = Vector3.one * scale;
                host.transform.localRotation = Quaternion.Euler(-18f, 190f + i * 23f, 0f);
                host.transform.localPosition = new Vector3(0f, PieceSize * 0.42f, 0f);
                foreach (var c in host.GetComponentsInChildren<Collider>()) Destroy(c);

                if (LabelFont != null)
                {
                    // the plate is tipped up off the base so it reads from standing height, the way a museum label does
                    var label = WorldLabel.Create(slot, LabelFont, LabelMaterial, 0.028f, new Color(0.95f, 0.9f, 0.78f), "Nameplate");
                    label.transform.localPosition = new Vector3(0f, 0.012f, -0.098f);
                    label.transform.localRotation = Quaternion.Euler(58f, 180f, 0f);
                    label.Text = MineralCatalog.Get(want[i]).Name.ToUpperInvariant();
                }
                _built.Add(host);
                yield return null;
            }
            _job = null;
        }
    }
}
