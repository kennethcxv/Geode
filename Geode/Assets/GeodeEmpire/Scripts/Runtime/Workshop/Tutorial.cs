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
            /// <summary>Which world object the step is about, so the beacon can point at it. Empty: no object.</summary>
            public string Target;
            /// <summary>A step whose station the player does not own yet waits its turn instead of blocking the chain.</summary>
            public Func<Save.GameState, bool> Available;
            /// <summary>What the acknowledgement says when the step completes.</summary>
            public string Done;
        }

        public static readonly Step[] Steps =
        {
            new Step { Id = "move", Text = "Have a look around the workshop. Move with {Move}, look with {Look}.", DoneBy = "moved", Done = "That is the workshop" },
            new Step { Id = "order", Text = "Money is tight. Order a crate of mystery rocks on the tablet ({Tablet} opens it from anywhere).", DoneBy = "crate_bought", Target = "tablet", Done = "Crate ordered" },
            new Step { Id = "open", Text = "Your crate arrived on the pallet. Open it.", DoneBy = "crate_opened", Target = "crate", Done = "Crate open" },
            new Step { Id = "pickup", Text = "Pick up a rock. Hold {Inspect} to turn it over and {Strike} to tap it: light rocks ring hollow, heavy ones thud solid.", DoneBy = "rock_picked", Target = "crate", Done = "Rock in hand" },
            new Step { Id = "wash", Text = "Quarry rock comes caked in clay. Dunk it in the wash tub by the bench and hold {Interact} to scrub: a clean shell shows its seam and any mineral showing through.", DoneBy = "washed", Target = "washtub", Done = "Washed" },
            new Step { Id = "bench", Text = "Set the rock on the cradle at the cracking bench.", DoneBy = "rock_on_bench", Target = "bench", Done = "On the cradle" },
            new Step { Id = "strike", Text = "Set the chisel on the seam that runs around the middle of the rock (it snaps on when you are close). Hold {Strike} to wind up, release to strike.", DoneBy = "first_strike", Target = "bench", Done = "Struck" },
            new Step { Id = "open_rock", Text = "Each strike chips the shell where the chisel stood. Turn the rock with {Rotate} and work around the whole ring: a careful tap is safe, a heavy blow is fast but can break the crystals inside.", DoneBy = "rock_opened", Target = "bench", Done = "Opened" },
            new Step { Id = "take_specimen", Text = "Take a look, then pick the specimen up.", DoneBy = "specimen_picked", Target = "bench", Done = "Specimen in hand" },
            new Step { Id = "rinse", Text = "Fresh from the break the inside is dusty. Dunk it in the wash tub: the dust goes and the colour comes up.", DoneBy = "rinsed", Target = "washtub", Done = "Rinsed" },
            new Step { Id = "sort", Text = "Ordinary pieces go in the dealer outbox. Anything promising: weigh it on the appraisal scale.", DoneBy = "specimen_sorted", Target = "scale", Done = "Sorted" },
            new Step { Id = "appraise", Text = "The scale explains what makes a piece valuable. Keep favourites in the display cabinet, or sell them.", DoneBy = "appraised", Target = "scale", Done = "Appraised" },
            new Step { Id = "display", Text = "A piece you want to keep goes in the display cabinet. It is out of the career for good, and the room is better for it.", DoneBy = "displayed", Target = "cabinet", Done = "On display" },
            new Step { Id = "ship", Text = "When the outbox has a few pieces, press the dealer intercom to sell them.", DoneBy = "shipped", Target = "intercom", Done = "Shipped" },
            new Step { Id = "upgrade", Text = "Profit. Check the tablet: a new supplier and bench upgrades change how you play.", DoneBy = "upgrade_or_crate", Target = "tablet", Done = "Bought" },
            new Step { Id = "retail", Text = "The showroom next door pays more than the dealer, if a customer wants the piece: put an appraised specimen on a sales shelf and keep working.", DoneBy = "for_sale", Target = "shelf", Done = "For sale" },
            new Step { Id = "checkout", Text = "When a customer waits at the counter, take the register. Ring the piece up, take their card or their cash, count the change out of the drawer and hand the bag across.", DoneBy = "checkout", Target = "counter", Done = "Served" },
            new Step
            {
                Id = "build",
                Text = "Your machine was delivered crated in the receiving bay, not installed. Press {Build} to open build mode and put it where you want it: the ghost turns red and says why if a spot will not work.",
                DoneBy = "fixture_placed", Target = "delivery", Done = "Sited",
                Available = st => Build.PlaceableFixture.AnyCratedFor(st),
            },
            new Step
            {
                Id = "inventory",
                Text = "Press {Inventory} for the stock book: everything the business owns, what it is worth, and which room it is standing in.",
                DoneBy = "inventory_opened", Done = "That is the stock book",
                Available = st => st.Specimens.Count > 0,
            },
            new Step
            {
                Id = "saw", Text = "A rock too big or too solid to crack can be cut instead: clamp it in the trim saw and take a face off it.",
                DoneBy = "saw_cut", Target = "saw", Done = "Cut",
                Available = st => st.HasUpgrade(Economy.UpgradeCatalog.TrimSaw),
            },
            new Step
            {
                Id = "polish", Text = "A cut face is dull until it is lapped. Hold the piece against the polish lap and work the whole face.",
                DoneBy = "polished", Target = "lap", Done = "Polished",
                Available = st => st.HasUpgrade(Economy.UpgradeCatalog.PolishLap),
            },
        };

        public static event Action Changed;
        private static readonly HashSet<string> _sessionDone = new HashSet<string>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; _sessionDone.Clear(); }

        /// <summary>The step being taught, or null when the tutorial is done or switched off.</summary>
        public static Step Current
        {
            get
            {
                var s = GameSession.Instance != null ? GameSession.Instance.State : null;
                if (s == null || !GameSettings.Current.ShowTutorial) return null;
                foreach (var st in Steps)
                {
                    if (s.TutorialDone(st.Id)) continue;
                    if (st.Available != null && !st.Available(s)) continue;   // its station is not built yet: it waits
                    return st;
                }
                return null;
            }
        }

        /// <summary>Raised as a step is finished, with the step, so the HUD can acknowledge it before it disappears.</summary>
        public static event Action<Step> Completed;

        /// <summary>Teach it again from the beginning: every step is cleared and the first hint comes back.</summary>
        public static void Restart()
        {
            var s = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (s == null) return;
            s.TutorialSteps.Clear();
            _sessionDone.Clear();
            GameSettings.Current.ShowTutorial = true;
            Changed?.Invoke();
            GameSession.Instance.QueueSave("tutorial-restart");
        }

        public static void Notify(string doneBy)
        {
            var s = GameSession.Instance != null ? GameSession.Instance.State : null;
            if (s == null) return;
            bool changed = false;
            Step finished = null;
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
                finished = st;
                changed = true;
            }
            if (changed)
            {
                if (finished != null) Completed?.Invoke(finished);
                Changed?.Invoke();
                GameSession.Instance.QueueSave("tutorial");
            }
        }

        public static string Format(string text)
        {
            return text.Replace("{Move}", GameInput.Glyph("Move")).Replace("{Look}", GameInput.Glyph("Look"))
                .Replace("{Interact}", GameInput.Glyph("Interact")).Replace("{Strike}", GameInput.Glyph("Strike"))
                .Replace("{Inspect}", GameInput.Glyph("Inspect")).Replace("{Rotate}", GameInput.Glyph("Rotate"))
                .Replace("{Drop}", GameInput.Glyph("Drop")).Replace("{Back}", GameInput.Glyph("Back")).Replace("{Tablet}", GameInput.Glyph("Tablet"))
                .Replace("{Build}", GameInput.Glyph("Build")).Replace("{Inventory}", GameInput.Glyph("Inventory"));
        }
    }
}
