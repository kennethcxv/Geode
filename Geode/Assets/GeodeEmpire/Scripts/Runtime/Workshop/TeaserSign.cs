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
{    /// <summary>Restrained future hook: a covered machine you cannot use yet.</summary>
    public sealed class TeaserSign : InteractableBehaviour
    {
        public string Title = "Precision saw";
        public string Message = "A lapidary saw under a tarp. Slabs, cut faces, polished display pieces — the next chapter of the workshop.";
        public override bool CanInteract(PlayerInteractor player) => true;
        public override string GetPrompt(PlayerInteractor player) => $"{Title} (locked)";
        public override void Interact(PlayerInteractor player)
        {
            GameSession.Instance?.Notify(Message, NotificationKind.Info);
            WorkshopAudio.Play("ui_click", transform.position, 0.4f, 0.8f);
        }
    }
}
