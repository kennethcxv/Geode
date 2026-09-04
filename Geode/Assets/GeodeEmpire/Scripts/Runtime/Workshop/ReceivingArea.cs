using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Workshop
{    /// <summary>Where purchased crates land.</summary>
    public sealed class ReceivingArea : MonoBehaviour
    {
        public Vector2 Footprint = new Vector2(1.1f, 0.7f);

        /// <summary>Four pallet cells, front row first (closest to the room), each 1.2 x 0.8 m so a crate and its lid fit.</summary>
        private static readonly Vector3[] Cells =
        {
            new Vector3(-0.6f, 0.12f, 0.4f), new Vector3(0.6f, 0.12f, 0.4f),
            new Vector3(-0.6f, 0.12f, -0.4f), new Vector3(0.6f, 0.12f, -0.4f),
        };
        /// <summary>Stage 3: a third pallet in the row north of the first four (world (1.0, -0.62)).</summary>
        private static readonly Vector3[] Stage3Cells = { new Vector3(0f, 0.12f, 1.23f) };
        private System.Collections.Generic.IEnumerable<Vector3> ActiveCells()
        {
            foreach (var c in Cells) yield return c;
            if (WorkshopExpansion.Stage3Active) foreach (var c in Stage3Cells) yield return c;
        }

        public Vector3 NextSpot()
        {
            var session = GameSession.Instance;
            for (int stack = 0; stack < 3; stack++)
            {
                foreach (var cell in ActiveCells())
                {
                    var spot = transform.TransformPoint(cell + Vector3.up * (stack * 0.44f));
                    bool occupied = false;
                    if (session != null)
                        foreach (var c in session.Crates.Values)
                        {
                            if (c == null) continue;
                            var d = c.transform.position - spot; d.y = 0f;          // a crate still dropping in counts too
                            if (d.sqrMagnitude < 0.45f * 0.45f && Mathf.Abs(c.transform.position.y - spot.y) < 1.6f) { occupied = true; break; }
                        }
                    if (!occupied) return spot;
                }
            }
            return transform.TransformPoint(Cells[0]);
        }

        public void Deliver(CrateRecord crate)
        {
            var session = GameSession.Instance;
            var spot = NextSpot();
            crate.Position = spot;
            crate.Rotation = transform.rotation * Quaternion.Euler(0f, UnityEngine.Random.Range(-8f, 8f), 0f);
            crate.Delivered = true;
            var ce = CrateEntity.Create(crate, session);
            ce.transform.SetPositionAndRotation(spot + Vector3.up * 1.3f, crate.Rotation);
            StartCoroutine(Drop(ce.transform, spot, crate.Rotation));
        }

        private IEnumerator Drop(Transform t, Vector3 target, Quaternion rot)
        {
            float v = 0f, y = t.position.y;
            while (y > target.y)
            {
                v += 9.81f * Time.deltaTime * 1.4f;
                y -= v * Time.deltaTime;
                if (t == null) yield break;
                t.position = new Vector3(target.x, Mathf.Max(y, target.y), target.z);
                yield return null;
            }
            if (t == null) yield break;
            t.SetPositionAndRotation(target, rot);
            WorkshopAudio.Play("thud", target, 1f);
            VFX.EffectsFactory.Instance?.Impact(target + Vector3.up * 0.02f, Vector3.up, 0.9f);
            GameSession.Instance.QueueSave("delivered");
        }
    }
}
