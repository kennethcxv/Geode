using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;

namespace GeodeEmpire.Retail
{
    /// <summary>
    /// The POS on the counter. Two presses per sale, on purpose: ring it up (the card shows what, who and how much),
    /// then take the money. Fast, tactile, never a menu.
    /// </summary>
    public sealed class CheckoutRegister : InteractableBehaviour
    {
        public RetailShop Shop;
        public bool RungUp { get; private set; }
        public Customer Current => Shop != null ? Shop.AtCounter : null;

        private float _drawer;
        private Transform _screen;

        public override bool CanInteract(PlayerInteractor player) => Shop != null && Shop.AtCounter != null && Shop.AtCounter.Wanted != null && player.Held == null;

        public override string GetPrompt(PlayerInteractor player)
        {
            var c = Current;
            if (c == null || c.Wanted == null) return "";
            float price = c.Wanted.Record.AskingPrice;
            return RungUp ? $"Take {UI.UiKit.Money(price)} from the {c.Archetype.Name.ToLower()}" : $"Ring up {c.Wanted.Record.DisplayName}  {UI.UiKit.Money(price)}";
        }

        public override string GetHint(PlayerInteractor player) => RungUp ? null : "The register reads the tag: press again to take payment";

        public override void Interact(PlayerInteractor player)
        {
            var c = Current;
            if (c == null || c.Wanted == null) return;
            if (!RungUp)
            {
                RungUp = true;
                WorkshopAudio.Play("register_beep", transform.position, 0.6f);
                return;
            }
            RungUp = false;
            if (Shop.CompleteSale(c)) _drawer = 1f;
        }

        private void Update()
        {
            if (Shop != null && Shop.AtCounter == null && RungUp) RungUp = false;
            if (_drawer > 0f) _drawer = Mathf.MoveTowards(_drawer, 0f, Time.deltaTime * 1.4f);
        }
    }
}
