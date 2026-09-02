using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>Builds and slowly turns a museum-grade specimen for the title backdrop.</summary>
    public sealed class TitleHero : MonoBehaviour
    {
        public float TurnSpeed = 9f;
        private Transform _spec;

        private void Start()
        {
            var lib = SpecimenAssetLibrary.Load();
            ulong seed = FindHeroSeed();
            var g = SpecimenGenerator.Generate(seed);
            var go = new GameObject("HeroSpecimen");
            go.transform.SetParent(transform, false);
            var vis = go.AddComponent<SpecimenVisual>();
            vis.Build(g, new SpecimenCondition { Opened = true }, lib);
            vis.SetCrystalsVisible(true);
            var geo = vis.Geometry;
            vis.TopHalf.localRotation = Quaternion.Euler(0f, 0f, 180f);
            vis.TopHalf.localPosition = new Vector3(-geo.MeanEquatorRadius * 2.3f, geo.BottomY + geo.TopY, 0f);
            float scale = Mathf.Clamp(0.11f / Mathf.Max(0.02f, geo.MaxRadius), 0.8f, 2.5f);
            go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = new Vector3(0f, -geo.BottomY * scale - 0.015f, 0f);
            _spec = go.transform;
        }

        private static ulong FindHeroSeed()
        {
            for (ulong seed = 90001; seed < 90001 + 20000; seed++)
            {
                var g = SpecimenGenerator.Generate(seed);
                if (g.Tier >= QualityTier.MuseumGrade && (g.Mineral == MineralId.Amethyst || g.Mineral == MineralId.Celestite || g.Mineral == MineralId.Fluorite) && g.Cavity != CavityArchetype.Nodule) return seed;
            }
            return 90001;
        }

        private void Update()
        {
            if (_spec != null) transform.Rotate(0f, TurnSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
