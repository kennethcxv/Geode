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
                string key = DrawerMoneyLayout.WellKey(denom);
                var well = DrawerRig.Well(key);
                if (well == null || well.Socket == null) continue;
                int want = stack != null ? stack[i] : 0;
                var list = Pieces(key);
                bool coin = denom < 1f;
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

        private void Place(DrawerWellContract well, List<GameObject> pieces, float denom, bool coin)
        {
            if (coin)
            {
                float r = DrawerMoneyLayout.CoinDiameter(denom) * 0.5f;
                var layout = DrawerMoneyLayout.CoinLayout(well, pieces.Count, r, well.PileH > 0f ? well.PileH : 0.0032f, denom);
                for (int i = 0; i < pieces.Count && i < layout.Length; i++)
                {
                    pieces[i].transform.localPosition = layout[i].Offset;
                    pieces[i].transform.localRotation = Quaternion.Euler(layout[i].Euler);
                }
                return;
            }
            // a note lies along the well's depth, not across it: the mesh's long axis is its local X, and the well runs
            // along Z, so the note turns a quarter and is scaled to 94% of the depth and 92% of the width
            var foot = DrawerMoneyLayout.BillFootprint(denom);
            var fit = DrawerMoneyLayout.BillFit(well, foot.x, foot.y);
            var bills = DrawerMoneyLayout.BillLayout(well, pieces.Count, denom);
            for (int i = 0; i < pieces.Count && i < bills.Length; i++)
            {
                pieces[i].transform.localPosition = bills[i].Offset;
                pieces[i].transform.localRotation = Quaternion.Euler(0f, 90f + bills[i].Euler.y, 0f);
                pieces[i].transform.localScale = new Vector3(fit.x, 1f, fit.y);
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
