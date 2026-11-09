using UnityEngine;

namespace WispTorchHeeler.Lighting
{
    // owns the copy applicator so runtime materials get released however the copy dies, wearer death included
    // the anchor is where the hidden ball gets pinned
    public class WispDemisterHipVisual : MonoBehaviour
    {
        [SerializeField] private Transform _anchor = null!;

        public WispVisualApplicator Applicator { get; } = new();

        public Transform Anchor => _anchor;

        public void SetAnchor(Transform anchor) => _anchor = anchor;

        private void Awake()
        {
            Applicator.Initialize(gameObject);
        }

        private void OnDestroy()
        {
            Applicator.CleanupRuntimeMaterials();
        }
    }
}
