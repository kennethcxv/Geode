using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
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
        private CheckoutRegister _register;
        private VisualElement _chip, _card;
        private Label _chipText, _cardWho, _cardWhat, _cardPrice, _cardPrompt;
        private float _flash;

        private void Start()
        {
            _shop = RetailShop.Instance;
            _register = FindAnyObjectByType<CheckoutRegister>();
            var hud = HudController.Instance;
            if (_shop == null || hud == null) { enabled = false; return; }
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            _chip = UiKit.Box(root, "card", "retail-chip");
            _chip.pickingMode = PickingMode.Ignore;
            _chipText = UiKit.Label(_chip, "", "caption");
            _chip.style.display = DisplayStyle.None;

            _card = UiKit.Box(root, "card", "checkout-card");
            _card.pickingMode = PickingMode.Ignore;
            UiKit.Label(_card, "CHECKOUT", "section");
            _cardWho = UiKit.Label(_card, "", "item-sub");
            _cardWhat = UiKit.Label(_card, "", "appraisal-name", "bold");
            _cardPrice = UiKit.Label(_card, "", "appraisal-value");
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

        private void OnSale(Customer c, Save.SpecimenRecord r, float price) { _flash = 1.6f; }

        private void Update()
        {
            if (_shop == null) return;
            bool rung = _register != null && _register.RungUp && _shop.AtCounter != null && _shop.AtCounter.Wanted != null;
            if (rung)
            {
                var c = _shop.AtCounter;
                _cardWho.text = $"{c.Archetype.Name}  •  {c.Archetype.Blurb}";
                _cardWhat.text = c.Wanted.Record.DisplayName;
                _cardPrice.text = UiKit.Money(c.Wanted.Record.AskingPrice);
                _cardPrompt.text = $"[{GameInput.Glyph("Interact")}] Take payment";
            }
            _card.style.display = rung ? DisplayStyle.Flex : DisplayStyle.None;
            if (_flash > 0f) { _flash -= Time.deltaTime; if (_flash <= 0f) Refresh(); }
        }

        private void Refresh()
        {
            if (_shop == null || _chip == null) return;
            int browsing = 0, queued = 0;
            foreach (var c in _shop.Customers) { if (c == null) continue; if (c.State == Customer.Phase.Queued || c.State == Customer.Phase.ToQueue) queued++; else if (c.State != Customer.Phase.Leaving && c.State != Customer.Phase.Done) browsing++; }
            bool waiting = _shop.AtCounter != null && _shop.AtCounter.Wanted != null;
            string text = waiting ? $"Customer waiting at the counter{(queued > 0 ? $"  +{queued} in line" : "")}" : browsing > 0 ? $"{browsing} browsing the shop" : "";
            _chipText.text = text;
            _chip.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            _chip.EnableInClassList("retail-chip-waiting", waiting);
        }
    }
}
