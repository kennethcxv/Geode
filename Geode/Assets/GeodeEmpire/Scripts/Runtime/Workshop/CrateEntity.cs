using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{
    /// <summary>The physical delivered crate: pry the lid, see the rocks, take them one at a time.</summary>
    public sealed class CrateEntity : InteractableBehaviour
    {
        public CrateRecord Record { get; private set; }
        public Transform Lid;
        public Transform Bed;          // where rocks sit
        public Vector2 BedSize = new Vector2(0.5f, 0.34f);

        private GameSession _session;
        private readonly List<SpecimenEntity> _rocks = new List<SpecimenEntity>();
        private bool _opening;
        private AudioSource _audio;

        public bool IsOpened => Record.Opened;
        public int RemainingRocks
        {
            get { int n = 0; foreach (var r in _rocks) if (r != null && r.Record.Location == SpecimenLocation.InCrate) n++; return n; }
        }

        public static CrateEntity Create(CrateRecord record, GameSession session)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Crate");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("Crate");
            go.name = "Crate_" + record.Id;
            var c = go.GetComponent<CrateEntity>();
            if (c == null) c = go.AddComponent<CrateEntity>();
            c.Record = record;
            c._session = session;
            if (c.Lid == null) c.Lid = go.transform.Find("Lid");
            if (c.Bed == null) c.Bed = go.transform.Find("Bed");
            if (c.Bed == null) { c.Bed = new GameObject("Bed").transform; c.Bed.SetParent(go.transform, false); c.Bed.localPosition = new Vector3(0f, 0.07f, 0f); }
            if (record.Opened)
            {
                if (c.Lid != null) c.LayLidOnFloor();
                c.SetOpenColliders();
            }
            session.RegisterCrate(c);
            return c;
        }

        /// <summary>Once open, only the slats collide so rays reach the rocks inside.</summary>
        private void SetOpenColliders()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) box.enabled = false;
        }

        // the lid mesh is 0.61 m deep about its pivot and 0.036 m thick; stood almost upright behind the 0.68 m deep
        // body its top touches the crate's back face and its foot rests on the back edge of the crate's own pallet
        private static readonly Vector3 LidRestPosition = new Vector3(0f, 0.31f, -0.355f);
        private static readonly Quaternion LidRestRotation = Quaternion.Euler(-88f, 0f, 0f);

        private void LayLidOnFloor()
        {
            Lid.localPosition = LidRestPosition;
            Lid.localRotation = LidRestRotation;
        }

        public override bool CanInteract(PlayerInteractor player)
        {
            if (_opening) return false;
            if (!Record.Opened) return player.Held == null;
            return player.Held == null && RemainingRocks == 0;
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            if (!Record.Opened) return "Open crate";
            return "Break down empty crate";
        }

        public override void Interact(PlayerInteractor player)
        {
            if (!Record.Opened) StartCoroutine(OpenRoutine());
            else if (RemainingRocks == 0) BreakDown();
        }

        private IEnumerator OpenRoutine()
        {
            _opening = true;
            Record.Opened = true;
            WorkshopAudio.Play("crate_open", transform.position, 1f);
            if (Lid != null)
            {
                Vector3 p0 = Lid.localPosition; Quaternion r0 = Lid.localRotation;
                Vector3 p1 = p0 + new Vector3(0f, 0.14f, -0.30f);
                Quaternion r1 = Quaternion.Euler(-55f, 0f, 0f);
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2.2f;
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    Lid.localPosition = Vector3.Lerp(p0, p1, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 0.18f;
                    Lid.localRotation = Quaternion.Slerp(r0, r1, s);
                    yield return null;
                }
                // lid settles standing up against the back of the crate (never under a neighbouring crate)
                float t2 = 0f;
                Vector3 pa = Lid.localPosition; Quaternion ra = Lid.localRotation;
                Vector3 pb = LidRestPosition; Quaternion rb = LidRestRotation;
                while (t2 < 1f)
                {
                    t2 += Time.deltaTime * 3.2f;
                    float s2 = t2 * t2;
                    Lid.localPosition = Vector3.Lerp(pa, pb, s2);
                    Lid.localRotation = Quaternion.Slerp(ra, rb, s2);
                    yield return null;
                }
                LayLidOnFloor();
                WorkshopAudio.Play("wood_knock", transform.TransformPoint(Lid.localPosition), 0.7f);
            }
            SetOpenColliders();
            SpawnRocks();
            _session.State.Stats.CratesPurchased = Mathf.Max(_session.State.Stats.CratesPurchased, _session.State.CrateCounter);
            _session.QueueSave("crate-open");
            _session.RaiseStateChanged();
            Tutorial.Notify("crate_opened");
            _opening = false;
        }

        private void SpawnRocks()
        {
            var rng = new SeededRandom(Record.Seed ^ 0xABCDUL);
            // spawn first so every rock knows its real footprint, then shelf-pack them largest-first: rows along the
            // bed depth, a second layer on top when a big estate load does not fit, exactly like a straw-packed crate
            var rocks = new List<SpecimenEntity>();
            foreach (var id in Record.SpecimenIds)
            {
                var rec = _session.State.FindSpecimen(id);
                if (rec == null || rec.Location != SpecimenLocation.InCrate) continue;
                rocks.Add(_session.Spawn(rec, Vector3.zero, Quaternion.identity, false));
            }
            rocks.Sort((a, b) => Footprint(b).CompareTo(Footprint(a)));

            const float gap = 0.018f, margin = 0.03f;
            float usableX = BedSize.x - margin * 2f, usableZ = BedSize.y - margin * 2f;
            var layers = new List<List<List<SpecimenEntity>>>();     // layer -> rows -> rocks
            var layerRows = new List<List<SpecimenEntity>>();
            var row = new List<SpecimenEntity>();
            float rowWidth = 0f, rowsDepth = 0f, rowDepth = 0f;
            foreach (var e in rocks)
            {
                float d = Footprint(e) * 2f;
                if (row.Count > 0 && rowWidth + gap + d > usableX)
                {
                    layerRows.Add(row); rowsDepth += (rowsDepth > 0f ? gap : 0f) + rowDepth;
                    row = new List<SpecimenEntity>(); rowWidth = 0f; rowDepth = 0f;
                }
                if (layerRows.Count > 0 && rowsDepth + gap + Mathf.Max(rowDepth, d) > usableZ && row.Count == 0)
                {
                    layers.Add(layerRows); layerRows = new List<List<SpecimenEntity>>(); rowsDepth = 0f;
                }
                row.Add(e);
                rowWidth += (row.Count > 1 ? gap : 0f) + d;
                rowDepth = Mathf.Max(rowDepth, d);
            }
            if (row.Count > 0) layerRows.Add(row);
            if (layerRows.Count > 0) layers.Add(layerRows);

            float layerY = 0f;
            for (int li = 0; li < layers.Count; li++)
            {
                var rowsInLayer = layers[li];
                float totalDepth = 0f, layerMaxR = 0f;
                var depths = new float[rowsInLayer.Count];
                for (int ri = 0; ri < rowsInLayer.Count; ri++)
                {
                    foreach (var e in rowsInLayer[ri]) { depths[ri] = Mathf.Max(depths[ri], Footprint(e) * 2f); layerMaxR = Mathf.Max(layerMaxR, Footprint(e)); }
                    totalDepth += (ri > 0 ? gap : 0f) + depths[ri];
                }
                float z = -totalDepth * 0.5f;
                for (int ri = 0; ri < rowsInLayer.Count; ri++)
                {
                    var r = rowsInLayer[ri];
                    float width = 0f;
                    foreach (var e in r) width += Footprint(e) * 2f;
                    width += gap * (r.Count - 1);
                    float x = -width * 0.5f;
                    foreach (var e in r)
                    {
                        float fr = Footprint(e);
                        float px = x + fr + rng.Range(-0.008f, 0.008f);
                        float pz = z + depths[ri] * 0.5f + rng.Range(-0.008f, 0.008f);
                        var local = new Vector3(px, layerY + e.RestHeightOffset(false) + 0.01f, pz);
                        e.SetPose(Bed.TransformPoint(local), Bed.rotation * Quaternion.Euler(rng.Range(-12f, 12f), rng.Range(0f, 360f), rng.Range(-12f, 12f)));
                        e.SetStaticCollidable();
                        e.SyncRecordTransform();
                        _rocks.Add(e);
                        x += fr * 2f + gap;
                    }
                    z += depths[ri] + gap;
                }
                layerY += layerMaxR * 2f * 0.9f;   // next layer nestles into the straw on top of this one
            }
        }

        private static float Footprint(SpecimenEntity e)
        {
            if (e.Visual != null && e.Visual.Geometry != null) return Mathf.Max(0.03f, e.Visual.Geometry.MaxRadius);
            return Mathf.Max(0.03f, e.Geology != null ? e.Geology.Size * 1.3f : 0.06f);
        }

        /// <summary>Reload path: put a still-in-crate rock back at its saved spot.</summary>
        public void RestoreRock(SpecimenRecord rec)
        {
            var e = _session.Spawn(rec, rec.WorldPosition, rec.WorldRotation, false);
            e.SetStaticCollidable();
            _rocks.Add(e);
        }

        private void BreakDown()
        {
            WorkshopAudio.Play("wood_knock", transform.position, 0.8f);
            _session.State.Crates.Remove(Record);
            _session.UnregisterCrate(this);
            _session.QueueSave("crate-removed");
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            // rocks that left the crate no longer count against it
            for (int i = _rocks.Count - 1; i >= 0; i--)
                if (_rocks[i] == null || _rocks[i].Record.Location != SpecimenLocation.InCrate) _rocks.RemoveAt(i);
        }
    }
}
