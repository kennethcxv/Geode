using GeodeEmpire.Core;
using NUnit.Framework;
using UnityEngine;

namespace GeodeEmpire.Tests
{
    public sealed class StationInputTests
    {
        private bool _gameplayEnabled, _cursorVisible;
        private CursorLockMode _cursorLock;

        [SetUp]
        public void Prepare()
        {
            Assert.That(CursorController.InMenu, Is.False, "Run outside a live menu or checkout.");
            _gameplayEnabled = GameInput.GameplayEnabled;
            _cursorVisible = Cursor.visible;
            _cursorLock = Cursor.lockState;
            GameInput.Ensure();
            CursorController.Reset();
        }

        [TearDown]
        public void Restore()
        {
            CursorController.Reset();
            GameInput.SetGameplayEnabled(_gameplayEnabled);
            Cursor.lockState = _cursorLock;
            Cursor.visible = _cursorVisible;
        }

        [Test]
        public void OrdinaryMenuBlocksGameplayActions()
        {
            CursorController.EnterMenu();
            Assert.That(GameInput.GameplayEnabled, Is.False);
            Assert.That(CursorController.StationControlsActive, Is.False);
        }

        [Test]
        public void CounterKeepsActionsWithAFreeCursor()
        {
            CursorController.EnterMenu(stationControls: true);
            Assert.That(GameInput.GameplayEnabled, Is.True, "The counter reads the remapped player actions.");
            Assert.That(CursorController.StationControlsActive, Is.True);
            Assert.That(Cursor.visible, Is.True);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        }

        [Test]
        public void NestedMenuBlocksThenRestoresCounterActions()
        {
            CursorController.EnterMenu(stationControls: true);
            CursorController.EnterMenu();
            Assert.That(GameInput.GameplayEnabled, Is.False);
            Assert.That(CursorController.StationControlsActive, Is.False);
            CursorController.ExitMenu();
            Assert.That(GameInput.GameplayEnabled, Is.True);
            Assert.That(CursorController.StationControlsActive, Is.True);
            CursorController.ExitMenu(stationControls: true);
            Assert.That(CursorController.InMenu, Is.False);
            Assert.That(CursorController.StationControlsActive, Is.False);
            Assert.That(GameInput.GameplayEnabled, Is.True);
        }

        [Test]
        public void RemovingCounterKeepsTheRemainingMenuBlocked()
        {
            CursorController.EnterMenu(stationControls: true);
            CursorController.EnterMenu();
            CursorController.ExitMenu(stationControls: true);
            Assert.That(CursorController.InMenu, Is.True);
            Assert.That(GameInput.GameplayEnabled, Is.False);
            CursorController.ExitMenu();
            Assert.That(GameInput.GameplayEnabled, Is.True);
        }

        [Test]
        public void ResetDoesNotLeaveCounterPermissionInTheNextMenu()
        {
            CursorController.EnterMenu(stationControls: true);
            CursorController.Reset();
            CursorController.EnterMenu();
            Assert.That(GameInput.GameplayEnabled, Is.False);
            Assert.That(CursorController.StationControlsActive, Is.False);
        }
    }
}
