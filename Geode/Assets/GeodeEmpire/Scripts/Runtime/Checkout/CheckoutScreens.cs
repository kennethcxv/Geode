using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.UI;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// The three live screens: the POS the cashier reads, the card terminal, and the display that faces the queue.
    /// Each is a UI Toolkit panel rendered to its own RenderTexture and shown unlit on the authored screen face, so a
    /// screen is a real drawn interface rather than a painted texture.
    ///
    /// STAGE_COPY is carried over from Golf verbatim: those lines were written against the states a player actually
    /// sees, and rewording them is how a screen stops matching what the counter is doing.
    /// </summary>
    public sealed class CheckoutScreens : MonoBehaviour
    {
        public static readonly Dictionary<string, (string Headline, string Sub)> StageCopy = new()
        {
            ["waiting"] = ("WAITING FOR CUSTOMER", "The register is ready for the next transaction."),
            ["products-ready"] = ("PRODUCTS READY", "Ring up each piece and drop it in the bag."),
            ["scanning"] = ("BAGGING", "Ring up each piece and drop it in the bag."),
            ["all-items-scanned"] = ("ALL ITEMS RUNG UP", "The customer is confirming how they will pay."),
            ["select-payment"] = ("PAYMENT CONFIRMED", "Opening the selected payment workspace automatically."),
            ["card-payment"] = ("CARD PAYMENT", "Insert the customer card into the chip reader."),
            ["cash-payment"] = ("CASH PAYMENT", "Take the cash the customer laid on the counter."),
            ["change-selection"] = ("SELECT CHANGE", "Count change from the drawer: exact, or up to $5.00 over."),
            ["payment-complete"] = ("PAYMENT COMPLETE", "Payment was accepted successfully."),
            ["bag-transfer"] = ("BAG TO CUSTOMER", "The customer is taking their bag."),
            ["complete"] = ("TRANSACTION COMPLETE", "The customer has been served."),
            ["recovery"] = ("RESTORING", "Putting the counter back to a safe point."),
        };

        // the POS palette, carried over so the screen reads like the reference
        public static readonly Color Cream = Hex("f4eddb"), Paper = Hex("fffaf0"), Green = Hex("173f35"),
            Sage = Hex("a8b9a4"), Charcoal = Hex("272b29"), Muted = Hex("667069"), Brass = Hex("b58a42"),
            BrassPale = Hex("e5d2a8"), White = Hex("fffdf8"), Danger = Hex("9b443d"), DangerPale = Hex("efd8d2"),
            Success = Hex("2f7257"), SuccessPale = Hex("d6e8dc"), Line = Hex("c8c7b8"), Orange = Hex("ef9824"),
            Navy = Hex("163e5a");

        private static Color Hex(string rgb) =>
            new Color(int.Parse(rgb.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                      int.Parse(rgb.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                      int.Parse(rgb.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f);

        private sealed class Panel
        {
            public RenderTexture Texture;
            public UIDocument Document;
            public VisualElement Root;
        }

        private Panel _pos, _terminal, _display;
        private Label _posHeadline, _posSub, _posCustomer, _posTicket, _posTotal, _posPayment, _posHint;
        private VisualElement _posRows, _posStatus, _posCash;
        private Label _cashReceived, _cashTotal, _cashChange, _cashGiving, _cashCaption;
        private Label _termAmount, _termPrompt, _termStatus;
        private Label _displayName, _displayPrice, _displayNote;

        public void Build(CheckoutRig monitor, CheckoutRig terminal, CheckoutRig display)
        {
            _pos = MakePanel("POS", monitor, 1024, 640);
            _terminal = MakePanel("Terminal", terminal, 480, 440);
            _display = MakePanel("CustomerDisplay", display, 512, 300);
            if (_pos != null) BuildPos(_pos.Root);
            if (_terminal != null) BuildTerminal(_terminal.Root);
            if (_display != null) BuildDisplay(_display.Root);
        }

        private Panel MakePanel(string name, CheckoutRig rig, int w, int h)
        {
            if (rig == null || rig.Screen == null) return null;
            if (rig.ScreenPixels.x > 0) { w = rig.ScreenPixels.x; h = rig.ScreenPixels.y; }
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = $"RT_{name}", filterMode = FilterMode.Bilinear };
            rt.Create();
            var settings = Instantiate(Resources.Load<PanelSettings>("UI/GeodePanelSettings"));
            settings.name = $"Panel_{name}";
            settings.targetTexture = rt;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.clearColor = true;
            settings.colorClearValue = Paper;
            var go = new GameObject($"Screen_{name}");
            go.transform.SetParent(transform, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            var root = doc.rootVisualElement;
            root.style.width = w;
            root.style.height = h;
            root.style.flexGrow = 0;

            // the screen face is drawn unlit: it is a lit panel, not a surface catching room light
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = $"M_Screen_{name}" };
            mat.SetTexture("_BaseMap", rt);
            mat.SetColor("_BaseColor", Color.white);
            var mats = rig.Screen.sharedMaterials;
            var replaced = new Material[mats.Length];
            for (int i = 0; i < replaced.Length; i++) replaced[i] = mat;
            rig.Screen.sharedMaterials = replaced;
            return new Panel { Texture = rt, Document = doc, Root = root };
        }

        private static Label Text(VisualElement parent, string text, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var l = new Label(text);
            l.style.fontSize = size;
            l.style.color = color;
            l.style.unityFontStyleAndWeight = style;
            l.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(l);
            return l;
        }

        private static VisualElement Box(VisualElement parent, Color bg, int pad = 0)
        {
            var v = new VisualElement();
            v.style.backgroundColor = bg;
            v.style.paddingLeft = v.style.paddingRight = v.style.paddingTop = v.style.paddingBottom = pad;
            parent.Add(v);
            return v;
        }

        // ---- POS -------------------------------------------------------------------------------------------
        private void BuildPos(VisualElement root)
        {
            root.style.backgroundColor = Cream;
            var header = Box(root, Green);
            header.style.height = 78;
            header.style.paddingLeft = 24;
            header.style.justifyContent = Justify.Center;
            var title = Text(header, "GEODE WORKS", 22, White, FontStyle.Bold);
            title.style.letterSpacing = 2;
            Text(header, "ROCK SHOP  /  COUNTER", 14, BrassPale, FontStyle.Bold);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.paddingLeft = 24; body.style.paddingRight = 24; body.style.paddingTop = 18; body.style.paddingBottom = 18;
            root.Add(body);

            var left = Box(body, Paper, 18);
            left.style.width = 600;
            left.style.marginRight = 24;
            left.style.borderTopLeftRadius = left.style.borderTopRightRadius = 14;
            left.style.borderBottomLeftRadius = left.style.borderBottomRightRadius = 14;
            Text(left, "ORDER", 13, Muted, FontStyle.Bold);
            _posRows = new VisualElement();
            _posRows.style.marginTop = 10;
            left.Add(_posRows);
            _posHint = Text(left, "", 16, Muted);
            _posHint.style.marginTop = 12;

            var right = Box(body, Paper, 18);
            right.style.flexGrow = 1;
            right.style.borderTopLeftRadius = right.style.borderTopRightRadius = 14;
            right.style.borderBottomLeftRadius = right.style.borderBottomRightRadius = 14;
            Text(right, "CURRENT TRANSACTION", 13, Muted, FontStyle.Bold);
            _posCustomer = Text(right, "", 22, Green, FontStyle.Bold);
            _posCustomer.style.marginTop = 8;
            _posTicket = Text(right, "", 13, Muted, FontStyle.Bold);

            _posStatus = Box(right, SuccessPale, 14);
            _posStatus.style.marginTop = 14;
            _posStatus.style.borderTopLeftRadius = _posStatus.style.borderTopRightRadius = 10;
            _posStatus.style.borderBottomLeftRadius = _posStatus.style.borderBottomRightRadius = 10;
            _posHeadline = Text(_posStatus, "", 19, Success, FontStyle.Bold);
            _posSub = Text(_posStatus, "", 15, Charcoal);
            _posSub.style.marginTop = 4;

            var totalRow = new VisualElement();
            totalRow.style.flexDirection = FlexDirection.Row;
            totalRow.style.justifyContent = Justify.SpaceBetween;
            totalRow.style.alignItems = Align.FlexEnd;
            totalRow.style.marginTop = 18;
            right.Add(totalRow);
            Text(totalRow, "TOTAL", 18, Charcoal, FontStyle.Bold);
            _posTotal = Text(totalRow, "$0.00", 38, Green, FontStyle.Bold);
            _posPayment = Text(right, "", 15, Muted, FontStyle.Bold);

            // the cash count owns the whole glass: offering anything else mid-count only orphans an open drawer
            _posCash = Box(root, Orange, 20);
            _posCash.style.position = Position.Absolute;
            _posCash.style.left = 24; _posCash.style.right = 24; _posCash.style.top = 100; _posCash.style.bottom = 24;
            _posCash.style.borderTopLeftRadius = _posCash.style.borderTopRightRadius = 16;
            _posCash.style.borderBottomLeftRadius = _posCash.style.borderBottomRightRadius = 16;
            _posCash.style.display = DisplayStyle.None;
            var cashTitle = Text(_posCash, "CASH PAYMENT", 26, White, FontStyle.Bold);
            cashTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _cashReceived = CashRow(_posCash, "RECEIVED");
            _cashTotal = CashRow(_posCash, "TOTAL");
            _cashChange = CashRow(_posCash, "CHANGE");
            var giving = Box(_posCash, Navy, 14);
            giving.style.marginTop = 14;
            giving.style.flexDirection = FlexDirection.Row;
            giving.style.justifyContent = Justify.SpaceBetween;
            giving.style.borderTopLeftRadius = giving.style.borderTopRightRadius = 12;
            giving.style.borderBottomLeftRadius = giving.style.borderBottomRightRadius = 12;
            Text(giving, "GIVING", 30, White, FontStyle.Bold);
            _cashGiving = Text(giving, "$0.00", 34, White, FontStyle.Bold);
            _cashCaption = Text(_posCash, "", 22, White, FontStyle.Bold);
            _cashCaption.style.marginTop = 12;
        }

        private Label CashRow(VisualElement parent, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 12;
            parent.Add(row);
            Text(row, label, 30, White, FontStyle.Bold);
            return Text(row, "$0.00", 32, White, FontStyle.Bold);
        }

        private void BuildTerminal(VisualElement root)
        {
            root.style.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
            root.style.paddingLeft = 26; root.style.paddingRight = 26; root.style.paddingTop = 30;
            _termStatus = Text(root, "READY", 34, new Color(0.62f, 0.86f, 0.72f), FontStyle.Bold);
            _termAmount = Text(root, "0.00", 96, Color.white, FontStyle.Bold);
            _termAmount.style.marginTop = 30;
            _termPrompt = Text(root, "", 30, new Color(0.75f, 0.79f, 0.84f));
            _termPrompt.style.marginTop = 30;
        }

        private void BuildDisplay(VisualElement root)
        {
            root.style.backgroundColor = new Color(0.03f, 0.05f, 0.05f);
            root.style.paddingLeft = 26; root.style.paddingRight = 26; root.style.paddingTop = 26;
            _displayName = Text(root, "GEODE WORKS", 34, new Color(0.85f, 0.92f, 0.86f), FontStyle.Bold);
            _displayPrice = Text(root, "", 72, new Color(1f, 0.92f, 0.7f), FontStyle.Bold);
            _displayPrice.style.marginTop = 18;
            _displayNote = Text(root, "", 26, new Color(0.7f, 0.76f, 0.72f));
            _displayNote.style.marginTop = 18;
        }

        // ---- updates ---------------------------------------------------------------------------------------
        public void ShowIdle()
        {
            if (_pos == null) return;
            _posCash.style.display = DisplayStyle.None;
            _posRows.Clear();
            _posCustomer.text = "";
            _posTicket.text = "";
            _posTotal.text = UiKit.Money(0f);
            _posPayment.text = "";
            _posHint.text = "";
            SetStage("waiting");
            if (_display != null) { _displayPrice.text = ""; _displayNote.text = "Welcome in."; }
            if (_terminal != null) { _termStatus.text = "READY"; _termAmount.text = ""; _termPrompt.text = ""; }
        }

        private void SetStage(string key)
        {
            if (!StageCopy.TryGetValue(key, out var copy)) copy = ("CHECKOUT", "Follow the counter.");
            _posHeadline.text = copy.Headline;
            _posSub.text = copy.Sub;
            bool warn = key == "change-selection" || key == "recovery";
            _posStatus.style.backgroundColor = warn ? BrassPale : SuccessPale;
            _posHeadline.style.color = warn ? new Color(0.42f, 0.31f, 0.11f) : Success;
        }

        /// <summary>Draw the whole POS from the transaction: one row per line, the stage copy, and the totals.</summary>
        public void ShowTransaction(RegisterTransaction tx, string posState, string hint, int ticketNumber)
        {
            if (_pos == null || tx == null) return;
            _posCash.style.display = posState == "change-selection" || posState == "cash-payment" ? DisplayStyle.Flex : DisplayStyle.None;
            SetStage(posState);
            _posCustomer.text = tx.CustomerName;
            _posTicket.text = $"#{ticketNumber:0000}";
            _posTotal.text = UiKit.Money(tx.Total);
            _posPayment.text = tx.Method == PaymentMethod.None ? "" : $"PAYMENT   {tx.Method.ToString().ToUpperInvariant()}";
            _posHint.text = hint ?? "";

            _posRows.Clear();
            foreach (var item in tx.Items)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 8; row.style.paddingBottom = 8;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = Line;
                var dot = new VisualElement();
                dot.style.width = 14; dot.style.height = 14; dot.style.marginRight = 12;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 7;
                dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 7;
                dot.style.backgroundColor = item.Scanned ? Success : Brass;
                row.Add(dot);
                var name = Text(row, item.Name, 22, item.Scanned ? Muted : Charcoal, item.Scanned ? FontStyle.Normal : FontStyle.Bold);
                name.style.flexGrow = 1;
                Text(row, UiKit.Money(item.Price), 20, Green, FontStyle.Bold);
                _posRows.Add(row);
            }

            if (_posCash.style.display == DisplayStyle.Flex)
            {
                var state = tx.ChangeGivingState(out int delta);
                _cashReceived.text = UiKit.Money(tx.TenderedTotal > 0f ? tx.TenderedTotal : tx.Tendered.Total);
                _cashTotal.text = UiKit.Money(tx.CashTotal);
                _cashChange.text = UiKit.Money(tx.ChangeDue);
                _cashGiving.text = UiKit.Money(tx.HandTotal);
                _cashGiving.style.color = state == ChangeState.Exact ? new Color(0.33f, 0.94f, 0.43f)
                                        : state == ChangeState.Over ? new Color(1f, 0.79f, 0.30f)
                                        : new Color(1f, 0.32f, 0.28f);
                _cashCaption.text = tx.Stage == TxStage.CashTender ? "TAKE THEIR CASH"
                                  : !tx.Deposited ? "SORTING THE RECEIVED CASH"
                                  : state == ChangeState.Exact ? "EXACT CHANGE"
                                  : state == ChangeState.Over ? $"{UiKit.Money(delta / 100f)} OVER - THEY KEEP IT"
                                  : state == ChangeState.Excess ? "TOO MUCH - MAX EXTRA IS $5.00"
                                  : $"SHORT BY {UiKit.Money(-delta / 100f)}";
            }

            if (_display != null)
            {
                _displayPrice.text = UiKit.Money(tx.Total);
                _displayNote.text = tx.Stage == TxStage.Done ? "Thank you." : tx.Items.Count == 1 ? tx.Items[0].Name : $"{tx.Items.Count} pieces";
            }
        }

        public void ShowTerminal(string status, string amount, string prompt, Color? statusColor = null)
        {
            if (_terminal == null) return;
            _termStatus.text = status;
            _termStatus.style.color = statusColor ?? new Color(0.62f, 0.86f, 0.72f);
            _termAmount.text = amount;
            _termPrompt.text = prompt;
        }

        private void OnDestroy()
        {
            foreach (var p in new[] { _pos, _terminal, _display })
                if (p != null && p.Texture != null) { p.Texture.Release(); Destroy(p.Texture); }
        }
    }
}
