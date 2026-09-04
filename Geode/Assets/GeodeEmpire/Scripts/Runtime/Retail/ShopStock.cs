using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Retail
{
    /// <summary>
    /// The shop's own standing stock on the display walls and inside the island counter. The reference showroom
    /// (R06) is full: shelf after shelf of lit rock, which is what makes it read as a shop rather than a room with
    /// a table in it. These are real generator specimens, so the stock on the shelf is the same rock the game
    /// makes — but scenery only: deterministic seeds, no colliders, no interaction, nothing in the save. The
    /// player's own goods live in the sale slots, which stay empty until they put something in one.
    ///
    /// Built one piece per frame, because a specimen mesh costs a few milliseconds.
    /// </summary>
    public sealed class ShopStock : MonoBehaviour
    {
        public List<Transform> Slots = new List<Transform>();
        /// <summary>Longest side of a piece, in metres.</summary>
        public float MinSize = 0.14f, MaxSize = 0.24f;
        public ulong Seed = 0x2C7B91E5A3D06F4UL;
        /// <summary>
        /// Finished goods: the rock is opened and its top half taken away, so a half stands on the shelf with the
        /// cavity leaning out at the customer. False leaves it as unopened rough — what a shop sells by the crate.
        /// </summary>
        public bool Opened = true;
        /// <summary>Keep only the N largest crystals per piece: shelf dressing, not a specimen under a loupe.</summary>
        public int CrystalBudget = 70;

        private bool _done;
        public bool Ready => _done;

        private void Start() => StartCoroutine(Build());

        private IEnumerator Build()
        {
            for (int i = 0; i < 240 && (GameSession.Instance == null || GameSession.Instance.Library == null); i++) yield return null;
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) yield break;
            var rng = new SeededRandom(Seed);
            for (int i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                if (slot == null) continue;
                var geology = Draw(rng);
                var host = new GameObject("Stock" + i);
                host.transform.SetParent(slot, false);
                var visual = host.AddComponent<SpecimenVisual>();
                visual.CrystalBudget = CrystalBudget;
                visual.Build(geology, new SpecimenCondition { Cleaned = 1f, Rinsed = true, Opened = Opened }, lib);
                float radius = Mathf.Max(0.01f, visual.Geometry != null ? visual.Geometry.MaxRadius : 0.06f);
                float size = rng.Range(MinSize, MaxSize);
                host.transform.localScale = Vector3.one * (size / (radius * 2f));
                if (Opened && visual.TopHalf != null)
                {
                    // the half is displayed the way a shop stands one: cut face out, tipped back onto its own rim
                    visual.TopHalf.gameObject.SetActive(false);
                    visual.BottomHalf.localRotation = Quaternion.Euler(-46f + rng.Range(-7f, 7f), 0f, rng.Range(-5f, 5f));
                    host.transform.localRotation = Quaternion.Euler(0f, rng.Range(-24f, 24f), 0f);
                }
                else
                {
                    host.transform.localRotation = Quaternion.Euler(rng.Range(-18f, 18f), rng.Range(0f, 360f), rng.Range(-18f, 18f));
                }
                foreach (var c in host.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var r in host.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // seat the piece on the shelf: a tipped half's lowest point is nowhere near its pivot, so measure it
                Seat(host.transform, slot);
                _built++;
                yield return null;
            }
            _done = true;
        }

        /// <summary>
        /// The families a rock shop actually puts in the window. A neutral roll returns mostly pale quartz and
        /// calcite, and forty of those on a shelf is a wall of grey; the reference showroom is amethyst, citrine
        /// and agate because that is what sells. Rerolling the seed keeps the geology generator untouched.
        /// </summary>
        private static readonly MineralId[] ShopFamilies =
        {
            MineralId.Amethyst, MineralId.Amethyst, MineralId.Amethyst, MineralId.Citrine, MineralId.Citrine,
            MineralId.Agate, MineralId.Agate, MineralId.Fluorite, MineralId.Malachite, MineralId.Azurite,
            MineralId.Rhodochrosite, MineralId.Vanadinite, MineralId.SmokyQuartz, MineralId.Celestite,
        };

        /// <summary>
        /// Pick a piece worth putting in a window that the machine can also afford to draw. A fine druse is
        /// hundreds of crystals combined into one mesh: forty of those is three and a half million triangles for
        /// set dressing, on a machine with eight gigabytes. Larger crystals at lower density cost a fraction of
        /// that and read better at shelf distance anyway, which is what a shop puts out front.
        /// </summary>
        private static SpecimenGeology Draw(SeededRandom rng)
        {
            SpecimenGeology best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < 48; i++)
            {
                var g = SpecimenGenerator.Generate(rng.NextULong());
                bool family = System.Array.IndexOf(ShopFamilies, g.Mineral) >= 0;
                float score = (family ? 1f : 0f) + g.QualityRoll * 0.5f + g.CrystalScale - g.CrystalDensity;
                if (score > bestScore) { bestScore = score; best = g; }
                if (family && g.QualityRoll > 0.35f && g.CrystalScale > 0.34f && g.CrystalDensity < 0.62f) return g;
            }
            return best;
        }

        /// <summary>Drop the piece until its lowest rendered point rests on the slot's own plane.</summary>
        private static void Seat(Transform host, Transform slot)
        {
            host.localPosition = Vector3.zero;
            var renderers = host.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            float lowest = float.PositiveInfinity;
            foreach (var r in renderers)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;   // the top half is switched off: it must not vote on where the base sits
                var b = r.bounds;   // world space, so convert the corner into the slot's frame
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
                    lowest = Mathf.Min(lowest, slot.InverseTransformPoint(corner).y);
                }
            }
            if (float.IsInfinity(lowest)) return;
            host.localPosition = new Vector3(0f, -lowest, 0f);
        }

        private int _built;
        /// <summary>How many pieces exist so far: the performance pass counts what the shop is actually drawing.</summary>
        public int BuiltCount => _built;
    }
}
