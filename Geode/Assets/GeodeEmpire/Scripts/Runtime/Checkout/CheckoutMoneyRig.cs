using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// Every physical piece of money at the counter: what is in the drawer's wells, what the customer laid down, and
    /// what the cashier has counted out. Placement comes from DrawerMoneyLayout and CheckoutPresentation, which are
    /// pure and deterministic, so the till never reshuffles itself behind the player.
    /// </summary>
    public sealed class CheckoutMoneyRig : MonoBehaviour
    {
        public CheckoutPropLibrary Library;
        public CheckoutRig DrawerRig;
        public Transform Counter;

        private readonly Dictionary<string, List<GameObject>> _wellPieces = new();
        private readonly List<GameObject> _tender = new();
        private readonly List<GameObject> _change = new();
        private readonly Dictionary<Transform, Quaternion> _clipHome = new();

        /// <summary>Redraw the drawer's contents from a stack. Only the pieces that changed are rebuilt.</summary>
        public void RefreshDrawer(MoneyStack stack)
        {
            if (DrawerRig == null || Library == null) return;
            for (int i = 0; i < Money.Denoms.Length; i++)
            {
                float denom = Money.Denoms[i];
                bool coin = denom < 1f;
                string key = (coin ? "c" : "b") + DrawerMoneyLayout.WellKey(denom);
                var well = DrawerRig.Well(DrawerMoneyLayout.WellKey(denom), coin);
                if (well == null || well.Socket == null) continue;
                int want = stack != null ? stack[i] : 0;
                var list = Pieces(key);
                int capped = Mathf.Min(want, well.MaxPieces);
                while (list.Count > capped) { var last = list[list.Count - 1]; list.RemoveAt(list.Count - 1); if (last != null) Destroy(last); }
                while (list.Count < capped)
                {
                    var go = Library.Instantiate(DrawerMoneyLayout.AssetStem(denom, false), well.Socket);
                    go.name = $"{key}_{list.Count}";
                    list.Add(go);
                }
                Place(well, list, denom, coin);
                if (well.Clip != null)
                {
                    if (!_clipHome.TryGetValue(well.Clip, out var home)) { home = well.Clip.localRotation; _clipHome[well.Clip] = home; }
                    float fill = DrawerMoneyLayout.ClipFillRatio(well, capped);
                    well.Clip.localRotation = Quaternion.Slerp(home, Quaternion.identity, fill);
                }
            }
        }

        private List<GameObject> Pieces(string key)
        {
            if (!_wellPieces.TryGetValue(key, out var list)) { list = new List<GameObject>(); _wellPieces[key] = list; }
            return list;
        }

        /// <summary>
        /// Lay a well's pieces. The offsets are computed in the COUNTER'S frame — across the counter, up, and along the
        /// well's depth — and then converted into the socket's own space, because the kit's drawer nodes carry a baked
        /// axis conversion: taking their local axes at face value stood every note on its edge across the dividers.
        /// </summary>
        private void Place(DrawerWellContract well, List<GameObject> pieces, float denom, bool coin)
        {
            if (pieces.Count == 0 || Counter == null) return;
            var socket = well.Socket;
            Vector3 across = socket.InverseTransformDirection(Counter.right).normalized;    // along the counter
            Vector3 up = socket.InverseTransformDirection(Counter.up).normalized;           // the stack climbs
            Vector3 along = socket.InverseTransformDirection(Counter.forward).normalized;   // the well's depth
            // measured off the imported prefabs, not assumed: a piece's face normal is its root-local +Y and its long
            // axis its root-local +Z, so lying one flat down the well is LookRotation(along, up)
            Quaternion flat = Quaternion.LookRotation(along, up);

            if (coin)
            {
                float r = DrawerMoneyLayout.CoinDiameter(denom) * 0.5f;
                float thickness = well.PileH > 0f ? well.PileH : 0.0032f;
                var layout = DrawerMoneyLayout.CoinLayout(well, pieces.Count, r, thickness, denom);
                for (int i = 0; i < pieces.Count && i < layout.Length; i++)
                {
                    var o = layout[i].Offset;
                    pieces[i].transform.localPosition = across * o.x + up * o.y + along * o.z;
                    pieces[i].transform.localRotation = Quaternion.AngleAxis(layout[i].Euler.y, up) * flat;
                    pieces[i].transform.localScale = Vector3.one;
                }
                return;
            }

            var foot = DrawerMoneyLayout.BillFootprint(denom);
            var fit = DrawerMoneyLayout.BillFit(well, foot.x, foot.y);
            var bills = DrawerMoneyLayout.BillLayout(well, pieces.Count, denom);
            for (int i = 0; i < pieces.Count && i < bills.Length; i++)
            {
                var o = bills[i].Offset;
                pieces[i].transform.localPosition = across * o.x + up * o.y + along * o.z;
                pieces[i].transform.localRotation = Quaternion.AngleAxis(bills[i].Euler.y, up) * flat;
                pieces[i].transform.localScale = new Vector3(fit.y, 1f, fit.x);   // x is the note's width, z its length
            }
        }

        /// <summary>The customer lays their money on the counter: notes fan flat, coins sit at the near edge.</summary>
        public void ShowTender(MoneyStack stack, Vector3 anchorLocal)
        {
            ClearList(_tender);
            if (stack == null || Library == null || Counter == null) return;
            int bill = 0, coin = 0;
            for (int i = 0; i < Money.Denoms.Length; i++)
            {
                float denom = Money.Denoms[i];
                for (int n = 0; n < stack[i]; n++)
                {
                    var p = CheckoutPresentation.PresentedTender(denom, bill, coin, anchorLocal);
                    var go = Library.Instantiate(DrawerMoneyLayout.AssetStem(denom, true), Counter);
                    go.name = $"Tender_{Money.Label(denom)}_{n}";
                    go.transform.localPosition = p.LocalPosition;
                    go.transform.localRotation = Quaternion.Euler(p.LocalEuler);
                    _tender.Add(go);
                    if (Money.IsBill(denom)) bill++; else coin++;
                }
            }
        }

        /// <summary>Counted change piles flat on the bare counter, left of the register block and clear of it.</summary>
        public void ShowChange(MoneyStack hand, Vector3 pileLocal)
        {
            ClearList(_change);
            if (hand == null || Library == null || Counter == null) return;
            int bill = 0, coin = 0;
            for (int i = 0; i < Money.Denoms.Length; i++)
            {
                float denom = Money.Denoms[i];
                for (int n = 0; n < hand[i]; n++)
                {
                    var p = CheckoutPresentation.SelectedChange(denom, bill, coin, pileLocal);
                    var go = Library.Instantiate(DrawerMoneyLayout.AssetStem(denom, false), Counter);
                    go.name = $"Change_{Money.Label(denom)}_{n}";
                    go.transform.localPosition = p.LocalPosition;
                    go.transform.localRotation = Quaternion.Euler(p.LocalEuler);
                    _change.Add(go);
                }
            }
        }

        public IReadOnlyList<GameObject> TenderPieces => _tender;
        public IReadOnlyList<GameObject> ChangePieces => _change;

        public void ClearTender() => ClearList(_tender);
        public void ClearChange() => ClearList(_change);

        public void ClearAll()
        {
            ClearList(_tender);
            ClearList(_change);
            foreach (var list in _wellPieces.Values) ClearList(list);
        }

        private static void ClearList(List<GameObject> list)
        {
            foreach (var go in list) if (go != null) Destroy(go);
            list.Clear();
        }
    }
}
