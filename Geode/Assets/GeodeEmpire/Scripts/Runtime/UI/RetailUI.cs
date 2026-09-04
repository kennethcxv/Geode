using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Checkout;
using GeodeEmpire.Retail;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Retail feedback on the HUD: a quiet status chip while someone is browsing or waiting, and the checkout card
    /// (who, what, how much) while the register has a sale rung up.
    /// </summary>
    public sealed class RetailUI : MonoBehaviour
    {
        private RetailShop _shop;
        private CheckoutStation _station;
        private VisualElement _chip, _card;
        private Label _chipText, _cardSection, _cardWho, _cardWhat, _cardPrice, _cardProfit, _cardPrompt;
        private float _flash;
        private string _soldWhat, _soldPrice, _soldProfit;

        private void Start()
        {
            _shop = RetailShop.Instance;
            _station = FindAnyObjectByType<CheckoutStation>();
            var hud = HudController.Instance;
            if (_shop == null || hud == null) { enabled = false; return; }
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            _chip = UiKit.Box(root, "card", "retail-chip");
            _chip.pickingMode = PickingMode.Ignore;
            _chipText = UiKit.Label(_chip, "", "caption");
            _chip.style.display = DisplayStyle.None;

            _card = UiKit.Box(root, "card", "checkout-card");
            _card.pickingMode = PickingMode.Ignore;
            _cardSection = UiKit.Label(_card, "CHECKOUT", "section");
            _cardWho = UiKit.Label(_card, "", "item-sub");
            _cardWhat = UiKit.Label(_card, "", "appraisal-name", "bold");
            _cardPrice = UiKit.Label(_card, "", "appraisal-value");
            _cardProfit = UiKit.Label(_card, "", "item-sub");
            _cardPrompt = UiKit.Label(_card, "", "muted");
            _card.style.display = DisplayStyle.None;
            _shop.Changed += Refresh;
            _shop.SaleCompleted += OnSale;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_shop != null) { _shop.Changed -= Refresh; _shop.SaleCompleted -= OnSale; }
        }

        private void OnSale(Customer c, Save.SpecimenRecord r, float price)
        {
            _flash = 2.2f;
            _soldWhat = r.DisplayName;
            _soldPrice = UiKit.Money(price);
            float dealer = r.EstimatedValue();
            _soldProfit = dealer > 0f ? $"{UiKit.Money(price - dealer)} over the dealer's price" : "";
            Refresh();
        }

        private void Update()
        {
            if (_shop == null || _card == null || _station == null) return;
            var tx = _station.Tx;
            bool rung = tx != null && tx.Items.Count > 0;
            bool station = rung && _station.Active;      // the counter view: a slim strip, the counter itself stays clear
            // the sale card belongs to the counter: it shares the right-hand rail with the workstation panels,
            // so out at the saw or the lap it must be gone, not stacked on top of them
            var cam = Camera.main;
            bool atCounter = station || (cam != null && (cam.transform.position - _station.transform.position).sqrMagnitude < 4.2f * 4.2f);
            if (rung)
            {
                var line = tx.Items[0];
                var rec = _shop.AtCounter != null && _shop.AtCounter.Wanted != null ? _shop.AtCounter.Wanted.Record : null;
                float dealer = rec != null ? rec.EstimatedValue() : 0f;
                _cardWho.text = tx.CustomerName;
                _cardWhat.text = station ? $"{line.Name}   {UiKit.Money(tx.Total)}" : line.Name;
                _cardPrice.text = UiKit.Money(tx.Total);
                _cardProfit.text = dealer > 0f ? $"{UiKit.Money(tx.Total - dealer)} over the dealer's price" : "";
                string status = _station.StatusLine;
                _cardPrompt.text = _station.Busy || string.IsNullOrEmpty(status) ? status : $"[{GameInput.Glyph("Interact")}] {status}";
                _card.RemoveFromClassList("checkout-sold");
            }
            else if (_flash > 0f)
            {
                // the payoff: SOLD, the price, the margin, for a couple of seconds
                _cardWho.text = "SOLD";
                _cardWhat.text = _soldWhat;
                _cardPrice.text = _soldPrice;
                _cardProfit.text = _soldProfit;
                _cardPrompt.text = "Thank you, come again";
                _card.AddToClassList("checkout-sold");
            }
            _card.EnableInClassList("checkout-card-station", station);
            var detail = station ? DisplayStyle.None : DisplayStyle.Flex;
            _cardSection.style.display = detail; _cardWho.style.display = detail; _cardPrice.style.display = detail; _cardProfit.style.display = detail;
            _card.style.display = (rung || _flash > 0f) && atCounter ? DisplayStyle.Flex : DisplayStyle.None;
            // the SOLD payoff waits until the piece has gone across and the station has reset
            if (_flash > 0f && !rung) { _flash -= Time.deltaTime; if (_flash <= 0f) Refresh(); }
        }

        private void Refresh()
        {
            if (_shop == null || _chip == null) return;
            int browsing = 0, queued = 0;
            foreach (var c in _shop.Customers) { if (c == null) continue; if (c.State == Customer.Phase.Queued || c.State == Customer.Phase.ToQueue) queued++; else if (c.State != Customer.Phase.Leaving && c.State != Customer.Phase.Done) browsing++; }
            bool waiting = _shop.AtCounter != null && _shop.AtCounter.Wanted != null;
            string text = waiting ? $"{_shop.AtCounter.Archetype.Name} waiting at the counter{(queued > 0 ? $"  +{queued} in line" : "")}" : browsing > 0 ? $"{browsing} browsing the shop" : "";
            _chipText.text = text;
            _chip.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            _chip.EnableInClassList("retail-chip-waiting", waiting);
        }
    }
}
