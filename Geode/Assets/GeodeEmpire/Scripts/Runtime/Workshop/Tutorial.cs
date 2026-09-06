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
            new Step { Id = "open", Text = "Your crate arrived in goods-in. Open it.", DoneBy = "crate_opened", Target = "crate", Done = "Crate open" },
            new Step { Id = "pickup", Text = "Pick up a rock. Hold {Inspect} to turn it over and {Strike} to tap it: light rocks ring hollow, heavy ones thud solid.", DoneBy = "rock_picked", Target = "rock", Done = "Rock in hand" },
            new Step { Id = "wash", Text = "Quarry rock comes caked in clay. Put it in the wash basin, then work the brush over it with {Look} and hold {Interact} to scrub. Turn the rock with {Rotate}: the clay you cannot reach stays on.", DoneBy = "washed", Target = "basin", Done = "Washed",
                Available = st => FixtureAvailable(st, "wash_station", Economy.UpgradeCatalog.WashStation, true) },
            new Step { Id = "bench", Text = "Set the rock on the cradle at the cracking bench.", DoneBy = "rock_on_bench", Target = "cradle", Done = "On the cradle" },
            new Step { Id = "strike", Text = "Set the chisel on the seam that runs around the middle of the rock (it snaps on when you are close). Hold {Strike} to wind up, release to strike.", DoneBy = "first_strike", Target = "chisel", Done = "Struck" },
            new Step { Id = "open_rock", Text = "Each strike chips the shell where the chisel stood. Turn the rock with {Rotate} and work around the whole ring: a careful tap is safe, a heavy blow is fast but can break the crystals inside.", DoneBy = "rock_opened", Target = "cradle", Done = "Opened" },
            new Step { Id = "take_specimen", Text = "Take a look, then pick the specimen up.", DoneBy = "specimen_picked", Target = "cradle", Done = "Specimen in hand" },
            new Step { Id = "rinse", Text = "Fresh from the break the inside is dusty. Rinse it in the wash basin: the dust goes and the colour comes up.", DoneBy = "rinsed", Target = "basin", Done = "Rinsed",
                Available = st => FixtureAvailable(st, "wash_station", Economy.UpgradeCatalog.WashStation, true) },
            new Step { Id = "sort", Text = "Put an opened piece in the dealer outbox, or on a sales tray for a customer. Detailed appraisal can wait until you own an inspection bench.", DoneBy = "specimen_sorted", Target = "outbox_tray", Done = "Sorted" },
            new Step { Id = "appraise", Text = "The scale explains what makes a piece valuable. Appraise an opened specimen before choosing what to sell or keep.", DoneBy = "appraised", Target = "pan", Done = "Appraised",
                Available = st => FixtureAvailable(st, "appraisal_station", Economy.UpgradeCatalog.AppraisalStation, true) },
            new Step { Id = "display", Text = "Put a favourite in your collection cabinet. It stays yours; you can take it back out later.", DoneBy = "displayed", Target = "cabinet", Done = "On display",
                Available = st => FixtureAvailable(st, "display_cabinet", Economy.UpgradeCatalog.CollectionCabinet) },
            new Step { Id = "ship", Text = "When the outbox has a few pieces, press the dealer intercom to sell them.", DoneBy = "shipped", Target = "intercom", Done = "Shipped" },
            new Step { Id = "upgrade", Text = "Profit. Check the tablet: a new supplier and bench upgrades change how you play.", DoneBy = "upgrade_or_crate", Target = "tablet", Done = "Bought" },
            new Step { Id = "retail", Text = "Put an opened specimen on a sales tray and turn the door sign to OPEN. Customers can buy it at its estimated price; you do not need an appraisal bench or showroom to start.", DoneBy = "for_sale", Target = "shelf", Done = "For sale" },
            new Step { Id = "checkout", Text = "When a customer waits at the counter, take the register. Ring the piece up, take their card or their cash, count the change out of the drawer and hand the bag across.", DoneBy = "checkout", Target = "register", Done = "Served" },
            new Step
            {
                Id = "build",
                Text = "Bought equipment waits for a free space in goods-in. Unpack its parcel to choose a position, or use {Build}. The ghost turns red and explains why a spot will not work.",
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
                DoneBy = "saw_cut", Target = "vise", Done = "Cut",
                Available = st => FixtureAvailable(st, "trim_saw", Economy.UpgradeCatalog.TrimSaw),
            },
            new Step
            {
                Id = "polish", Text = "A cut face is dull until it is lapped. Hold the piece against the polish lap and work the whole face.",
                DoneBy = "polished", Target = "platen", Done = "Polished",
                Available = st => FixtureAvailable(st, "flat_lap", Economy.UpgradeCatalog.PolishLap),
            },
        };

        private static bool FixtureAvailable(Save.GameState state, string fixtureId, string upgrade, bool legacyInstalled = false)
        {
            if (state == null) return false;
            bool legacy = state.LayoutRevision < Build.AstraWorkshop.Revision;
            if (!state.HasUpgrade(upgrade) && !(legacy && legacyInstalled)) return false;
            var pose = state.Fixture(fixtureId);
            // Legacy scenes supplied the basic stations. In the Astra layout ownership alone is not installation.
            return pose != null ? pose.Placed : legacy;
        }

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
                return CurrentFor(s);
            }
        }

        public static Step CurrentFor(Save.GameState state)
        {
            if (state == null) return null;
            foreach (var step in Steps)
                if (!state.TutorialDone(step.Id) && (step.Available == null || step.Available(state))) return step;
            return null;
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
            var finished = RecordAction(s, doneBy);
            if (finished == null) return;
            Completed?.Invoke(finished);
            Changed?.Invoke();
            GameSession.Instance.QueueSave("tutorial");
        }

        /// <summary>Apply a real action to the saved lesson state, retaining lessons for equipment not yet installed.</summary>
        public static Step RecordAction(Save.GameState s, string doneBy)
        {
            if (s == null) return null;
            Step finished = null;
            int currentIndex = Steps.Length;
            var current = CurrentFor(s);
            for (int i = 0; i < Steps.Length; i++) if (Steps[i] == current) { currentIndex = i; break; }
            for (int i = 0; i < Steps.Length; i++)
            {
                var st = Steps[i];
                if (st.DoneBy != doneBy || s.TutorialDone(st.Id)) continue;
                if (i - currentIndex <= 2)
                {
                    // completing the next step or the one after implicitly completes the ones before it
                    foreach (var prev in Steps)
                    {
                        if (!s.TutorialDone(prev.Id) && (prev == st || prev.Available == null || prev.Available(s)))
                            s.TutorialSteps.Add(prev.Id);
                        if (prev == st) break;
                    }
                }
                else
                {
                    // a far jump (buying an upgrade before the first crate) only ticks that step; the hints keep teaching
                    s.TutorialSteps.Add(st.Id);
                }
                finished = st;
            }
            return finished;
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
