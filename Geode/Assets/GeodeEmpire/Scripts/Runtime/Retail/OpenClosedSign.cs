using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.UI;
using UnityEngine;

namespace GeodeEmpire.Retail
{
    /// <summary>A physical control for the same saved opening choice used by the business page.</summary>
    public sealed class OpenClosedSign : InteractableBehaviour
    {
        public WorldLabel Label;
        public WorldLabel OutsideLabel;
        private GameSession _session;
        private void Start()
        {
            _session = GameSession.Instance;
            if (_session != null) _session.StateChanged += Refresh;
            Refresh();
        }
        private void OnDestroy() { if (_session != null) _session.StateChanged -= Refresh; }
        private void Refresh()
        {
            bool open = RetailShop.Instance != null && RetailShop.Instance.IsOpen;
            foreach (var label in new[] { Label, OutsideLabel })
            {
                if (label == null) continue;
                label.Text = open ? "OPEN" : "CLOSED";
                label.SetColor(open ? new Color(.65f, .86f, .72f) : new Color(.94f, .89f, .76f));
            }
        }
        public override bool CanInteract(PlayerInteractor player) => player != null && player.Held == null && RetailShop.Instance != null;
        public override string GetPrompt(PlayerInteractor player)
            => RetailShop.Instance != null && RetailShop.Instance.IsOpen ? "Close shop to new arrivals" : "Open shop";
        public override void Interact(PlayerInteractor player)
        {
            var shop = RetailShop.Instance;
            if (shop == null) return;
            if (!shop.SetOpen(!shop.IsOpen, out string error)) GameSession.Instance?.Notify(error, NotificationKind.Warning);
            CursorController.MarkInputConsumed();
        }
    }
}
