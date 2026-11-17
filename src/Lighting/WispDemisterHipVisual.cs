using System;
using UnityEngine;

namespace WispTorchHeeler.Lighting
{
    // owns the copy applicator so runtime materials get released however the copy dies, wearer death included
    // the anchor is where the hidden ball gets pinned
    public class WispDemisterHipVisual : MonoBehaviour
    {
        [SerializeField] private Transform _anchor = null!;
        [SerializeField] private GameObject[] _lightParts = Array.Empty<GameObject>();

        public WispVisualApplicator Applicator { get; } = new();

        public Transform Anchor => _anchor;

        public void SetAnchor(Transform anchor) => _anchor = anchor;

        public void SetLightParts(GameObject[] lightParts) => _lightParts = lightParts;

        // LightLod turns its light back on by itself, so whole objects get switched. the anchor keeps moving while inactive
        public void SetLit(bool isLit)
        {
            foreach (GameObject lightPart in _lightParts)
            {
                lightPart.SetActive(isLit);
            }
        }

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
