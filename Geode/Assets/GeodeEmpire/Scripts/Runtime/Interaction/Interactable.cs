using UnityEngine;
using GeodeEmpire.Player;

namespace GeodeEmpire.Interaction
{
    /// <summary>Anything the player can look at and use.</summary>
    public interface IInteractable
    {
        Transform transform { get; }
        bool CanInteract(PlayerInteractor player);
        string GetPrompt(PlayerInteractor player);
        void Interact(PlayerInteractor player);
        void SetHighlight(bool on);
        /// <summary>Optional secondary prompt shown under the main one (e.g. "Hold RMB to inspect").</summary>
        string GetHint(PlayerInteractor player) => null;
    }

    /// <summary>
    /// Base for static props/stations: highlight by brightening the base colour through a property block,
    /// which works with URP Lit without touching shared materials.
    /// </summary>
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer[] highlightRenderers;
        private MaterialPropertyBlock _mpb;
        private Color[] _baseColors;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private bool _highlighted;

        protected virtual void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0) highlightRenderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseColors = new Color[highlightRenderers.Length];
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                var r = highlightRenderers[i];
                _baseColors[i] = r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
            }
        }

        public void SetHighlightRenderers(Renderer[] renderers)
        {
            highlightRenderers = renderers;
            _baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                _baseColors[i] = r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
            }
        }

        public abstract bool CanInteract(PlayerInteractor player);
        public abstract string GetPrompt(PlayerInteractor player);
        public abstract void Interact(PlayerInteractor player);
        public virtual string GetHint(PlayerInteractor player) => null;

        public virtual void SetHighlight(bool on)
        {
            if (_highlighted == on || highlightRenderers == null) return;
            _highlighted = on;
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                var r = highlightRenderers[i];
                if (r == null) continue;
                if (on)
                {
                    r.GetPropertyBlock(_mpb);
                    var c = _baseColors[i];
                    _mpb.SetColor(BaseColorId, new Color(Mathf.Min(1f, c.r * 1.25f + 0.12f), Mathf.Min(1f, c.g * 1.22f + 0.1f), Mathf.Min(1f, c.b * 1.1f + 0.05f), c.a));
                    r.SetPropertyBlock(_mpb);
                }
                else
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, _baseColors[i]);
                    r.SetPropertyBlock(_mpb);
                }
            }
        }
    }
}
