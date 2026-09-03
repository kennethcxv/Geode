using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Interaction;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Retail
{
    /// <summary>A customer's taste and temperament: enough variation that merchandising choices matter, no more.</summary>
    public sealed class CustomerArchetype
    {
        public string Name;
        public string Blurb;                 // one line on the checkout card
        public float BudgetMin, BudgetMax;   // multipliers of the shop's typical asking price (people come in for what is in the window)
        public MineralId[] Likes;            // preferred families
        public float ColourWeight;           // how much saturation matters
        public float SizeWeight;             // how much mass matters
        public float ConditionWeight;        // how much damage puts them off
        public float Patience;               // seconds they will queue
        public float Threshold;              // interest needed to buy
        public Color Jacket, Trousers, Skin, Hair;
        public float Height;

        public static readonly CustomerArchetype[] All =
        {
            new CustomerArchetype { Name = "Collector", Blurb = "knows exactly what this is", BudgetMin = 1.0f, BudgetMax = 2.2f, Likes = new[] { MineralId.Amethyst, MineralId.Fluorite, MineralId.Celestite }, ColourWeight = 0.5f, SizeWeight = 0.3f, ConditionWeight = 1.0f, Patience = 75f, Threshold = 0.55f,
                Jacket = new Color(0.22f, 0.24f, 0.3f), Trousers = new Color(0.2f, 0.18f, 0.17f), Skin = new Color(0.86f, 0.7f, 0.58f), Hair = new Color(0.32f, 0.22f, 0.16f), Height = 1.02f },
            new CustomerArchetype { Name = "Tourist", Blurb = "wants something that sparkles", BudgetMin = 0.35f, BudgetMax = 1.05f, Likes = new[] { MineralId.Agate, MineralId.ClearQuartz, MineralId.Citrine }, ColourWeight = 0.8f, SizeWeight = 0.1f, ConditionWeight = 0.3f, Patience = 40f, Threshold = 0.42f,
                Jacket = new Color(0.78f, 0.36f, 0.26f), Trousers = new Color(0.62f, 0.6f, 0.55f), Skin = new Color(0.7f, 0.52f, 0.4f), Hair = new Color(0.12f, 0.1f, 0.09f), Height = 0.96f },
            new CustomerArchetype { Name = "Decorator", Blurb = "buying for a shelf, not a cabinet", BudgetMin = 0.6f, BudgetMax = 1.35f, Likes = new[] { MineralId.Amethyst, MineralId.Citrine, MineralId.Celestite, MineralId.Agate }, ColourWeight = 0.9f, SizeWeight = 0.6f, ConditionWeight = 0.5f, Patience = 55f, Threshold = 0.5f,
                Jacket = new Color(0.86f, 0.82f, 0.74f), Trousers = new Color(0.15f, 0.15f, 0.16f), Skin = new Color(0.94f, 0.8f, 0.7f), Hair = new Color(0.82f, 0.68f, 0.42f), Height = 1.0f },
            new CustomerArchetype { Name = "Student", Blurb = "counting coins", BudgetMin = 0.25f, BudgetMax = 0.8f, Likes = new[] { MineralId.Pyrite, MineralId.Calcite, MineralId.Fluorite, MineralId.Aragonite }, ColourWeight = 0.3f, SizeWeight = 0.2f, ConditionWeight = 0.2f, Patience = 60f, Threshold = 0.4f,
                Jacket = new Color(0.28f, 0.42f, 0.32f), Trousers = new Color(0.25f, 0.27f, 0.36f), Skin = new Color(0.58f, 0.42f, 0.32f), Hair = new Color(0.08f, 0.07f, 0.07f), Height = 0.93f },
            new CustomerArchetype { Name = "Jeweller", Blurb = "only clean crystal will do", BudgetMin = 0.8f, BudgetMax = 1.8f, Likes = new[] { MineralId.ClearQuartz, MineralId.SmokyQuartz, MineralId.Citrine, MineralId.Amethyst }, ColourWeight = 0.4f, SizeWeight = 0.0f, ConditionWeight = 1.2f, Patience = 50f, Threshold = 0.58f,
                Jacket = new Color(0.14f, 0.13f, 0.14f), Trousers = new Color(0.12f, 0.12f, 0.13f), Skin = new Color(0.9f, 0.76f, 0.66f), Hair = new Color(0.6f, 0.6f, 0.62f), Height = 1.04f },
            new CustomerArchetype { Name = "Rockhound", Blurb = "would rather it were bigger", BudgetMin = 0.5f, BudgetMax = 1.25f, Likes = new[] { MineralId.SmokyQuartz, MineralId.Pyrite, MineralId.Aragonite, MineralId.Celestite }, ColourWeight = 0.2f, SizeWeight = 0.7f, ConditionWeight = 0.6f, Patience = 90f, Threshold = 0.45f,
                Jacket = new Color(0.5f, 0.38f, 0.24f), Trousers = new Color(0.32f, 0.3f, 0.26f), Skin = new Color(0.8f, 0.62f, 0.5f), Hair = new Color(0.45f, 0.3f, 0.2f), Height = 0.99f },
        };
    }

    /// <summary>
    /// One shop visitor: walks in, looks at what is on sale, decides, queues, pays, leaves. NavMeshAgent for the walk,
    /// a procedural stride and head-turn for life. Never persisted; never touches the career state directly.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour
    {
        public enum Phase { Entering, Browsing, Deciding, ToQueue, Queued, AtCounter, Leaving, Done }

        public CustomerArchetype Archetype { get; private set; }
        public Phase State { get; private set; } = Phase.Entering;
        public SpecimenEntity Wanted { get; private set; }
        public float Budget { get; private set; }
        public bool Bought { get; private set; }
        public int Id { get; private set; }

        private RetailShop _shop;
        private NavMeshAgent _agent;
        private Transform _legL, _legR, _armL, _armR, _head, _torso;
        private Transform _handPoint;
        private float _stride;
        private float _timer;
        private float _queueTimer;
        private float _navTimer;             // seconds since the last destination was set
        private int _queueIndex = -1;
        private readonly List<PlacementZone> _plan = new List<PlacementZone>();
        private int _planIndex;
        private PlacementZone _lookingAt;
        private float _bestInterest;
        private SpecimenEntity _best;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public void Init(RetailShop shop, int id)
        {
            _shop = shop;
            Id = id;
            var rng = new SeededRandom((ulong)id * 7919UL + (ulong)Time.frameCount);
            Archetype = CustomerArchetype.All[rng.Range(0, CustomerArchetype.All.Length)];
            // budgets follow the stock: a shop full of $300 amethysts draws people carrying $300, a $40 agate table draws $40
            float anchor = _shop.StockAnchorPrice();
            Budget = Mathf.Clamp(Mathf.Round(anchor * rng.Range(Archetype.BudgetMin, Archetype.BudgetMax) / 5f) * 5f, 10f, 9999f);
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = rng.Range(0.95f, 1.25f);
            _agent.angularSpeed = 240f;
            _agent.acceleration = 4f;
            _agent.stoppingDistance = 0.12f;
            _agent.radius = 0.28f;
            _agent.height = 1.7f;
            _agent.avoidancePriority = 40 + id % 20;
            _legL = Find("LegL"); _legR = Find("LegR"); _armL = Find("ArmL"); _armR = Find("ArmR"); _head = Find("Head"); _torso = Find("Torso");
            transform.localScale = Vector3.one * Archetype.Height * rng.Range(0.97f, 1.03f);
            // colours per material slot, with a little per-person variation
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Color c = i == 0 ? Archetype.Jacket : i == 1 ? Archetype.Trousers : i == 2 ? Archetype.Skin : Archetype.Hair;
                    c = Color.Lerp(c, c * rng.Range(0.85f, 1.15f), 0.6f); c.a = 1f;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor(BaseColorId, c);
                    r.SetPropertyBlock(mpb, i);
                }
            }
            if (_armR != null)
            {
                _handPoint = new GameObject("HandPoint").transform;
                _handPoint.SetParent(_armR, false);
                _handPoint.localPosition = new Vector3(0.04f, -0.62f, 0.12f);
            }
            // plan: look at 1-3 stocked slots, nearest first
            var avail = _shop.Available(this);
            var slots = new List<PlacementZone>();
            foreach (var e in avail) if (e.Zone != null) slots.Add(e.Zone);
            slots.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude));
            int look = Mathf.Min(slots.Count, rng.Range(1, 4));
            for (int i = 0; i < look; i++) _plan.Add(slots[i]);
            if (_plan.Count == 0 && _shop.SaleSlots.Count > 0) _plan.Add(_shop.SaleSlots[rng.Range(0, Mathf.Min(_shop.SaleSlots.Count, 6))]);   // window shopping
            State = Phase.Entering;
            GoToBrowse();
        }

        private Transform Find(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>()) if (t.name == n) return t;
            return null;
        }

        private bool Go(Transform target)
        {
            if (target == null || _agent == null || !_agent.isOnNavMesh) return false;
            _agent.isStopped = false;
            _navTimer = 0f;
            return _agent.SetDestination(target.position);
        }

        /// <summary>
        /// Reached the destination, or as close as this shop lets them: someone (usually the player) standing on the
        /// browse point, or an unreachable point, must never park a customer forever.
        /// </summary>
        private bool Arrived
        {
            get
            {
                if (_agent == null) return true;
                if (_agent.pathPending) return false;
                float remaining = _agent.remainingDistance;
                if (remaining <= _agent.stoppingDistance + 0.08f) return true;
                if (_agent.pathStatus != NavMeshPathStatus.PathComplete && remaining <= 0.7f) return true;
                if (_navTimer > 5f && remaining <= 1.1f && _agent.velocity.sqrMagnitude < 0.0004f) return true;
                return _navTimer > 14f;
            }
        }

        private void GoToBrowse()
        {
            if (_planIndex >= _plan.Count) { Decide(); return; }
            _lookingAt = _plan[_planIndex];
            var bp = _shop.BrowsePointFor(_lookingAt);
            if (bp == null || !Go(bp)) { _planIndex++; GoToBrowse(); return; }
            State = Phase.Browsing;
            _timer = -1f;
        }

        private float Interest(SpecimenRecord r)
        {
            var g = r.Geology;
            float price = r.AskingPrice > 0f ? r.AskingPrice : RetailShop.AskingPrice(r);
            if (price > Budget) return price > Budget * 1.3f ? 0f : 0.15f;
            float f = 0.25f;
            if (System.Array.IndexOf(Archetype.Likes, g.Mineral) >= 0) f += 0.35f;
            f += Archetype.ColourWeight * (g.Saturation - 0.35f) * 0.5f;
            f += Archetype.SizeWeight * Mathf.Clamp01((g.MassKg - 1.2f) / 3f) * 0.4f;
            f -= Archetype.ConditionWeight * r.DamageFraction * 0.8f;
            f += Mathf.Clamp01((float)g.Tier / 5f) * 0.25f;
            // a bargain relative to their budget is tempting
            f += Mathf.Clamp01(1f - price / Budget) * 0.15f;
            return Mathf.Clamp01(f);
        }

        private void Decide()
        {
            State = Phase.Deciding;
            _timer = 0.6f;
        }

        private void Update()
        {
            if (_shop == null || _agent == null) return;
            float dt = Time.deltaTime;
            _navTimer += dt;
            Animate(dt);
            switch (State)
            {
                case Phase.Entering:
                case Phase.Browsing:
                    if (!Arrived) break;
                    if (_timer < 0f)
                    {
                        _timer = Random.Range(2.8f, 5.5f);
                        _agent.isStopped = true;
                    }
                    _timer -= dt;
                    FaceSlot(_lookingAt, dt);
                    if (_timer <= 0f)
                    {
                        var occ = _lookingAt != null ? _lookingAt.First : null;
                        if (occ != null && occ.Record.Location == SpecimenLocation.SaleSlot)
                        {
                            float i = Interest(occ.Record) * Random.Range(0.85f, 1.15f);
                            if (i > _bestInterest) { _bestInterest = i; _best = occ; }
                        }
                        _planIndex++;
                        GoToBrowse();
                    }
                    break;
                case Phase.Deciding:
                    _timer -= dt;
                    if (_timer > 0f) break;
                    if (_best != null && _bestInterest >= Archetype.Threshold && _shop.Available(this).Contains(_best))
                    {
                        Wanted = _best;
                        Carry(_best);
                        WorkshopAudio.Play("rock_pickup", transform.position, 0.4f, 1.1f);
                        _queueIndex = _shop.JoinQueue(this);
                        State = Phase.ToQueue;
                        Go(_shop.QueuePoint(_queueIndex));
                        _queueTimer = 0f;
                    }
                    else Leave(false);
                    break;
                case Phase.ToQueue:
                case Phase.Queued:
                {
                    int idx = _shop.QueueIndex(this);
                    if (idx != _queueIndex) { _queueIndex = idx; Go(_shop.QueuePoint(idx)); }
                    if (Arrived)
                    {
                        _agent.isStopped = true;
                        if (idx == 0)
                        {
                            if (State != Phase.AtCounter)
                            {
                                State = Phase.AtCounter;
                                PresentItem();
                                _shop.ArrivedAtCounter(this);
                            }
                        }
                        else State = Phase.Queued;
                    }
                    FaceTowards(_shop.CounterItemPoint != null ? _shop.CounterItemPoint.position : transform.position + transform.forward, dt);
                    _queueTimer += dt;
                    if (_queueTimer > Archetype.Patience && State != Phase.AtCounter) { PutBack(); Leave(false); }
                    break;
                }
                case Phase.AtCounter:
                    FaceTowards(_shop.CounterItemPoint != null ? _shop.CounterItemPoint.position - transform.right * 0.2f : transform.position + transform.forward, dt);
                    _queueTimer += dt;
                    if (_queueTimer > Archetype.Patience * 1.6f) { PutBack(); Leave(false); }
                    break;
                case Phase.Leaving:
                    if (Arrived || _timer < 0f) Finish();
                    _timer -= dt;
                    break;
            }
        }

        private void Carry(SpecimenEntity e)
        {
            if (e.Zone != null) e.Zone.Take(e, true);
            e.SetPhysics(false);
            e.SetCollidersEnabled(false);
            e.Locked = true;
            e.transform.SetParent(_handPoint != null ? _handPoint : transform, true);
            e.transform.localPosition = Vector3.zero;
            e.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            e.Record.Location = SpecimenLocation.SaleSlot;   // still stock until the money changes hands
            _shop.RefreshLabels();
        }

        private void PresentItem()
        {
            if (Wanted == null || _shop.CounterItemPoint == null) return;
            var e = Wanted;
            e.transform.SetParent(null, true);
            e.SetPose(_shop.CounterItemPoint.position + Vector3.up * e.RestHeightOffset(true), _shop.CounterItemPoint.rotation);
            e.SetStaticCollidable();
            WorkshopAudio.Play("rock_place", e.transform.position, 0.6f);
        }

        /// <summary>The player took the piece off the shelf under their nose: shrug and look for something else.</summary>
        public void ItemGone()
        {
            if (Wanted == null) return;
            Wanted = null; _best = null; _bestInterest = 0f;
            if (State == Phase.ToQueue || State == Phase.Queued || State == Phase.AtCounter) { _shop.LeaveQueue(this); Leave(false); }
        }

        private void PutBack()
        {
            if (Wanted == null) return;
            var e = Wanted;
            Wanted = null;
            e.transform.SetParent(null, true);
            e.Locked = false;
            e.SetCollidersEnabled(true);
            // back onto a free sale slot (its own if still empty)
            PlacementZone home = null;
            foreach (var s in _shop.SaleSlots) if (s.IsEmpty && !s.Locked) { home = s; break; }
            if (home != null) home.Place(e, true);
            else { e.SetPhysics(true); e.Record.Location = SpecimenLocation.World; e.Record.AskingPrice = 0f; }
            _shop.LeaveQueue(this);
            _shop.RefreshLabels();
        }

        public void Paid()
        {
            Bought = true;
            Wanted = null;
            Leave(true);
        }

        private void Leave(bool happy)
        {
            if (!happy && !Bought) _shop.CustomerLeftEmptyHanded();
            State = Phase.Leaving;
            _timer = 40f;
            if (!Go(_shop.OutsidePoint)) Finish();
        }

        private void Finish()
        {
            State = Phase.Done;
            _shop.Remove(this);
            Destroy(gameObject);
        }

        // ---- presentation --------------------------------------------------------------------
        private void Animate(float dt)
        {
            float speed = _agent != null ? _agent.velocity.magnitude : 0f;
            _stride += dt * speed * 5.2f;
            float swing = Mathf.Sin(_stride) * Mathf.Clamp01(speed / 0.5f) * 28f;
            if (_legL != null) _legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            float armSwing = swing * 0.6f;
            if (_armL != null) _armL.localRotation = Quaternion.Euler(-armSwing, 0f, 4f);
            if (_armR != null) _armR.localRotation = Quaternion.Slerp(_armR.localRotation, Wanted != null ? Quaternion.Euler(-62f, 0f, -6f) : Quaternion.Euler(armSwing, 0f, -4f), dt * 6f);
            if (_torso != null) _torso.localPosition = new Vector3(0f, 0.95f + Mathf.Abs(Mathf.Sin(_stride)) * 0.012f * Mathf.Clamp01(speed), 0f);
        }

        private void FaceSlot(PlacementZone z, float dt)
        {
            if (z == null) return;
            FaceTowards(z.transform.position, dt);
            if (_head != null)
            {
                Vector3 to = z.transform.position - _head.position;
                var target = Quaternion.LookRotation(to.normalized, Vector3.up);
                _head.rotation = Quaternion.Slerp(_head.rotation, target * Quaternion.Euler(0f, 180f, 0f), dt * 4f);
            }
        }

        private void FaceTowards(Vector3 point, float dt)
        {
            Vector3 flat = point - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            // the figure's front is -Z (Blender -Y): look away so the face points at the target
            var target = Quaternion.LookRotation(-flat.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, dt * 5f);
        }
    }
}
