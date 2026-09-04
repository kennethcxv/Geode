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
        public float BudgetFloor;            // what they carry regardless of how modest the window looks
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
            new CustomerArchetype { Name = "Collector", Blurb = "knows exactly what this is", BudgetMin = 1.0f, BudgetMax = 2.2f, BudgetFloor = 70f, Likes = new[] { MineralId.Amethyst, MineralId.Fluorite, MineralId.Celestite, MineralId.Wulfenite, MineralId.Tourmaline, MineralId.Garnet }, ColourWeight = 0.5f, SizeWeight = 0.3f, ConditionWeight = 1.0f, Patience = 75f, Threshold = 0.5f,
                Jacket = new Color(0.22f, 0.24f, 0.3f), Trousers = new Color(0.2f, 0.18f, 0.17f), Skin = new Color(0.86f, 0.7f, 0.58f), Hair = new Color(0.32f, 0.22f, 0.16f), Height = 1.02f },
            new CustomerArchetype { Name = "Tourist", Blurb = "wants something that sparkles", BudgetMin = 0.35f, BudgetMax = 1.05f, BudgetFloor = 18f, Likes = new[] { MineralId.Agate, MineralId.ClearQuartz, MineralId.Citrine, MineralId.Malachite }, ColourWeight = 0.8f, SizeWeight = 0.1f, ConditionWeight = 0.3f, Patience = 40f, Threshold = 0.36f,
                Jacket = new Color(0.78f, 0.36f, 0.26f), Trousers = new Color(0.62f, 0.6f, 0.55f), Skin = new Color(0.7f, 0.52f, 0.4f), Hair = new Color(0.12f, 0.1f, 0.09f), Height = 0.96f },
            new CustomerArchetype { Name = "Decorator", Blurb = "buying for a shelf, not a cabinet", BudgetMin = 0.6f, BudgetMax = 1.35f, BudgetFloor = 35f, Likes = new[] { MineralId.Amethyst, MineralId.Citrine, MineralId.Celestite, MineralId.Agate, MineralId.Malachite, MineralId.Selenite }, ColourWeight = 0.9f, SizeWeight = 0.6f, ConditionWeight = 0.5f, Patience = 55f, Threshold = 0.42f,
                Jacket = new Color(0.86f, 0.82f, 0.74f), Trousers = new Color(0.15f, 0.15f, 0.16f), Skin = new Color(0.94f, 0.8f, 0.7f), Hair = new Color(0.82f, 0.68f, 0.42f), Height = 1.0f },
            new CustomerArchetype { Name = "Student", Blurb = "counting coins", BudgetMin = 0.25f, BudgetMax = 0.8f, BudgetFloor = 10f, Likes = new[] { MineralId.Pyrite, MineralId.Calcite, MineralId.Fluorite, MineralId.Aragonite, MineralId.Hematite, MineralId.Garnet }, ColourWeight = 0.3f, SizeWeight = 0.2f, ConditionWeight = 0.2f, Patience = 60f, Threshold = 0.34f,
                Jacket = new Color(0.28f, 0.42f, 0.32f), Trousers = new Color(0.25f, 0.27f, 0.36f), Skin = new Color(0.58f, 0.42f, 0.32f), Hair = new Color(0.08f, 0.07f, 0.07f), Height = 0.93f },
            new CustomerArchetype { Name = "Jeweller", Blurb = "only clean crystal will do", BudgetMin = 0.8f, BudgetMax = 1.8f, BudgetFloor = 45f, Likes = new[] { MineralId.ClearQuartz, MineralId.SmokyQuartz, MineralId.Citrine, MineralId.Amethyst, MineralId.Tourmaline, MineralId.Garnet }, ColourWeight = 0.4f, SizeWeight = 0.0f, ConditionWeight = 1.2f, Patience = 50f, Threshold = 0.5f,
                Jacket = new Color(0.14f, 0.13f, 0.14f), Trousers = new Color(0.12f, 0.12f, 0.13f), Skin = new Color(0.9f, 0.76f, 0.66f), Hair = new Color(0.6f, 0.6f, 0.62f), Height = 1.04f },
            new CustomerArchetype { Name = "Rockhound", Blurb = "would rather it were bigger", BudgetMin = 0.5f, BudgetMax = 1.25f, BudgetFloor = 28f, Likes = new[] { MineralId.SmokyQuartz, MineralId.Pyrite, MineralId.Aragonite, MineralId.Celestite, MineralId.Hematite, MineralId.Selenite }, ColourWeight = 0.2f, SizeWeight = 0.7f, ConditionWeight = 0.6f, Patience = 90f, Threshold = 0.4f,
                Jacket = new Color(0.5f, 0.38f, 0.24f), Trousers = new Color(0.32f, 0.3f, 0.26f), Skin = new Color(0.8f, 0.62f, 0.5f), Hair = new Color(0.45f, 0.3f, 0.2f), Height = 0.99f },
        };
    }

    /// <summary>
    /// One shop visitor: walks in, looks at what is on sale, decides, queues, pays, leaves. NavMeshAgent for the walk
    /// (position only: the body turns itself, smoothly), browse spots are reserved so two people never stand in one
    /// place, everyone yields a little to whoever is ahead, and a customer who makes no progress repaths, sidesteps
    /// and, as a last resort, is moved on while nobody is looking. Never persisted; never touches the career state.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour
    {
        public enum Phase { Entering, Browsing, Deciding, ToQueue, Queued, AtCounter, Thanking, Leaving, Done }

        public CustomerArchetype Archetype { get; private set; }
        public Phase State { get; private set; } = Phase.Entering;
        public SpecimenEntity Wanted { get; private set; }
        public float Budget { get; private set; }
        public bool Bought { get; private set; }
        /// <summary>V6 checkout: how this customer pays (seeded; the harness can force one).</summary>
        public enum Payment { Cash, Card }
        public Payment Method { get; private set; }
        public static int ForcedMethod = -1;
        public Transform HandPoint => _handPoint;
        public bool Reaching { get; private set; }
        public bool Receiving { get; private set; }
        public void Reach(bool on) { Reaching = on; if (on) Receiving = false; }
        /// <summary>
        /// Palm-open to receive what is coming: nothing (change) or a bag in one hand, a box or a bare piece in both.
        /// The receiving pose is the carry pose, so the handoff lands in the grip that keeps it (§53).
        /// </summary>
        public void Receive(bool on, GameObject package = null, SpecimenEntity piece = null)
        {
            Receiving = on;
            if (on) { Reaching = false; _pendingPackage = package; _pendingPiece = piece; SetHold(package, piece); }
        }
        private bool _twoHands, _carryLow;
        private float _holdHalf = 0.1f;           // half-width of what the hands close on, along the figure's right axis
        private Bounds _carryFoot;                // closed footprint of a bare carried piece (specimen-local)
        private GameObject _pendingPackage;
        private SpecimenEntity _pendingPiece;

        private void SetHold(GameObject package, SpecimenEntity piece)
        {
            _twoHands = package != null ? package.name != "Bag" : piece != null;
            _carryLow = !_twoHands;
            if (package != null) _holdHalf = package.transform.localScale.x * (_carryLow ? 0.1f : 0.15f);
            else if (piece != null) { _carryFoot = piece.FootprintFor(DisplayPose.Closed); _holdHalf = _carryFoot.extents.x + 0.01f; }
            else _holdHalf = 0.06f;
        }

        /// <summary>
        /// Where the object in (or coming to) the hands sits in the world this frame: a bag hangs from the one hand with
        /// its wide face along the walk; a box or a bare piece sits on the body's midline between both hands, base at
        /// hand height, upright and facing the way the figure faces.
        /// </summary>
        public void HoldPose(out Vector3 pos, out Quaternion rot)
        {
            Vector3 facing = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);
            if (facing.sqrMagnitude < 1e-4f) facing = Vector3.forward;
            var pkg = _package != null ? _package : _pendingPackage;
            var piece = _carriedOut != null ? _carriedOut : _pendingPiece;
            rot = Quaternion.LookRotation(_carryLow && pkg != null ? -transform.right : facing.normalized, Vector3.up);
            var hand = _handPoint != null ? _handPoint : transform;
            Vector3 p = hand.position + Vector3.down * 0.02f;
            if (_twoHands) p -= Vector3.Project(p - transform.position, transform.right);   // between the hands: on the midline
            if (pkg == null && piece != null) p += Vector3.up * (-_carryFoot.min.y) - rot * new Vector3(_carryFoot.center.x, 0f, _carryFoot.center.z);
            pos = p;
        }
        public void SayLine(string text) => Say(text);
        private bool _awaitingHandover;
        private GameObject _package;
        public int Id { get; private set; }
        public int Priority => _agent != null ? _agent.avoidancePriority : 50;
        public float Speed => _agent != null ? _agent.velocity.magnitude : 0f;
        /// <summary>Has somewhere to be and is not deliberately standing still.</summary>
        public bool Walking => _hasTarget && _agent != null && !_agent.isStopped && !_agent.pathPending;
        public bool Arrived => HasArrived();
        public int Recoveries { get; private set; }
        public float StuckSeconds { get; private set; }

        private RetailShop _shop;
        private NavMeshAgent _agent;
        private Transform _legL, _legR, _armL, _armR, _head, _torso;
        private Transform _shinL, _shinR, _foreL, _foreR;   // V6 figure: knees and elbows (absent on the old mannequin)
        private Transform _handPoint;
        private float _stride;
        private float _timer;
        private float _queueTimer;
        private float _navTimer;             // seconds since the last destination was set
        private int _queueIndex = -1;
        private readonly List<PlacementZone> _plan = new List<PlacementZone>();
        private int _planIndex;
        private int _deferred;
        private int _waits;
        private PlacementZone _lookingAt;
        private float _bestInterest;
        private SpecimenEntity _best;
        private float _baseSpeed = 1.1f, _speedMul = 1f;
        private Vector3 _target;
        private bool _hasTarget, _fallback;
        private Vector3 _progressPos;
        private float _progressTimer;
        private int _stuckLevel;
        private float _turnRate;
        private float _fidget;
        private float _headYaw;
        private UI.WorldLabel _bubble;
        private float _bubbleTimer;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public void Init(RetailShop shop, int id)
        {
            _shop = shop;
            Id = id;
            var rng = new SeededRandom((ulong)id * 7919UL + (ulong)Time.frameCount);
            Archetype = CustomerArchetype.All[rng.Range(0, CustomerArchetype.All.Length)];
            // budgets follow the stock: a shop full of $300 amethysts draws people carrying $300, a $40 agate table draws $40
            float anchor = _shop.StockAnchorPrice();
            Budget = Mathf.Clamp(Mathf.Round(Mathf.Max(Archetype.BudgetFloor, anchor * rng.Range(Archetype.BudgetMin, Archetype.BudgetMax)) / 5f) * 5f, 10f, 9999f);
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = rng.Range(0.9f, 1.25f);
            Method = ForcedMethod >= 0 ? (Payment)ForcedMethod : (rng.Chance(0.55f) ? Payment.Card : Payment.Cash);
            _agent.speed = _baseSpeed;
            _agent.angularSpeed = 220f;
            _agent.acceleration = 3.2f;
            _agent.stoppingDistance = 0.2f;
            _agent.radius = 0.3f;
            _agent.height = 1.7f;
            _agent.autoBraking = true;
            _agent.autoRepath = true;
            _agent.updateRotation = false;   // the body turns itself: no instant pivots
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            _agent.avoidancePriority = 40 + id % 20;
            _fidget = rng.Range(0f, 6.28f);
            _legL = Find("LegL"); _legR = Find("LegR"); _armL = Find("ArmL"); _armR = Find("ArmR"); _head = Find("Head"); _torso = Find("Torso");
            _shinL = Find("ShinL"); _shinR = Find("ShinR"); _foreL = Find("ForearmL"); _foreR = Find("ForearmR");
            // one hair shape, sometimes a hat, sometimes a longer coat: the crowd is not one figure
            int hairPick = rng.Range(0, 3);        // short, long, none (under a hat)
            bool hat = rng.Chance(0.3f) || hairPick == 2;
            bool cap = hat && rng.Chance(0.55f);
            bool coat = rng.Chance(0.3f);
            void Show(string part, bool on) { var t = Find(part); if (t != null) t.gameObject.SetActive(on); }
            Show("HairShort", hairPick == 0 && !cap); Show("HairLong", hairPick == 1 && !cap);
            Show("Cap", cap); Show("Beanie", hat && !cap); Show("CoatTail", coat);
            transform.localScale = Vector3.one * Archetype.Height * rng.Range(0.97f, 1.03f);
            // colours per part and sub-mesh, with a little per-person variation. The figure's parts only carry the
            // slots they use (see gen_props.customer_parts): torso [jacket], hips [trousers], legs [trousers, shoes],
            // arms [jacket, skin], head [skin, hair]
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                string part = r.gameObject.name;
                for (int i = 0; i < mats.Length; i++)
                {
                    Color c;
                    if (part.StartsWith("Head") || part.StartsWith("Hair")) c = i == 0 && part.StartsWith("Head") ? Archetype.Skin : Archetype.Hair;
                    else if (part == "Cap" || part == "Beanie") c = Color.Lerp(Archetype.Trousers, Archetype.Jacket, 0.4f) * 0.9f;
                    else if (part == "CoatTail") c = Archetype.Jacket * 0.95f;
                    else if (part.StartsWith("Forearm")) c = i == 0 ? Archetype.Jacket : Archetype.Skin;
                    else if (part.StartsWith("Arm")) c = i == 0 ? Archetype.Jacket : Archetype.Skin;
                    else if (part.StartsWith("Shin")) c = i == 0 ? Archetype.Trousers : Archetype.Hair * 0.6f;
                    else if (part.StartsWith("Leg")) c = i == 0 ? Archetype.Trousers : Archetype.Hair * 0.6f;
                    else if (part.StartsWith("Hips")) c = i == 0 ? Archetype.Trousers : Archetype.Hair * 0.5f;
                    else c = i == 0 ? Archetype.Jacket : Archetype.Jacket * 0.55f;   // torso, buttons
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
                if (_foreR != null) { _handPoint.SetParent(_foreR, false); _handPoint.localPosition = new Vector3(0.02f, -0.34f, -0.05f); }
                else { _handPoint.SetParent(_armR, false); _handPoint.localPosition = new Vector3(0.04f, -0.62f, -0.1f); }
            }
            // plan: look at 2-4 stocked slots, the families they came in for first, then nearest
            var avail = _shop.Available(this);
            var slots = new List<PlacementZone>();
            var liked = new HashSet<PlacementZone>();
            foreach (var e in avail) if (e.Zone != null) { slots.Add(e.Zone); if (System.Array.IndexOf(Archetype.Likes, e.Geology.Mineral) >= 0) liked.Add(e.Zone); }
            slots.Sort((a, b) =>
            {
                bool la = liked.Contains(a), lb = liked.Contains(b);
                if (la != lb) return la ? -1 : 1;
                return (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude);
            });
            int look = Mathf.Min(slots.Count, rng.Range(2, 5));
            for (int i = 0; i < look; i++) _plan.Add(slots[i]);
            if (_plan.Count == 0 && _shop.SaleSlots.Count > 0)
            {
                // window shopping: any fixture that exists in this shop
                var open = new List<PlacementZone>();
                foreach (var s in _shop.SaleSlots) if (s.gameObject.activeInHierarchy) open.Add(s);
                if (open.Count > 0) _plan.Add(open[rng.Range(0, Mathf.Min(open.Count, 6))]);
            }
            State = Phase.Entering;
            _progressPos = transform.position;
            GoToBrowse();
        }

        private Transform Find(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>()) if (t.name == n) return t;
            return null;
        }

        // ---- navigation ------------------------------------------------------------------------
        private bool Go(Vector3 point)
        {
            if (_agent == null || !_agent.isOnNavMesh) return false;
            _agent.isStopped = false;
            _navTimer = 0f;
            _target = point;
            _hasTarget = true;
            _fallback = false;
            _stuckLevel = 0;
            _progressPos = transform.position;
            _progressTimer = 0f;
            if (!_agent.SetDestination(point)) { _shop.Metrics.PathFailures++; _hasTarget = false; return false; }
            return true;
        }

        private bool Go(Transform target) => target != null && Go(target.position);

        private void Stop()
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            _hasTarget = false;
        }

        /// <summary>
        /// Reached the destination, or as close as this shop lets them: someone (usually the player) standing on the
        /// browse point, or an unreachable point, must never park a customer forever.
        /// </summary>
        private bool HasArrived()
        {
            if (_agent == null || !_hasTarget) return true;
            if (_agent.pathPending) return false;
            float remaining = _agent.remainingDistance;
            float radius = _agent.stoppingDistance + (_fallback ? 0.3f : 0.1f);
            if (remaining <= radius) return true;
            if (_agent.pathStatus != NavMeshPathStatus.PathComplete && remaining <= 0.7f) return true;
            if (remaining <= 0.8f && SomeoneStandingNear(_target, 0.7f)) return true;   // close enough: the next spot is taken, look from here
            if (_navTimer > 5f && remaining <= 1.1f && _agent.velocity.sqrMagnitude < 0.0004f) return true;
            return _navTimer > 16f;
        }

        private bool SomeoneStandingNear(Vector3 point, float radius)
        {
            // The player counts, and until now did not: HasArrived's own comment says "someone (usually the player)
            // standing on the browse point must never park a customer forever", but only other customers were
            // checked. A player parked on a queue spot slowed the walker to a crawl (YieldToOthers) without ever
            // letting it arrive, which is what drove the shop's stuck recoveries and its two teleports.
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 dp = cam.transform.position - point; dp.y = 0f;
                if (dp.sqrMagnitude < radius * radius) return true;
            }
            foreach (var o in _shop.Customers)
            {
                if (o == null || o == this || o.Walking) continue;
                Vector3 d = o.transform.position - point; d.y = 0f;
                if (d.sqrMagnitude < radius * radius) return true;
            }
            return false;
        }

        /// <summary>No meaningful progress for a short while: repath, then sidestep to a nearby valid point, then move on unseen.</summary>
        private void TrackProgress(float dt)
        {
            if (!Walking) { _progressTimer = 0f; _progressPos = transform.position; return; }
            _progressTimer += dt;
            if (_progressTimer < 1.6f) return;
            float moved = Vector3.Distance(transform.position, _progressPos);
            float remaining = _agent.remainingDistance;
            bool invalid = _agent.pathStatus == NavMeshPathStatus.PathInvalid;
            _progressPos = transform.position;
            _progressTimer = 0f;
            bool stalled = moved < 0.08f && remaining > _agent.stoppingDistance + 0.25f;
            if (!stalled && !invalid) { _stuckLevel = 0; return; }
            StuckSeconds += 1.6f;
            Recover(invalid);
        }

        private void Recover(bool invalid)
        {
            Recoveries++;
            _shop.Metrics.StuckRecoveries++;
            if (invalid) _shop.Metrics.PathFailures++;
            _stuckLevel++;
            if (_stuckLevel == 1 && !invalid)
            {
                _agent.ResetPath();
                _agent.SetDestination(_target);
                return;
            }
            if (_stuckLevel == 3 && _shop.Metrics.JamReports.Count < 20)
            {
                var cam0 = Camera.main;
                float pd = cam0 != null ? Vector3.Distance(new Vector3(cam0.transform.position.x, 0f, cam0.transform.position.z), new Vector3(transform.position.x, 0f, transform.position.z)) : -1f;
                _shop.Metrics.JamReports.Add($"stuck3 {State} at ({transform.position.x:F2},{transform.position.z:F2}) -> ({_target.x:F2},{_target.z:F2}) v={_agent.velocity.magnitude:F2} spd={_agent.speed:F2} stopped={_agent.isStopped} next=({_agent.nextPosition.x:F2},{_agent.nextPosition.z:F2}) player={pd:F2} onMesh={_agent.isOnNavMesh}");
            }
            if (_stuckLevel <= 4)
            {
                for (int i = 0; i < 8; i++)
                {
                    var off = Random.insideUnitCircle * (0.45f + 0.15f * _stuckLevel);
                    if (NavMesh.SamplePosition(_target + new Vector3(off.x, 0f, off.y), out var hit, 0.5f, NavMesh.AllAreas))
                    {
                        _agent.ResetPath();
                        if (_agent.SetDestination(hit.position)) { _fallback = true; return; }
                    }
                }
                return;
            }
            // unrecoverable in place: put them where they were going, while the player is not looking (or after long enough anyway)
            if (!VisibleToPlayer() || _stuckLevel >= 8)
            {
                if (NavMesh.SamplePosition(_target, out var hit, 0.8f, NavMesh.AllAreas)) _agent.Warp(hit.position);
                _shop.Metrics.Repositions++;
                if (_shop.Metrics.JamReports.Count < 20)
                    _shop.Metrics.JamReports.Add($"{State} at ({transform.position.x:F2},{transform.position.z:F2}) -> ({_target.x:F2},{_target.z:F2}) remaining={_agent.remainingDistance:F2} path={_agent.pathStatus}");
                _stuckLevel = 0;
                _fallback = true;
            }
        }

        private bool VisibleToPlayer()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 d = transform.position + Vector3.up * 0.9f - cam.transform.position;
            if (d.sqrMagnitude > 12f * 12f) return false;
            return Vector3.Angle(cam.transform.forward, d) < cam.fieldOfView * 0.7f;
        }

        /// <summary>
        /// Pinned by the player: slide sideways rather than grind to a halt. The agent plans over a NavMesh the
        /// player is not carved into, so a walker whose path runs through where the player is now standing leans
        /// into them until it gives up and is teleported. The stress harness parks the player on the browse line
        /// for two minutes and that is exactly what happened: customers stuck against the south wall on their way
        /// out, path complete, 1.6 m from the door. This steers them round.
        /// </summary>
        private void Unpin(float dt)
        {
            if (!Walking || _agent == null || !_agent.isOnNavMesh || _agent.pathPending) return;
            if (_stuckLevel == 0 && _progressTimer < 0.8f) return;          // judged by ground covered, not by the
            if (_agent.velocity.sqrMagnitude > 0.16f && _stuckLevel == 0) return;   // velocity avoidance keeps up
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 d = transform.position - cam.transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist > 1.0f || dist < 0.001f) return;
            Vector3 away = d / dist;
            Vector3 want = _target - transform.position; want.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, away);
            if (Vector3.Dot(side, want) < 0f) side = -side;                 // go round on the side the goal is on
            _agent.Move((side * 1.0f + away * 0.35f).normalized * _agent.speed * 0.6f * dt);
        }

        /// <summary>Personal space: slow for whoever is ahead (the lower-priority walker gives way) and never push through the player.</summary>
        private void YieldToOthers(float dt)
        {
            float mul = 1f;
            if (Walking)
            {
                Vector3 fwd = _agent.velocity.sqrMagnitude > 0.01f ? _agent.velocity.normalized : -transform.forward;
                foreach (var o in _shop.Customers)
                {
                    if (o == null || o == this) continue;
                    Vector3 d = o.transform.position - transform.position; d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > 1.2f || dist < 0.001f) continue;
                    if (Vector3.Dot(d / dist, fwd) < 0.35f) continue;
                    bool iYield = Priority >= o.Priority || !o.Walking;
                    float slow = Mathf.Lerp(iYield ? 0.3f : 0.65f, 1f, Mathf.InverseLerp(0.5f, 1.2f, dist));
                    mul = Mathf.Min(mul, slow);
                }
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 d = cam.transform.position - transform.position; d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > 0.001f && dist < 1.0f && Vector3.Dot(d / dist, fwd) > 0.3f) mul = Mathf.Min(mul, Mathf.Lerp(0.15f, 1f, Mathf.InverseLerp(0.45f, 1.0f, dist)));
                }
            }
            _speedMul = Mathf.MoveTowards(_speedMul, mul, dt * 2.2f);
            if (_agent != null) _agent.speed = _baseSpeed * _speedMul;
        }

        /// <summary>The body follows the direction of travel with a turn-rate limit; standing still it faces what it is doing.</summary>
        private void TurnBody(float dt)
        {
            Vector3 v = _agent != null ? _agent.velocity : Vector3.zero; v.y = 0f;
            float before = transform.eulerAngles.y;
            if (v.sqrMagnitude > 0.03f)
            {
                var want = Quaternion.LookRotation(-v.normalized, Vector3.up);   // the figure's front (face, placket, toes) is -Z (Blender -Y)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 240f * dt);
            }
            float rate = dt > 0f ? Mathf.DeltaAngle(before, transform.eulerAngles.y) / dt : 0f;
            _turnRate = Mathf.Lerp(_turnRate, rate, dt * 6f);
        }

        // ---- browsing / deciding ---------------------------------------------------------------
        private void GoToBrowse()
        {
            _shop.ReleaseBrowse(this);
            int guard = 0;
            while (_planIndex < _plan.Count && guard++ < 12)
            {
                var slot = _plan[_planIndex];
                if (!_shop.ClaimBrowse(slot, this))
                {
                    // someone is standing there: come back to it, and if the whole plan is taken, wait a little way off
                    if (_deferred < _plan.Count) { _plan.RemoveAt(_planIndex); _plan.Add(slot); _deferred++; continue; }
                    if (_waits < 2) { _waits++; _deferred = 0; Loiter(slot); return; }
                    _planIndex++;
                    continue;
                }
                var bp = _shop.BrowsePointFor(slot);
                if (bp == null || !Go(bp.position)) { _shop.ReleaseBrowse(this); _planIndex++; continue; }
                _lookingAt = slot;
                _deferred = 0;
                State = Phase.Browsing;
                _timer = -1f;
                return;
            }
            Decide();
        }

        /// <summary>Wait a step back from a busy fixture, looking at it, then try the plan again.</summary>
        private void Loiter(PlacementZone slot)
        {
            var bp = _shop.BrowsePointFor(slot);
            Vector3 basePos = bp != null ? bp.position : transform.position;
            Vector3 away = basePos - slot.transform.position; away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, away) * (Random.value < 0.5f ? -1f : 1f);
            Vector3 spot = basePos + away * 0.8f + side * 0.55f;
            if (NavMesh.SamplePosition(spot, out var hit, 0.8f, NavMesh.AllAreas)) spot = hit.position;
            _lookingAt = slot;
            State = Phase.Browsing;
            _timer = -1f;
            if (!Go(spot)) { _planIndex++; GoToBrowse(); }
        }

        private float Interest(SpecimenRecord r)
        {
            var g = r.Geology;
            float price = r.AskingPrice > 0f ? r.AskingPrice : RetailShop.AskingPrice(r);
            if (price > Budget) return price > Budget * 1.15f ? 0f : 0.2f;   // a stretch, only if everything else is right
            float f = 0.3f;
            if (System.Array.IndexOf(Archetype.Likes, g.Mineral) >= 0) f += 0.3f;
            f += Archetype.ColourWeight * (g.Saturation - 0.35f) * 0.5f;
            f += Archetype.SizeWeight * Mathf.Clamp01((g.MassKg - 1.2f) / 3f) * 0.4f;
            f -= Archetype.ConditionWeight * r.DamageFraction * 0.8f;
            f += 0.08f + Mathf.Clamp01((float)g.Tier / 4f) * 0.22f;       // anything beyond a plain common piece reads as "nice"
            // what the workshop made of it: decorators and tourists love a polished face, collectors and rockhounds a natural split
            if (r.IsPiece)
            {
                bool decor = Archetype.Name == "Decorator" || Archetype.Name == "Tourist" || Archetype.Name == "Jeweller";
                if (r.Polish > 0.5f) f += decor ? 0.14f : 0.05f;
                if (r.Piece.IsSlab) f += decor ? 0.06f : -0.04f;
            }
            else if (Archetype.Name == "Collector" || Archetype.Name == "Rockhound") f += 0.06f;
            // a bargain relative to their budget is tempting
            f += Mathf.Clamp01(1f - price / Budget) * 0.2f;
            return Mathf.Clamp01(f);
        }

        private void Decide()
        {
            _shop.ReleaseBrowse(this);
            Stop();
            State = Phase.Deciding;
            _timer = Random.Range(0.5f, 1.1f);
        }

        private void Update()
        {
            if (_shop == null || _agent == null) return;
            float dt = Time.deltaTime;
            _navTimer += dt;
            TrackProgress(dt);
            YieldToOthers(dt);
            Unpin(dt);
            TurnBody(dt);
            Animate(dt);
            if (_bubble != null) { _bubbleTimer -= dt; if (_bubbleTimer <= 0f) { Destroy(_bubble.gameObject); _bubble = null; } else FaceBubble(); }
            switch (State)
            {
                case Phase.Entering:
                case Phase.Browsing:
                    if (!Arrived) break;
                    if (_timer < 0f)
                    {
                        _timer = Random.Range(2.6f, 5.5f);
                        Stop();
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
                            // something rare, or a favourite family at a good grade, stops them in their tracks
                            var g = occ.Geology;
                            bool rare = g.Tier >= QualityTier.Exceptional || (g.Tier >= QualityTier.Good && System.Array.IndexOf(Archetype.Likes, g.Mineral) >= 0);
                            if (rare && !_reacted)
                            {
                                _reacted = true;
                                _timer = Random.Range(2.5f, 4f);   // linger on it
                                Say(g.Tier >= QualityTier.Exceptional ? "Oh, look at that" : "Now that is nice");
                                break;
                            }
                        }
                        if (_shop.BrowseClaimedBy(_lookingAt) == this) _planIndex++;   // a loiter does not consume the plan entry
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
                        Stop();
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
                    if (!Walking) FaceTowards(_shop.CounterItemPoint != null ? _shop.CounterItemPoint.position : transform.position - transform.forward, dt);
                    _queueTimer += dt;
                    if (_queueTimer > Archetype.Patience && State != Phase.AtCounter) { PutBack(); Leave(false); }
                    break;
                }
                case Phase.AtCounter:
                    FaceTowards(_shop.CounterItemPoint != null ? _shop.CounterItemPoint.position - transform.right * 0.2f : transform.position - transform.forward, dt);
                    _queueTimer += dt;
                    if (!_awaitingHandover && _queueTimer > Archetype.Patience * 1.6f) { PutBack(); Leave(false); }
                    break;
                case Phase.Thanking:
                    _timer -= dt;
                    FaceTowards(Camera.main != null ? Camera.main.transform.position : transform.position - transform.forward, dt);
                    if (_timer <= 0f) Leave(true);
                    break;
                case Phase.Leaving:
                    if (Arrived || _timer < 0f) Finish();
                    _timer -= dt;
                    break;
            }
        }

        private void Carry(SpecimenEntity e, bool sold = false)
        {
            if (e.Zone != null) e.Zone.Take(e, true);
            e.SetPhysics(false);
            e.SetCollidersEnabled(false);
            e.Locked = true;
            e.transform.SetParent(_handPoint != null ? _handPoint : transform, true);
            e.transform.localPosition = Vector3.zero;
            e.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            if (!sold) e.Record.Location = SpecimenLocation.SaleSlot;   // still stock until the money changes hands
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
            var e = Wanted;
            Wanted = null;
            if (e != null)
            {
                e.transform.SetParent(null, true);
                e.Locked = false;
                e.SetCollidersEnabled(true);
                // back onto a free sale slot (its own if still empty)
                PlacementZone home = null;
                foreach (var s in _shop.SaleSlots) if (s.IsEmpty && !s.Locked && s.gameObject.activeInHierarchy) { home = s; break; }
                if (home != null) home.Place(e, true);
                else { e.SetPhysics(true); e.Record.Location = SpecimenLocation.World; e.Record.AskingPrice = 0f; }
            }
            _shop.LeaveQueue(this);
            _shop.RefreshLabels();
        }

        /// <summary>The money is taken but the piece is still being packed: wait for it, however long the player takes.</summary>
        public void AwaitHandover() { _awaitingHandover = true; }

        /// <summary>
        /// V6 §54: the purchased object itself (in its package, if any) is placed in this hand by the checkout and is
        /// carried out as the same object; nothing is respawned.
        /// </summary>
        public void TakeOwnership(SpecimenEntity piece, GameObject package)
        {
            Bought = true;
            Wanted = null;
            _awaitingHandover = false;
            Receiving = false; Reaching = false;
            _carriedOut = piece;
            _package = package;
            _pendingPackage = null; _pendingPiece = null;
            SetHold(package, piece);   // a bag hangs from one hand; a box or a bare piece takes both
            _ownsHold = true;
            if (piece != null)
            {
                piece.Locked = true;
                piece.SetPhysics(false);
                piece.SetCollidersEnabled(false);
            }
            var hand = _handPoint != null ? _handPoint : transform;
            if (package != null) package.transform.SetParent(hand, true);
            else if (piece != null) piece.transform.SetParent(hand, true);
            KeepUpright();
            State = Phase.Thanking;
            _timer = 1.1f;
            Say("Thanks!");
        }

        /// <summary>What the customer owns stays in the hold pose whatever the arms do: upright, base at hand height, on the midline for two hands.</summary>
        private void KeepUpright()
        {
            if (!_ownsHold) return;
            var t = _package != null ? _package.transform : _carriedOut != null ? _carriedOut.transform : null;
            if (t == null) return;
            HoldPose(out var p, out var r);
            t.SetPositionAndRotation(p, r);
        }
        private bool _ownsHold;

        /// <summary>Money taken: a beat of thanks at the counter, then out.</summary>
        public void Paid() => Paid(null);

        /// <summary>Money taken: the piece is picked up off the counter and carried out; a beat of thanks, then the door.</summary>
        public void Paid(SpecimenEntity bought)
        {
            Bought = true;
            Wanted = null;
            if (bought != null)
            {
                _carriedOut = bought;
                Carry(bought, true);
                WorkshopAudio.Play("rock_pickup", transform.position, 0.4f, 1.05f);
            }
            State = Phase.Thanking;
            _timer = 0.9f;
            Say("Thanks!");
        }

        private SpecimenEntity _carriedOut;
        private bool _reacted;

        private void Leave(bool happy)
        {
            if (!happy && !Bought) _shop.CustomerLeftEmptyHanded();
            _shop.ReleaseBrowse(this);
            State = Phase.Leaving;
            _timer = 40f;
            if (!Go(_shop.OutsidePoint)) Finish();
        }

        private void Finish()
        {
            State = Phase.Done;
            _shop.ReleaseBrowse(this);
            _shop.Remove(this);
            if (_carriedOut != null) { GameSession.Instance?.Despawn(_carriedOut); _carriedOut = null; }
            if (_package != null) { Destroy(_package); _package = null; }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // never leave a sold piece behind if the figure is removed some other way
            if (_carriedOut != null) { GameSession.Instance?.Despawn(_carriedOut); _carriedOut = null; }
        }

        // ---- presentation --------------------------------------------------------------------
        private void Say(string text)
        {
            if (_shop.LabelFont == null) return;
            if (_bubble == null)
            {
                _bubble = UI.WorldLabel.Create(transform, _shop.LabelFont, _shop.LabelMaterial, 0.07f, new Color(0.98f, 0.95f, 0.85f), "Bubble");
                _bubble.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            }
            _bubble.Text = text;
            _bubbleTimer = 1.4f;
            FaceBubble();
        }

        private void FaceBubble()
        {
            var cam = Camera.main;
            if (cam == null || _bubble == null) return;
            Vector3 d = _bubble.transform.position - cam.transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) _bubble.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        private void Animate(float dt)
        {
            float speed = _agent != null ? _agent.velocity.magnitude : 0f;
            float gait = Mathf.Clamp01(speed / 0.5f);
            _stride += dt * speed * 5.2f;
            float swing = Mathf.Sin(_stride) * gait * 28f;
            if (_legL != null) _legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            // knees: the trailing leg bends as it comes through, the planted leg stays straight
            float kneeL = Mathf.Max(0f, Mathf.Sin(_stride + 1.1f)) * gait * 42f;
            float kneeR = Mathf.Max(0f, Mathf.Sin(_stride + 1.1f + Mathf.PI)) * gait * 42f;
            if (_shinL != null) _shinL.localRotation = Quaternion.Euler(-kneeL, 0f, 0f);   // the foot swings back under the knee (front is -Z)
            if (_shinR != null) _shinR.localRotation = Quaternion.Euler(-kneeR, 0f, 0f);
            float t = Time.time + _fidget;
            bool standing = gait < 0.15f;
            bool browsing = State == Phase.Browsing && standing;
            bool waiting = (State == Phase.Queued || State == Phase.AtCounter) && standing;
            float armSwing = swing * 0.6f;
            // the right arm holds a piece only while one is actually in the hand: on the counter it hangs like the other
            bool carrying = (Wanted != null && Wanted.transform.IsChildOf(transform)) || _carriedOut != null;
            // left hand: swings, or comes up to the chin for a moment while weighing something up
            var leftIdle = Quaternion.Euler(-armSwing, 0f, 4f);
            if (browsing && Mathf.Sin(t * 0.6f) > 0.55f) leftIdle = Quaternion.Euler(88f, 0f, -30f);
            bool bothHands = _twoHands && (carrying || Receiving);
            // both hands: forearms level at the waist, yawed inward until the hands close on the sides of what they hold
            // (hands rest 0.22 out from the midline and reach ~0.38 forward in this pose)
            float holdYaw = bothHands ? Mathf.Asin(Mathf.Clamp((0.22f - (_holdHalf + 0.03f)) / 0.38f, 0f, 0.85f)) * Mathf.Rad2Deg : 0f;
            if (bothHands) leftIdle = Quaternion.Euler(24f, -holdYaw, 6f);
            var holdR = _carryLow ? Quaternion.Euler(18f, 0f, -6f) : Quaternion.Euler(24f, holdYaw, -6f);
            if (_armL != null) _armL.localRotation = Quaternion.Slerp(_armL.localRotation, leftIdle, dt * 5f);
            if (_armR != null) _armR.localRotation = Quaternion.Slerp(_armR.localRotation, carrying ? holdR : Quaternion.Euler(armSwing, 0f, -4f), dt * 6f);
            // elbows: a resting bend that opens with the swing, sharper when the hand is at the chin or carrying a piece
            bool chin = browsing && Mathf.Sin(t * 0.6f) > 0.55f;
            float elbowL = bothHands ? 80f : chin ? 70f : 14f + Mathf.Max(0f, -armSwing) * 0.8f;
            float elbowR = carrying ? (_carryLow ? 28f : 80f) : 14f + Mathf.Max(0f, armSwing) * 0.8f;
            // at the counter: the right arm reaches out to present cash or a card, or comes up palm-open to receive
            if (Reaching) { elbowR = 12f; if (_armR != null) _armR.localRotation = Quaternion.Slerp(_armR.localRotation, Quaternion.Euler(78f, 0f, -8f), dt * 6f); }
            else if (Receiving) { elbowR = _carryLow ? 28f : 80f; if (_armR != null) _armR.localRotation = Quaternion.Slerp(_armR.localRotation, holdR, dt * 6f); }
            if (_foreL != null) _foreL.localRotation = Quaternion.Slerp(_foreL.localRotation, Quaternion.Euler(elbowL, 0f, 0f), dt * 5f);
            if (_foreR != null) _foreR.localRotation = Quaternion.Slerp(_foreR.localRotation, Quaternion.Euler(elbowR, 0f, 0f), dt * 5f);
            KeepUpright();
            if (_torso != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(_stride)) * 0.012f * gait;
                float breathe = Mathf.Sin(t * 1.7f) * 0.003f;
                float shift = (1f - gait) * Mathf.Sin(t * 0.5f) * 0.014f;
                _torso.localPosition = new Vector3(shift, 0.95f + bob + breathe, 0f);
                float lean = browsing ? 7f : waiting ? 2f : gait * 3f;   // toward the goods, a little into the walk
                float roll = Mathf.Clamp(_turnRate / 220f, -1f, 1f) * 5f;
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, Quaternion.Euler(-lean, 0f, roll), dt * 4f);
            }
            if (_head != null && !browsing)
            {
                // waiting or walking: glance at the player when they are close, otherwise look where you are going
                var cam = Camera.main;
                Quaternion want = Quaternion.identity;
                if (cam != null)
                {
                    Vector3 toCam = cam.transform.position - _head.position;
                    float dist = toCam.magnitude;
                    if (dist < (waiting ? 3.2f : 1.8f) && dist > 0.05f)
                    {
                        Vector3 local = transform.InverseTransformDirection(toCam / dist);
                        float yaw = Mathf.Atan2(-local.x, -local.z) * Mathf.Rad2Deg;   // front is -Z
                        if (Mathf.Abs(yaw) < 75f) want = Quaternion.Euler(0f, Mathf.Clamp(yaw, -60f, 60f), 0f);
                    }
                }
                if (State == Phase.Thanking) want *= Quaternion.Euler(Mathf.Sin(Time.time * 9f) * 6f, 0f, 0f);   // a nod
                _head.localRotation = Quaternion.Slerp(_head.localRotation, want, dt * 3f);
            }
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
            // the figure's front (face, placket, toes) is -Z (Blender -Y): look away so the face points at the target
            var target = Quaternion.LookRotation(-flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 200f * dt);
        }
    }
}
