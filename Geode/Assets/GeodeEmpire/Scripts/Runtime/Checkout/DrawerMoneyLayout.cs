using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// Pure placement math for the cash drawer, ported from Golf's drawerMoneyLayout.js. Each compartment's authored
    /// socket carries its own contract (interior bounds, wall height, piece cap, note spacing, clip hinge drop); these
    /// functions turn that contract plus a piece count into deterministic transforms that stay inside the compartment.
    /// Deterministic on purpose: the same seed gives the same heap, so a save/reload or a re-open never reshuffles the
    /// till behind the player's back.
    /// </summary>
    public static class DrawerMoneyLayout
    {
        public struct Piece { public Vector3 Offset; public Vector3 Euler; }

        /// <summary>A sin-hash: same denomination and index, same jitter, for the life of the save.</summary>
        public static float Scramble(float denom, int index, int salt)
        {
            float h = Mathf.Sin(denom * 127.1f + index * 311.7f + salt * 74.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        public enum Fill { Empty, Low, Moderate, Full }

        public static Fill FillState(int count, int maxPieces)
        {
            if (count <= 0) return Fill.Empty;
            if (count <= maxPieces * 0.25f) return Fill.Low;
            if (count <= maxPieces * 0.75f) return Fill.Moderate;
            return Fill.Full;
        }

        /// <summary>Notes lie flat and fill their slot: long axis front to back at ~94% of the interior depth.</summary>
        public static Vector2 BillFit(DrawerWellContract meta, float length, float width)
            => new Vector2((meta.WellD * 0.94f) / Mathf.Max(1e-4f, length), (meta.WellW * 0.92f) / Mathf.Max(1e-4f, width));

        /// <summary>A tidy stack with human jitter: centred, one spacing step per note, millimetres of slide and a hair of skew.</summary>
        public static Piece[] BillLayout(DrawerWellContract meta, int count, float denom)
        {
            int n = Mathf.Clamp(count, 0, meta.MaxPieces);
            var outPieces = new Piece[n];
            for (int i = 0; i < n; i++)
                outPieces[i] = new Piece
                {
                    Offset = new Vector3((Scramble(denom, i, 1) - 0.5f) * 0.004f,
                                         0.0015f + i * meta.Spacing,
                                         (Scramble(denom, i, 2) - 0.5f) * 0.005f),
                    Euler = new Vector3(0f, (Scramble(denom, i, 3) - 0.5f) * 0.04f * Mathf.Rad2Deg, 0f),
                };
            return outPieces;
        }

        /// <summary>The retaining clip rides the top of the stack: 0 rests on the slot floor, 1 is level with its hinge.</summary>
        public static float ClipFillRatio(DrawerWellContract meta, int count)
        {
            int n = Mathf.Clamp(count, 0, meta.MaxPieces);
            if (meta.HingeDrop <= 0f) return 0f;
            float stack = n > 0 ? 0.0015f + n * meta.Spacing : 0f;
            return Mathf.Clamp01(stack / meta.HingeDrop);
        }

        /// <summary>
        /// Coins land as a scrambled mound: highest in the middle, spilling toward the walls, every piece resting on the
        /// floor or the layer beneath it, none crossing a divider.
        /// </summary>
        public static Piece[] CoinLayout(DrawerWellContract meta, int count, float coinRadius, float coinThickness, float denom)
        {
            int n = Mathf.Clamp(count, 0, meta.MaxPieces);
            float hw = Mathf.Max(0.001f, meta.WellW / 2f - coinRadius - 0.0015f);
            float hd = Mathf.Max(0.001f, meta.WellD / 2f - coinRadius - 0.0015f);
            int perLayer = Mathf.Max(4, Mathf.FloorToInt((meta.WellW * meta.WellD) / (coinRadius * coinRadius * 5.5f)));
            var outPieces = new Piece[n];
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Sqrt((i % perLayer) / (float)perLayer);
                float ang = i * 2.39996f + Scramble(denom, i, 5) * 0.9f;      // golden angle: an even scatter
                int layer = i / perLayer;
                float shrink = 1f - layer * 0.30f;                            // upper layers pull toward the centre
                float dx = Mathf.Cos(ang) * t * hw * shrink + (Scramble(denom, i, 6) - 0.5f) * coinRadius * 0.5f;
                float dz = Mathf.Sin(ang) * t * hd * shrink + (Scramble(denom, i, 7) - 0.5f) * coinRadius * 0.5f;
                float lean = 0.16f + layer * 0.10f;
                outPieces[i] = new Piece
                {
                    Offset = new Vector3(Mathf.Clamp(dx, -hw, hw),
                                         coinThickness / 2f + layer * coinThickness * 0.85f + Scramble(denom, i, 8) * coinThickness * 0.35f,
                                         Mathf.Clamp(dz, -hd, hd)),
                    Euler = new Vector3((Scramble(denom, i, 9) - 0.5f) * 2f * lean * Mathf.Rad2Deg,
                                        Scramble(denom, i, 10) * 360f,
                                        (Scramble(denom, i, 11) - 0.5f) * 2f * lean * Mathf.Rad2Deg),
                };
            }
            return outPieces;
        }

        /// <summary>Real-world note footprints (metres), used to fit a note to its well.</summary>
        public static Vector2 BillFootprint(float denom)
        {
            int d = Mathf.RoundToInt(denom);
            switch (d)
            {
                case 1: return new Vector2(0.122f, 0.054f);
                case 5: return new Vector2(0.132f, 0.057f);
                case 10: return new Vector2(0.142f, 0.061f);
                default: return new Vector2(0.156f, 0.066f);   // 20 and 50 share a footprint
            }
        }

        /// <summary>Coin blank diameters (metres).</summary>
        public static float CoinDiameter(float denom)
        {
            int c = Mathf.RoundToInt(denom * 100f);
            switch (c)
            {
                case 1: return 0.018f;
                case 5: return 0.021f;
                case 10: return 0.024f;
                case 25: return 0.026f;
                default: return 0.030f;
            }
        }

        /// <summary>
        /// Which authored well holds a denomination. The kit's fourth coin well is labelled 20 because asset Sheet 02
        /// authored a 20-unit piece; the quarter is the canonical coin, so it lives there.
        /// </summary>
        public static string WellKey(float denom)
        {
            if (denom >= 1f) return Mathf.RoundToInt(denom).ToString();
            int cents = Mathf.RoundToInt(denom * 100f);
            if (cents == 25) return "20";
            return cents.ToString("00");
        }

        /// <summary>Which model draws a denomination. The larger Sheet-01 five-cent piece appears in customer tender only.</summary>
        public static string AssetStem(float denom, bool fromTender)
        {
            if (denom >= 1f) return $"cash_bill_{Mathf.RoundToInt(denom)}";
            int cents = Mathf.RoundToInt(denom * 100f);
            if (cents == 5 && fromTender) return "cash_coin_05_sheet01";
            return $"cash_coin_{cents:00}";
        }

        /// <summary>The drawer's own well labels, a tested contract rather than a screenshot.</summary>
        public static string[] BillLabels => new[] { "$1", "$5", "$10", "$20", "$50" };
        public static string[] CoinLabels => new[] { "1c", "5c", "10c", "25c", "50c" };
    }
}
