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

        private void LayLidOnFloor()
        {
            Lid.localPosition = new Vector3(-0.86f, 0.0f, 0.06f);
            Lid.localRotation = Quaternion.Euler(0f, 14f, 0f);
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
                Vector3 p1 = p0 + new Vector3(-0.42f, 0.05f, 0f);
                Quaternion r1 = Quaternion.Euler(0f, 0f, 78f);
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2.2f;
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    Lid.localPosition = Vector3.Lerp(p0, p1, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 0.18f;
                    Lid.localRotation = Quaternion.Slerp(r0, r1, s);
                    yield return null;
                }
                // lid drops flat on the floor beside the crate
                float t2 = 0f;
                Vector3 pa = Lid.localPosition; Quaternion ra = Lid.localRotation;
                Vector3 pb = new Vector3(-0.86f, 0.0f, 0.06f); Quaternion rb = Quaternion.Euler(0f, 14f, 0f);
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
            var ids = Record.SpecimenIds;
            int n = ids.Count;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(n * 1.5f));
            int rows = Mathf.CeilToInt(n / (float)cols);
            var rng = new SeededRandom(Record.Seed ^ 0xABCDUL);
            for (int i = 0; i < n; i++)
            {
                var rec = _session.State.FindSpecimen(ids[i]);
                if (rec == null || rec.Location != SpecimenLocation.InCrate) continue;
                int cx = i % cols, cz = i / cols;
                float x = (cx - (cols - 1) * 0.5f) / Mathf.Max(1, cols - 1) * (BedSize.x - 0.16f);
                float z = (cz - (rows - 1) * 0.5f) / Mathf.Max(1, rows - 1) * (BedSize.y - 0.16f);
                if (cols == 1) x = 0f;
                if (rows == 1) z = 0f;
                x += rng.Range(-0.02f, 0.02f);
                z += rng.Range(-0.015f, 0.015f);
                var e = _session.Spawn(rec, Vector3.zero, Quaternion.identity, false);
                var local = new Vector3(x, e.RestHeightOffset(false) + 0.01f, z);
                e.SetPose(Bed.TransformPoint(local), Bed.rotation * Quaternion.Euler(rng.Range(-12f, 12f), rng.Range(0f, 360f), rng.Range(-12f, 12f)));
                e.SetStaticCollidable();
                e.SyncRecordTransform();
                _rocks.Add(e);
            }
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
