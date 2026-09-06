using GeodeEmpire.Audio;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;

namespace GeodeEmpire.Build
{
    /// <summary>The crate itself: interacting opens build mode holding what is inside.</summary>
    public sealed class DeliveryCrate : InteractableBehaviour
    {
        [System.NonSerialized] public FixtureDelivery.Slot Slot;

        public override bool CanInteract(PlayerInteractor player) => Slot?.Fixture != null && player != null && player.Held == null;

        public override string GetPrompt(PlayerInteractor player)
            => Slot?.Fixture != null ? $"Unpack the {Slot.Fixture.DisplayName} and choose where it goes" : null;

        public override void Interact(PlayerInteractor player)
        {
            var f = Slot?.Fixture;
            var mode = BuildMode.Instance;
            if (f == null || mode == null) return;
            WorkshopAudio.Play("ui_click", transform.position, 0.5f, 0.9f);
            mode.EnterHolding(f);
        }
    }
}
