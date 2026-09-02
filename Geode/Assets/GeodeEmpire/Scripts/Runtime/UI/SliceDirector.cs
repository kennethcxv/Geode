using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Watches the career and delivers the slice's two restrained future hooks as dealer letters:
    /// the end-of-slice tease (after a few crates) and the premium dealer invitation (collection value).
    /// Never blocks play for long; one panel, one button.
    /// </summary>
    public sealed class SliceDirector : MonoBehaviour
    {
        private GameSession _s;
        private VisualElement _dim, _panel;
        private Label _title, _body;
        private Button _ok;
        private bool _open;

        private void Start()
        {
            _s = GameSession.Instance;
            var root = HudController.Instance.GetComponent<UIDocument>().rootVisualElement;
            _dim = UiKit.Box(root, "panel-dim");
            _dim.style.display = DisplayStyle.None;
            _panel = UiKit.Box(_dim, "panel");
            _panel.style.width = 640;
            UiKit.Label(_panel, "DEALER LETTER", "panel-subtitle");
            _title = UiKit.Label(_panel, "", "panel-title", "bold");
            _body = UiKit.Label(_panel, "", "item-desc");
            _body.style.whiteSpace = WhiteSpace.Normal;
            _body.style.fontSize = 18;
            _body.style.marginBottom = 18;
            _ok = UiKit.Button(_panel, "Back to work", Close, "btn-primary");
            _s.StateChanged += Evaluate;
            _dim.RegisterCallback<NavigationCancelEvent>(e => { Close(); e.StopPropagation(); });
        }

        private void OnDestroy()
        {
            if (_s != null) _s.StateChanged -= Evaluate;
        }

        private void Update()
        {
            if (!_open) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if ((kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame))) Close();
        }

        private void Evaluate()
        {
            var st = _s.State;
            if (st == null || _open) return;
            if (CursorController.InMenu) return;
            var bench = FindAnyObjectByType<Cracking.CrackingBench>();
            if (bench != null && bench.Active) return;

            if (!st.PremiumInviteShown && st.HasSupplier(SupplierCatalog.Premium))
            {
                st.PremiumInviteShown = true;
                Show("An invitation", "\"I saw the pieces in your cabinet. I do not sell to just anyone, but I will sell to you.\"\n\nThe Premium Dealer Crate is now on your tablet: display-grade material, a high floor, and a price to match. Your collection got you here.");
                _s.QueueSave("invite");
                return;
            }
            bool arcDone = st.Stats.CratesPurchased >= 3 && st.Stats.SpecimensOpened >= 18 && (st.Stats.SpecimensSold + st.DisplayedCount()) >= 12;
            if (!st.SliceTeaseShown && arcDone)
            {
                st.SliceTeaseShown = true;
                Show("Word gets around", $"\"Three crates in and you already have a cabinet worth looking at.\"\n\nHere is what comes next for this workshop: a precision saw under the tarp for slabs and cut faces, a bigger cabinet, and dealers who only sell to serious collectors ({UiKit.Money(1500)} on display earns the invitation).\n\nFor now: there is always one more crate.");
                _s.QueueSave("tease");
            }
        }

        private void Show(string title, string body)
        {
            _open = true;
            _title.text = title;
            _body.text = body;
            _dim.style.display = DisplayStyle.Flex;
            CursorController.EnterMenu();
            HudController.Instance.SetFreeRoamVisible(false);
            WorkshopAudio.Play2D("ui_buy", 0.5f, 0.9f);
            _ok.Focus();
        }

        private void Close()
        {
            if (!_open) return;
            _open = false;
            _dim.style.display = DisplayStyle.None;
            CursorController.ExitMenu();
            HudController.Instance.SetFreeRoamVisible(true);
        }
    }
}
