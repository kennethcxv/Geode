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
{    /// <summary>The order/upgrade/collection interface lives on a physical tablet.</summary>
    public sealed class OrderTablet : InteractableBehaviour
    {
        public static event Action Opened;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Opened = null; }
        public override bool CanInteract(PlayerInteractor player) => player.Held == null;
        public override string GetPrompt(PlayerInteractor player) => "Use tablet";
        public override void Interact(PlayerInteractor player)
        {
            WorkshopAudio.Play("ui_click", transform.position, 0.5f);
            Opened?.Invoke();
        }
    }
}
