using System;
using System.Linq;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // hangs a local orb or cage copy on the m_backTool joint and hides the networked ball, which keeps clearing mist from the same spot
    public class WispDemisterHipMount
    {
        // every renderer and light on the ball sits under these. the force field and sounds are on other children so mist clearing keeps going
        private static readonly string[] s_ballVisualPaths = { "demister_ball", "effects/flame", "effects/Point light" };

        private GameObject[] _ballVisuals = Array.Empty<GameObject>();
        private WispDemisterHipVisual? _hipVisual;
        private WornDemisterMount _activeMount = WornDemisterMount.Floating;
        private bool _isLit = true;

        public void Initialize(GameObject ballRoot)
        {
            _ballVisuals = s_ballVisualPaths.Select(path => ballRoot.transform.Find(path).gameObject).ToArray();
        }

        public WispDemisterHipVisual? Visual => _hipVisual;

        public bool IsLit => _isLit;

        // only a lit hip copy counts, the floating ball gives no heat
        public bool IsLitOnHip => _isLit && _hipVisual;

        // true when the light state changed and the visuals need a color refresh
        public bool SetLit(bool isLit)
        {
            if (isLit == _isLit)
            {
                return false;
            }

            _isLit = isLit;
            if (_activeMount == WornDemisterMount.Floating)
            {
                SetBallVisualsActive(isLit);
            }
            else if (_hipVisual)
            {
                _hipVisual.SetLit(isLit);
            }

            return true;
        }

        // true when the visuals changed, either a new copy went on or the ball came back because the template is missing
        public bool Attach(WornDemisterMount mount, Player wearer)
        {
            if (mount == _activeMount && _hipVisual)
            {
                return false;
            }

            WispDemisterHipVisual? template = WispDemisterHipTemplates.GetTemplate(mount);
            if (!template)
            {
                return Detach();
            }

            DestroyVisual();

            // same steps as VisEquipment.AttachItem. SetParent keeps world scale and the armature above the joint
            // is scaled 100, so parenting in local space would blow the visual up
            WispDemisterHipVisual hipVisual = UnityEngine.Object.Instantiate(template);
            hipVisual.gameObject.SetActive(true);
            VisEquipment.CleanupInstance(hipVisual.gameObject);
            Transform visualTransform = hipVisual.transform;
            visualTransform.SetParent(wearer.m_visEquipment.m_backTool);
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = Quaternion.identity;
            hipVisual.SetLit(_isLit);

            _hipVisual = hipVisual;
            _activeMount = mount;
            SetBallVisualsActive(false);

            Plugin.LogDebug($"[DemisterHipMount] Attached {mount} visual to '{wearer.GetPlayerName()}'.");
            return true;
        }

        // true when a hip style was active, the 1.5 s remote check calls this on floating balls all the time
        public bool Detach()
        {
            DestroyVisual();
            if (_activeMount == WornDemisterMount.Floating)
            {
                return false;
            }

            _activeMount = WornDemisterMount.Floating;
            SetBallVisualsActive(_isLit);
            return true;
        }

        // base game Fireplace hides and relights its flames the same way, play on awake restarts the particles
        private void SetBallVisualsActive(bool isActive)
        {
            foreach (GameObject ballVisual in _ballVisuals)
            {
                ballVisual.SetActive(isActive);
            }
        }

        // the copy lives under the wearer joint, not the ball, so it outlives the ball unless removed here
        public void DestroyVisual()
        {
            if (_hipVisual)
            {
                UnityEngine.Object.Destroy(_hipVisual.gameObject);
            }

            _hipVisual = null;
        }
    }
}
