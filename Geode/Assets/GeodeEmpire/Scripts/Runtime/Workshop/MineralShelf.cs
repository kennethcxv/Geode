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
        private readonly List<MineralId> _want = new List<MineralId>();
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
            _want.Clear(); _want.AddRange(want);
            if (_job == null) _job = StartCoroutine(Build());
        }

        private bool Same(List<MineralId> want)
        {
            if (want.Count != _shown.Count) return false;
            for (int i = 0; i < want.Count; i++) if (want[i] != _shown[i]) return false;
            return true;
        }

        /// <summary>
        /// One piece per frame, and only the pieces that are actually missing. This row is scenery, and building it
        /// used to cost 385-647 ms of the frame a discovery landed on: a new mineral changed the wanted list, so
        /// every piece already standing there was destroyed and rebuilt, and StartCoroutine runs the body up to the
        /// first yield synchronously, which put a whole cold geode build inside the caller's frame.
        /// </summary>
        private IEnumerator Build()
        {
            yield return null;                      // never build inside the caller's frame
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) { _job = null; yield break; }
            while (!Same(_want))
            {
                var session = GameSession.Instance;
                if (session != null && session.PresentationHold > 0) { yield return null; continue; }
                // drop anything that is no longer wanted, or is in the wrong place
                for (int i = _built.Count - 1; i >= 0; i--)
                    if (i >= _want.Count || i >= _shown.Count || _shown[i] != _want[i])
                    {
                        if (_built[i] != null) Destroy(_built[i]);
                        _built.RemoveAt(i);
                        if (i < _shown.Count) _shown.RemoveAt(i);
                    }
                int next = _built.Count;
                if (next >= _want.Count || next >= Slots.Count) break;
                BuildOne(next, _want[next], lib);
                yield return null;
            }
            _job = null;
        }

        private void BuildOne(int i, MineralId mineral, SpecimenAssetLibrary lib)
        {
            {
                var slot = Slots[i];
                if (slot == null) return;
                ulong seed = SpecimenThumbnailer.Instance.SeedFor(mineral);
                if (seed == 0UL) return;
                var geology = SpecimenGenerator.Generate(seed);
                var host = new GameObject("Reference_" + mineral);
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
                    label.Text = MineralCatalog.Get(mineral).Name.ToUpperInvariant();
                }
                _built.Add(host);
                while (_shown.Count <= i) _shown.Add(mineral);
                _shown[i] = mineral;
            }
        }
    }
}
