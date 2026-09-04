using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>One authored well of the cash drawer: the socket the pieces sit in and the placement contract the kit ships with.</summary>
    [Serializable]
    public sealed class DrawerWellContract
    {
        public string Denomination;   // "1".."50" for notes, "01".."50" cents for coins
        public bool Coin;
        public Transform Socket;
        public Transform Clip;        // note wells only: the retaining clip that rides the top of the stack
        public float WellW, WellD, WallH, Spacing, HingeDrop, PileH;
        public int MaxPieces;
    }

    [Serializable]
    public sealed class NamedRef
    {
        public string Name;
        public Transform Target;
    }

    /// <summary>
    /// The durable binding between a Golf kit model and the runtime: every ANCHOR_*, *_SOCKET, *_MOUNT, screen, key,
    /// tray and clip node is a serialized Transform reference written once by the kit builder (GUID + fileID), so a
    /// renamed node breaks at build time instead of silently at runtime. No string lookups against the scene.
    /// </summary>
    public sealed class CheckoutRig : MonoBehaviour
    {
        public string Stem;
        public List<NamedRef> Refs = new List<NamedRef>();
        public List<DrawerWellContract> Wells = new List<DrawerWellContract>();
        public Renderer Screen;                 // POS_Screen / Terminal_Screen / CustomerDisplay_Screen
        public Vector2Int ScreenPixels;         // authored screen_px
        public Transform Tray;                  // CashDrawer_Tray
        public float TrayTravel;                // authored open_travel_m
        public Vector3 BagInteriorHalf;         // ANCHOR_BagContents: interior_half_x / _mouth / _depth
        public Vector3 TargetDimensions;        // authored target_dimensions_m

        public Transform Find(string name)
        {
            for (int i = 0; i < Refs.Count; i++) if (Refs[i].Name == name) return Refs[i].Target;
            return null;
        }

        public Transform Require(string name)
        {
            var t = Find(name);
            if (t == null) throw new InvalidOperationException($"[CheckoutRig] {Stem} has no reference '{name}'");
            return t;
        }

        public DrawerWellContract Well(string denomination)
        {
            for (int i = 0; i < Wells.Count; i++) if (Wells[i].Denomination == denomination) return Wells[i];
            return null;
        }
    }
}
