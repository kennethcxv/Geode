using System;
using System.Collections.Generic;
using GeodeEmpire.Core;

namespace GeodeEmpire.Workshop
{
    /// <summary>
    /// Tutorial-by-doing: a linear list of contextual hints keyed by the action that completes them.
    /// Hints are short, disappear once done, and are persisted in the save.
    /// </summary>
    public static class Tutorial
    {
        public sealed class Step
        {
            public string Id;
            public string Text;
            public string DoneBy;
        }

        public static readonly Step[] Steps =
        {
            new Step { Id = "order", Text = "This is your workshop. Money is tight. Order a crate of mystery rocks from the tablet on the side bench ({Tablet} opens it anywhere).", DoneBy = "crate_bought" },
            new Step { Id = "open", Text = "Your crate arrived on the pallet. Open it.", DoneBy = "crate_opened" },
            new Step { Id = "pickup", Text = "Pick up a rock. Hold {Inspect} to turn it over; heavy rocks are usually solid, light ones hollow.", DoneBy = "rock_picked" },
            new Step { Id = "bench", Text = "Set the rock on the cradle at the cracking bench.", DoneBy = "rock_on_bench" },
            new Step { Id = "strike", Text = "Aim the chisel on the seam. Hold {Strike} to wind up, release to strike. Rotate with {Rotate} and work around the shell.", DoneBy = "first_strike" },
            new Step { Id = "open_rock", Text = "Keep working around the ring. Light taps are safer near crystals; heavy blows crack faster but can break them.", DoneBy = "rock_opened" },
            new Step { Id = "take_specimen", Text = "Take a look, then pick the specimen up.", DoneBy = "specimen_picked" },
            new Step { Id = "sort", Text = "Ordinary pieces go in the dealer outbox. Anything promising: weigh it on the appraisal scale.", DoneBy = "specimen_sorted" },
            new Step { Id = "appraise", Text = "The scale explains what makes a piece valuable. Keep favourites in the display cabinet, or sell them.", DoneBy = "appraised" },
            new Step { Id = "ship", Text = "When the outbox has a few pieces, press the dealer intercom to sell them.", DoneBy = "shipped" },
            new Step { Id = "upgrade", Text = "Profit. Check the tablet: a new supplier and bench upgrades change how you play.", DoneBy = "upgrade_or_crate" },
        };

        public static event Action Changed;
        private static readonly HashSet<string> _sessionDone = new HashSet<string>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; _sessionDone.Clear(); }

        public static Step Current
        {
            get
            {
                var s = GameSession.Instance != null ? GameSession.Instance.State : null;
                if (s == null || !GameSettings.Current.ShowTutorial) return null;
                foreach (var st in Steps) if (!s.TutorialDone(st.Id)) return st;
                return null;
            }
        }

        public static void Notify(string doneBy)
        {
            var s = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (s == null) return;
            bool changed = false;
            int currentIndex = Steps.Length;
            for (int i = 0; i < Steps.Length; i++) if (!s.TutorialDone(Steps[i].Id)) { currentIndex = i; break; }
            for (int i = 0; i < Steps.Length; i++)
            {
                var st = Steps[i];
                if (st.DoneBy != doneBy || s.TutorialDone(st.Id)) continue;
                if (i - currentIndex <= 2)
                {
                    // completing the next step or the one after implicitly completes the ones before it
                    foreach (var prev in Steps)
                    {
                        if (!s.TutorialDone(prev.Id)) s.TutorialSteps.Add(prev.Id);
                        if (prev == st) break;
                    }
                }
                else
                {
                    // a far jump (buying an upgrade before the first crate) only ticks that step; the hints keep teaching
                    s.TutorialSteps.Add(st.Id);
                }
                changed = true;
            }
            if (changed)
            {
                Changed?.Invoke();
                GameSession.Instance.QueueSave("tutorial");
            }
        }

        public static string Format(string text)
        {
            return text.Replace("{Interact}", GameInput.Glyph("Interact")).Replace("{Strike}", GameInput.Glyph("Strike"))
                .Replace("{Inspect}", GameInput.Glyph("Inspect")).Replace("{Rotate}", GameInput.Glyph("Rotate"))
                .Replace("{Drop}", GameInput.Glyph("Drop")).Replace("{Back}", GameInput.Glyph("Back")).Replace("{Tablet}", GameInput.Glyph("Tablet"));
        }
    }
}
