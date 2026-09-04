using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Opens the Curator's Exhibition: the view moves from plinth to plinth over the chosen pieces with a plaque for
    /// each, then a summary of the career so far. Nothing is taken away; the pieces stay on their plinths and the
    /// save continues.
    /// </summary>
    public sealed class ExhibitionDirector : MonoBehaviour
    {
        public static ExhibitionDirector Instance { get; private set; }
        public bool Running { get; private set; }
        public int FirstPlinthSlot = -1;
        private VisualElement _dim, _panel, _plaque;
        private Label _title, _body, _plaqueName, _plaqueLine;
        private Button _ok;
        private Transform _anchor;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; if (_session != null) _session.Loaded -= Abort; }

        private void Start()
        {
            _session = GameSession.Instance;
            if (_session != null) _session.Loaded += Abort;
            var root = HudController.Instance.GetComponent<UIDocument>().rootVisualElement;
            _plaque = UiKit.Box(root, "card");
            _plaque.style.position = Position.Absolute; _plaque.style.left = Length.Percent(50); _plaque.style.top = Length.Percent(74);
            _plaque.style.translate = new Translate(Length.Percent(-50), 0); _plaque.style.alignItems = Align.Center; _plaque.style.display = DisplayStyle.None;
            _plaqueName = UiKit.Label(_plaque, "", "appraisal-name", "bold");
            _plaqueLine = UiKit.Label(_plaque, "", "appraisal-line", "muted");
            _dim = UiKit.Box(root, "panel-dim"); _dim.style.display = DisplayStyle.None;
            _panel = UiKit.Box(_dim, "panel"); _panel.style.width = 720;
            UiKit.Label(_panel, "THE CURATOR'S EXHIBITION", "panel-subtitle");
            _title = UiKit.Label(_panel, "", "panel-title", "bold");
            _body = UiKit.Label(_panel, "", "item-desc"); _body.style.whiteSpace = WhiteSpace.Normal; _body.style.fontSize = 17; _body.style.marginBottom = 18;
            _ok = UiKit.Button(_panel, "Back to work", Close, "btn-primary");
        }

        public int PlinthCount(GameState s) => FirstPlinthSlot >= 0 ? Exhibition.OnPlinths(s, FirstPlinthSlot).Count : 0;

        private GameSession _session;

        /// <summary>A load in the middle of the pass (a relaunch, Continue from the title): nothing was recorded, so the room simply is not open yet. Camera, input and HUD come back.</summary>
        private void Abort()
        {
            if (!Running) return;
            StopAllCoroutines();
            if (_plaque != null) _plaque.style.display = DisplayStyle.None;
            if (_dim != null && _dim.style.display == DisplayStyle.Flex) { _dim.style.display = DisplayStyle.None; CursorController.ExitMenu(); }
            var controller = FindAnyObjectByType<FirstPersonController>();
            if (controller != null) controller.ExitStationView();
            var player = FindAnyObjectByType<PlayerInteractor>();
            if (player != null) player.InputLocked = false;
            if (HudController.Instance != null) HudController.Instance.SetFreeRoamVisible(true);
            Running = false;
        }

        public void Open()
        {
            if (Running) return;
            var s = GameSession.Instance.State;
            if (FirstPlinthSlot < 0 || PlinthCount(s) < 3) { GameSession.Instance.Notify("Set three pieces on the gallery plinths first.", NotificationKind.Warning); return; }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Running = true;
            var session = GameSession.Instance;
            var s = session.State;
            var controller = FindAnyObjectByType<FirstPersonController>();
            var player = FindAnyObjectByType<PlayerInteractor>();
            var pieces = Exhibition.OnPlinths(s, FirstPlinthSlot);
            if (_anchor == null) _anchor = new GameObject("ExhibitionCamera").transform;
            if (player != null) player.InputLocked = true;
            HudController.Instance.SetFreeRoamVisible(false);
            WorkshopAudio.Play2D("discovery", 0.6f);
            Vector3 from = controller != null ? controller.CameraPivot.position : Vector3.zero;
            Quaternion fromRot = controller != null ? controller.CameraPivot.rotation : Quaternion.identity;
            _anchor.SetPositionAndRotation(from, fromRot);
            if (controller != null) controller.EnterStationView(_anchor);
            foreach (var r in pieces)
            {
                var e = session.GetEntity(r.Id);
                if (e == null) continue;
                Vector3 c = e.transform.position + Vector3.up * (e.Radius * 0.5f);
                Vector3 to = c + new Vector3(0f, 0.22f, -0.55f) + Vector3.right * 0.1f;
                Quaternion toRot = Quaternion.LookRotation(c - to, Vector3.up);
                float t = 0f;
                Vector3 p0 = _anchor.position; Quaternion q0 = _anchor.rotation;
                while (t < 1f)
                {
                    t += Time.deltaTime / 1.6f;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    _anchor.SetPositionAndRotation(Vector3.Lerp(p0, to, k), Quaternion.Slerp(q0, toRot, k));
                    yield return null;
                }
                _plaqueName.text = r.DisplayName;
                _plaqueLine.text = TabletUI.Provenance(r, true) + "  •  " + UiKit.Money(r.EstimatedValue());
                _plaque.style.display = DisplayStyle.Flex;
                WorkshopAudio.Play("crystal_chime", e.transform.position, 0.5f, 1.1f);
                VFX.EffectsFactory.Instance?.Glints(c, e.Radius, 14, Color.white);
                yield return new WaitForSeconds(2.6f);
                _plaque.style.display = DisplayStyle.None;
            }
            // the record
            s.ExhibitionsHeld++;
            s.ExhibitionCompletedTicks = System.DateTime.UtcNow.Ticks;
            s.ExhibitedIds.Clear(); foreach (var r in pieces) { s.ExhibitedIds.Add(r.Id); GameState.Log(r, "exhibited", r.EstimatedValue(), "the Curator's Exhibition"); }
            session.FlushSave("exhibition");
            _title.text = s.ExhibitionsHeld == 1 ? "The room is open" : $"Exhibition {s.ExhibitionsHeld}";
            _body.text = "Three pieces, the rock they came from, the hands that opened them.\n\n" + Exhibition.Summary(s) + "\n\nThe workshop is still yours. There is always one more rock.";
            _dim.style.display = DisplayStyle.Flex;
            CursorController.EnterMenu();
            _ok.Focus();
            while (_dim.style.display == DisplayStyle.Flex) yield return null;
            if (controller != null) controller.ExitStationView();
            if (player != null) player.InputLocked = false;
            HudController.Instance.SetFreeRoamVisible(true);
            Running = false;
        }

        private void Close()
        {
            _dim.style.display = DisplayStyle.None;
            CursorController.ExitMenu();
        }
    }
}
