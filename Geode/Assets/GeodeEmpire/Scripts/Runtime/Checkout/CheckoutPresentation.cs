using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// Pure layout math for the money and the carrier on the counter, ported from Golf's checkoutPaymentPresentation.js
    /// and the bag fit that replaced its inverted clamp. All offsets are COUNTER-LOCAL; the caller resolves them
    /// through the counter transform, so nothing here can bake a world coordinate.
    /// </summary>
    public static class CheckoutPresentation
    {
        public struct Placement { public Vector3 LocalPosition; public Vector3 LocalEuler; }

        /// <summary>
        /// The customer LAYS their money ON THE COUNTER: notes rest flat in a loose readable fan in front of them,
        /// coins flat at the fan's near edge. Height climbs only paper thickness per overlap — a held-up handful can
        /// only ever read as one blob in the air.
        /// </summary>
        public static Placement PresentedTender(float denom, int billIndex, int coinIndex, Vector3 anchorLocal)
        {
            if (Money.IsBill(denom))
                return new Placement
                {
                    LocalPosition = anchorLocal + new Vector3(-0.078f + billIndex * 0.052f,
                                                              0.0016f + billIndex * 0.0016f,
                                                              billIndex % 2 == 1 ? 0.018f : -0.010f),
                    LocalEuler = new Vector3(0f, (-0.10f + ((billIndex % 3) - 1) * 0.14f) * Mathf.Rad2Deg, 0f),
                };
            return new Placement
            {
                LocalPosition = anchorLocal + new Vector3(-0.050f + (coinIndex % 3) * 0.033f,
                                                          0.0022f + (coinIndex / 3) * 0.0032f,
                                                          0.060f + (coinIndex / 3) * 0.028f),
                LocalEuler = new Vector3(0f, 0f, ((coinIndex % 3) - 1) * 0.07f * Mathf.Rad2Deg),
            };
        }

        /// <summary>
        /// Counted change accumulates as a FLAT PILE directly on the bare counter: notes lie flat in a loosely fanned
        /// overlap, coins rest flat beside them. There is no tray — the handoff point names bare counter surface.
        /// </summary>
        public static Placement SelectedChange(float denom, int billIndex, int coinIndex, Vector3 pileLocal)
        {
            if (Money.IsBill(denom))
                return new Placement
                {
                    LocalPosition = pileLocal + new Vector3(-0.105f + billIndex * 0.034f,
                                                            0.0016f + billIndex * 0.0016f,
                                                            -0.028f + (billIndex % 2 == 1 ? 0.016f : -0.008f)),
                    LocalEuler = new Vector3(0f, (0.08f + ((billIndex % 3) - 1) * 0.16f) * Mathf.Rad2Deg, 0f),
                };
            return new Placement
            {
                LocalPosition = pileLocal + new Vector3(0.052f + (coinIndex % 3) * 0.033f,
                                                        0.0022f + (coinIndex / 3) * 0.0032f,
                                                        0.028f + (coinIndex / 3) * 0.030f),
                LocalEuler = new Vector3(0f, 0f, ((coinIndex % 3) - 1) * 0.07f * Mathf.Rad2Deg),
            };
        }

        /// <summary>Once confirmed, every piece belongs to one handful: these are offsets inside that carrier.</summary>
        public static Placement ChangeBundle(float denom, int billIndex, int coinIndex)
        {
            if (Money.IsBill(denom))
                return new Placement
                {
                    LocalPosition = new Vector3(billIndex * 0.006f, billIndex * 0.0015f, billIndex * 0.002f),
                    LocalEuler = new Vector3(0f, billIndex * 0.018f * Mathf.Rad2Deg, 0f),
                };
            return new Placement
            {
                LocalPosition = new Vector3(-0.018f + (coinIndex % 3) * 0.022f,
                                            0.010f + (coinIndex / 3) * 0.003f,
                                            0.034f + (coinIndex / 3) * 0.017f),
                LocalEuler = new Vector3(0f, 0f, ((coinIndex % 3) - 1) * 0.08f * Mathf.Rad2Deg),
            };
        }

        /// <summary>The bag's interior volume, from the authored ANCHOR_BagContents contract, in the bag's own space.</summary>
        public struct BagInterior { public float HalfX, HalfMouth, HalfDepth; public Vector3 Centre; }

        public struct BagFitPlan { public bool StandUp; public int Axis; public Vector3 Half; }

        /// <summary>
        /// Does the body lie down in the bag, or does it have to stand on its longest axis?
        ///
        /// This replaces a clamp that INVERTED ITS OWN BOUNDS the moment the body was wider than the bag, shoving the
        /// item sideways by its own overflow and cutting it through both walls at once. The answer is a design one: a
        /// body that does not fit lying down is stood on its longest axis, so the only thing it can overflow is the one
        /// opening the bag actually has.
        /// </summary>
        public static BagFitPlan BagFit(Vector3 bodyHalf, BagInterior interior)
        {
            float hx = Mathf.Max(0f, bodyHalf.x), hy = Mathf.Max(0f, bodyHalf.y), hz = Mathf.Max(0f, bodyHalf.z);
            if (hx <= interior.HalfX && hy <= interior.HalfMouth && hz <= interior.HalfDepth)
                return new BagFitPlan { StandUp = false, Axis = -1, Half = new Vector3(hx, hy, hz) };
            int axis = hx >= hy && hx >= hz ? 0 : (hz >= hy ? 2 : 1);
            var half = axis == 0 ? new Vector3(hy, hx, hz)
                     : axis == 2 ? new Vector3(hx, hz, hy)
                                 : new Vector3(hx, hy, hz);
            return new BagFitPlan { StandUp = true, Axis = axis, Half = half };
        }

        /// <summary>Where a fitted body sits inside the bag: standing on the interior floor, or lying in a two-column stack.</summary>
        public static Vector3 BagPlacement(BagFitPlan plan, BagInterior interior, int index = 0, float layerStep = 0.075f)
        {
            float floorY = interior.Centre.y - interior.HalfMouth;
            if (plan.StandUp) return new Vector3(interior.Centre.x, floorY + plan.Half.y, interior.Centre.z);
            int column = index % 2;
            int layer = index / 2;
            float slackX = Mathf.Max(0f, interior.HalfX - plan.Half.x);
            float x = Mathf.Clamp(column == 1 ? 0.055f : -0.055f, -slackX, slackX);
            float y = Mathf.Min(floorY + plan.Half.y + layer * Mathf.Min(layerStep, plan.Half.y * 2f + 0.006f),
                                interior.Centre.y + interior.HalfMouth - plan.Half.y);
            return new Vector3(interior.Centre.x + x, y, interior.Centre.z);
        }

        /// <summary>Which of the three busy dots the terminal is showing (3 Hz).</summary>
        public static int TerminalBusyDotPhase(float elapsedSeconds, float hz = 3f)
            => elapsedSeconds <= 0f ? 0 : Mathf.FloorToInt(elapsedSeconds * hz) % 3;
    }
}
