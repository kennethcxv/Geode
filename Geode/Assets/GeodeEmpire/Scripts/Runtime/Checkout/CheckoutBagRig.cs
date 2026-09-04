using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// The carrier. The bag is LAID FLAT on the counter with its mouth pointing back along the counter toward the
    /// goods, so ringing a piece up is one lateral slide into the opening rather than a lift over a rim. What has to
    /// read is the opening: the liner is darker than the outside, or a laid bag reads as a fallen box.
    ///
    /// The seat height is DERIVED from the bag's own drawn bounds, never baked: a baked lift is what put a previous
    /// bag's flank through the counter top.
    /// </summary>
    public sealed class CheckoutBagRig : MonoBehaviour
    {
        public CheckoutPropLibrary Library;
        public Transform Counter;

        public GameObject Bag { get; private set; }
        public CheckoutRig Rig { get; private set; }

        /// <summary>Lay a fresh carrier at the bagging point. Returns its transform.</summary>
        public Transform Lay(Vector3 baggingLocal)
        {
            Clear();
            Bag = Library.Instantiate("shopping_bag", Counter);
            Bag.name = "Bag";
            Rig = Bag.GetComponent<CheckoutRig>();
            // mouth (+Y) along the counter toward the goods; printed face (+Z) up
            Bag.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right);
            Bag.transform.localPosition = baggingLocal;
            DarkenLiner();
            foreach (var c in Bag.GetComponentsInChildren<Collider>()) c.enabled = false;

            // seat it on the counter from its own drawn bounds: a baked lift is what put a previous bag's flank
            // through the counter top, so the 3 mm seat is measured off the model every time it is laid
            float counterTopY = Counter.TransformPoint(baggingLocal).y;
            float lowest = DrawnBounds(Bag).min.y;
            Bag.transform.position += Vector3.up * (counterTopY + 0.003f - lowest);
            return Bag.transform;
        }

        private static Bounds DrawnBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in rs)
            {
                if (!r.enabled || r.gameObject.name.StartsWith("COL_")) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>The liner reads as the opening only if it is darker than the kraft outside.</summary>
        private void DarkenLiner()
        {
            foreach (var r in Bag.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.gameObject.name.Contains("liner")) continue;
                var mats = r.sharedMaterials;
                var copy = new Material[mats.Length];
                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] = new Material(mats[i]);
                    copy[i].SetColor("_BaseColor", new Color(0.09f, 0.075f, 0.045f));
                    copy[i].SetFloat("_Smoothness", 0.05f);
                }
                r.sharedMaterials = copy;
            }
        }

        /// <summary>The interior contract the kit authored, in the bag's own space.</summary>
        public CheckoutPresentation.BagInterior Interior()
        {
            var contents = Rig != null ? Rig.Find("ANCHOR_BagContents") : null;
            var half = Rig != null ? Rig.BagInteriorHalf : new Vector3(0.125f, 0.126f, 0.07f);
            return new CheckoutPresentation.BagInterior
            {
                HalfX = half.x,
                HalfMouth = half.y,
                HalfDepth = half.z,
                Centre = contents != null ? Bag.transform.InverseTransformPoint(contents.position) : new Vector3(0f, 0.14f, 0f),
            };
        }

        /// <summary>
        /// Does this piece go in a bag at all? The answer is the authored size class, not runtime geometry: Golf learned
        /// that a body too big for the carrier is a DESIGN answer (it is carried, not bagged), and inferring it from
        /// bounds every time is how a bag ends up with a club sticking out of it.
        /// </summary>
        public static bool ShouldBag(SpecimenEntity piece)
            => piece != null && (piece.Geology.SizeClass == SizeClass.Small || piece.Geology.SizeClass == SizeClass.Medium);

        public static Vector3 HalfExtents(SpecimenEntity piece)
        {
            var b = piece.FootprintFor(DisplayPose.Closed);
            return b.extents;
        }

        /// <summary>Where the piece rests inside the bag, in the bag's own space.</summary>
        public Vector3 PlacementFor(SpecimenEntity piece, int index = 0)
        {
            var interior = Interior();
            var plan = CheckoutPresentation.BagFit(HalfExtents(piece), interior);
            return CheckoutPresentation.BagPlacement(plan, interior, index);
        }

        /// <summary>The carrier has been handed over: forget it without destroying it.</summary>
        public void Release()
        {
            Bag = null;
            Rig = null;
        }

        public void Clear()
        {
            if (Bag != null) Destroy(Bag);
            Bag = null;
            Rig = null;
        }
    }
}
