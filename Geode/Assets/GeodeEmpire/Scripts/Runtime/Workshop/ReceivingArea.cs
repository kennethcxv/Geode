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

        public Vector3 NextSpot()
        {
            int n = GameSession.Instance != null ? GameSession.Instance.Crates.Count : 0;
            int col = n % 2, row = (n / 2) % 2, stack = n / 4;
            var local = new Vector3((col - 0.5f) * 0.9f, 0.12f + stack * 0.42f, (row - 0.5f) * 0.66f);
            return transform.TransformPoint(local);
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
