using UnityEngine;

namespace GeodeEmpire.VFX
{
    /// <summary>Code-built, pooled particle systems: dust puffs, rock chips, reveal glints. No external assets.</summary>
    public sealed class EffectsFactory : MonoBehaviour
    {
        public static EffectsFactory Instance { get; private set; }

        private ParticleSystem _dust, _chips, _glints, _motes;
        private Material _dustMat, _chipMat, _glintMat;
        private static Texture2D _softCircle, _sparkle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; _softCircle = null; _sparkle = null; }

        private void Awake()
        {
            Instance = this;
            Build();
        }

        public static Texture2D SoftCircle()
        {
            if (_softCircle != null) return _softCircle;
            const int s = 64;
            _softCircle = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "SoftCircle", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                _softCircle.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _softCircle.Apply();
            return _softCircle;
        }

        public static Texture2D Sparkle()
        {
            if (_sparkle != null) return _sparkle;
            const int s = 64;
            _sparkle = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Sparkle", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float cross = Mathf.Max(Mathf.Exp(-Mathf.Abs(dx) * 40f) * Mathf.Exp(-Mathf.Abs(dy) * 6f), Mathf.Exp(-Mathf.Abs(dy) * 40f) * Mathf.Exp(-Mathf.Abs(dx) * 6f));
                float core = Mathf.Exp(-(dx * dx + dy * dy) * 60f);
                _sparkle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(cross + core)));
            }
            _sparkle.Apply();
            return _sparkle;
        }

        private static Material ParticleMaterial(Texture2D tex, Color tint, bool additive)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(sh) { name = "FX_" + tex.name };
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 1f : 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_Cull", 2f);
            if (additive)
            {
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        private void Build()
        {
            _dustMat = ParticleMaterial(SoftCircle(), new Color(0.62f, 0.58f, 0.52f, 0.55f), false);
            _chipMat = ParticleMaterial(SoftCircle(), new Color(0.3f, 0.27f, 0.24f, 1f), false);
            _glintMat = ParticleMaterial(Sparkle(), new Color(1f, 0.98f, 0.9f, 1f), true);

            _dust = MakeSystem("Dust", _dustMat, maxParticles: 200);
            var dm = _dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f); dm.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            dm.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f); dm.gravityModifier = -0.02f; dm.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var dcol = _dust.colorOverLifetime; dcol.enabled = true;
            dcol.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.5f));
            var dsz = _dust.sizeOverLifetime; dsz.enabled = true; dsz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

            _chips = MakeSystem("Chips", _chipMat, maxParticles: 150);
            var cm = _chips.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f); cm.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            cm.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.012f); cm.gravityModifier = 1.2f;
            var ccol = _chips.collision; ccol.enabled = true; ccol.type = ParticleSystemCollisionType.World; ccol.bounce = 0.25f; ccol.dampen = 0.4f; ccol.lifetimeLoss = 0.3f;
            var chipCol = _chips.colorOverLifetime; chipCol.enabled = true; chipCol.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.8f));

            _glints = MakeSystem("Glints", _glintMat, maxParticles: 120);
            var gm = _glints.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f); gm.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            gm.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.03f); gm.gravityModifier = 0f;
            var gcol = _glints.colorOverLifetime; gcol.enabled = true; gcol.color = new ParticleSystem.MinMaxGradient(FlashGradient());
            var gsz = _glints.sizeOverLifetime; gsz.enabled = true; gsz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));

            _motes = MakeSystem("Motes", _dustMat, maxParticles: 60);
            var mm = _motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); mm.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f); mm.gravityModifier = -0.002f; mm.loop = true; mm.startColor = new Color(1f, 0.95f, 0.85f, 0.18f);
            var mem = _motes.emission; mem.enabled = true; mem.rateOverTime = 6f;
            var msh = _motes.shape; msh.enabled = true; msh.shapeType = ParticleSystemShapeType.Box; msh.scale = new Vector3(5f, 2.4f, 4f);
            var mcol = _motes.colorOverLifetime; mcol.enabled = true; mcol.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.2f));
            _motes.transform.position = new Vector3(0f, 1.4f, 0f);
            _motes.Play();
        }

        private static Gradient FadeGradient(float peak)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.15f), new GradientAlphaKey(peak * 0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Gradient FlashGradient()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private ParticleSystem MakeSystem(string name, Material mat, int maxParticles)
        {
            var go = new GameObject("FX_" + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        private static void Burst(ParticleSystem ps, Vector3 position, Vector3 direction, int count, float speedMul, float spread)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var dir = (direction + Random.insideUnitSphere * spread).normalized;
                ep.position = position + Random.insideUnitSphere * 0.008f;
                ep.velocity = dir * Random.Range(0.5f, 1.4f) * speedMul;
                ps.Emit(ep, 1);
            }
        }

        public void Impact(Vector3 position, Vector3 normal, float force)
        {
            Burst(_dust, position, normal + Vector3.up * 0.3f, Mathf.RoundToInt(3 + force * 9f), 0.35f + force * 0.5f, 0.8f);
            Burst(_chips, position, normal + Vector3.up * 0.5f, Mathf.RoundToInt(1 + force * 6f), 0.6f + force * 1.4f, 0.7f);
        }

        public void Split(Vector3 position, float radius, Vector3 cameraDir)
        {
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                var p = position + new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a)) * radius;
                Burst(_dust, p, new Vector3(Mathf.Cos(a), 0.6f, Mathf.Sin(a)), 3, 0.7f, 0.6f);
                if (i % 2 == 0) Burst(_chips, p, new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)), 2, 1.3f, 0.5f);
            }
        }

        public void Glints(Vector3 position, float radius, int count, Color tint)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                ep.position = position + Random.insideUnitSphere * radius;
                ep.velocity = Vector3.up * Random.Range(0.02f, 0.08f);
                ep.startColor = Color.Lerp(Color.white, tint, 0.5f);
                _glints.Emit(ep, 1);
            }
        }
    }
}
