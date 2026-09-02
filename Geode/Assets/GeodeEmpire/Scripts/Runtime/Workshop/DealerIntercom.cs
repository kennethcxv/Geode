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
{
    public sealed class DealerIntercom : InteractableBehaviour
    {
        public SellOutbox Outbox;

        public override bool CanInteract(PlayerInteractor player) => Outbox != null && Outbox.Count > 0 && player.Held == null;

        public override string GetPrompt(PlayerInteractor player)
        {
            int n = Outbox.Count;
            return $"Call dealer: sell {n} piece{(n == 1 ? "" : "s")} for ~{UI.UiKit.Money(Outbox.EstimateTotal())}";
        }

        public override void Interact(PlayerInteractor player) => Outbox.Ship();
    }
}
