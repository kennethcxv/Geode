using UnityEngine;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;

namespace GeodeEmpire.Retail
{
    /// <summary>
    /// The POS on the counter. Two presses per sale, on purpose: ring it up (the screen lights and the card shows what,
    /// who and how much), then take the money (the drawer kicks open, the buyer takes their piece and goes). Fast,
    /// tactile, never a menu.
    /// </summary>
    public sealed class CheckoutRegister : InteractableBehaviour
    {
        public RetailShop Shop;
        public Transform Drawer;       // slides out along its local -Z on a sale, eases back
        public MeshRenderer Screen;    // the register body; slot 1 is the screen
        public bool RungUp { get; private set; }
        public Customer Current => Shop != null ? Shop.AtCounter : null;
        private float _drawer, _drawerVel, _screenGlow;
        private Vector3 _drawerHome;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        protected override void Awake()
        {
            base.Awake();
            if (Drawer != null) _drawerHome = Drawer.localPosition;
            _mpb = new MaterialPropertyBlock();
        }

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
                _screenGlow = 1f;
                WorkshopAudio.Play("register_beep", transform.position, 0.6f);
                return;
            }
            RungUp = false;
            if (Shop.CompleteSale(c)) { _drawer = 1f; _drawerVel = 0f; }
        }

        private void Update()
        {
            if (Shop != null && Shop.AtCounter == null && RungUp) RungUp = false;
            float dt = Time.deltaTime;
            // the drawer kicks out fast and is pushed back after a beat
            float target = _drawer > 0.5f ? 1f : 0f;
            _drawerVel = Mathf.Lerp(_drawerVel, (target - _drawerOpen) * 18f, 1f - Mathf.Exp(-dt * 14f));
            _drawerOpen = Mathf.Clamp01(_drawerOpen + _drawerVel * dt);
            if (_drawer > 0f) _drawer = Mathf.MoveTowards(_drawer, 0f, dt * 0.55f);
            if (Drawer != null) Drawer.localPosition = _drawerHome + Vector3.back * (0.13f * _drawerOpen);
            // the screen: lit while a sale is rung up, a soft glow otherwise
            float wantGlow = RungUp ? 1f : 0.25f;
            _screenGlow = Mathf.Lerp(_screenGlow, wantGlow, 1f - Mathf.Exp(-dt * 8f));
            if (Screen != null)
            {
                Screen.GetPropertyBlock(_mpb, 1);
                _mpb.SetColor(EmissionId, new Color(0.35f, 0.75f, 0.55f) * (0.2f + 1.6f * _screenGlow));
                Screen.SetPropertyBlock(_mpb, 1);
            }
        }

        private float _drawerOpen;
    }
}
