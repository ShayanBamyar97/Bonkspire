using UnityEngine;
using TowerOfChaos.Core;
using TowerOfChaos.Player;
using TowerOfChaos.Rooms;

namespace TowerOfChaos.CameraSystem
{
    public enum CameraViewMode
    {
        StandardTopDownBrawlView = 0, // 54° 3D Top-Down view (like Brawl Stars) showing full 14m room width & 3D traps
        CombatAndPortalWideView = 1,  // 58° wider 3D Top-Down zoom-out during PvP (>=2 players) or near Portals
        XRayTwoRoomOverview = 2       // 62° wide 3-second overview when player uses Gem X-Ray Vision
    }

    /// <summary>
    /// Controls the 3D Top-Down Camera System (Brawl Stars / Dota 2 style)
    /// and Hallucination visual distortion (Mirror Flip, Abyssal Blindness 1.5m spotlight & Inverted Controls tilt).
    /// </summary>
    public class HybridCameraController : MonoBehaviour
    {
        [Header("Target & Room Context")]
        public HeroController LocalHero;
        public RoomController CurrentRoom;
        public Transform NearestPortalTransform;

        [Header("3D Top-Down Camera Rig (Brawl Stars Style)")]
        public Camera MainCamera;
        public Vector3 StandardTopDownOffset = new Vector3(0f, 12.5f, -7.5f);
        public Vector3 CombatWideTopDownOffset = new Vector3(0f, 15.0f, -8.8f);
        public Vector3 XRayOverviewOffset = new Vector3(0f, 22.0f, -4.0f);

        [Header("Current Camera State")]
        public CameraViewMode ActiveViewMode = CameraViewMode.StandardTopDownBrawlView;

        private Vector3 _currentOffsetVelocity;
        private float _currentPitchVelocity;
        private Vector3 _smoothedOffset;
        private float _smoothedPitch;
        private float _xRayOverrideRemainingSec;

        private void Start()
        {
            _smoothedOffset = StandardTopDownOffset;
            _smoothedPitch = GameConstants.StandardTopDownPitchDegrees;
        }

        public void TriggerXRayTwoRoomCamera(float durationSec = 3.0f)
        {
            _xRayOverrideRemainingSec = durationSec;
        }

        private void LateUpdate()
        {
            if (LocalHero == null || MainCamera == null) return;

            EvaluateTargetCameraMode();

            Vector3 targetOffset;
            float targetPitch;

            switch (ActiveViewMode)
            {
                case CameraViewMode.XRayTwoRoomOverview:
                    targetOffset = XRayOverviewOffset;
                    targetPitch = GameConstants.XRayOverviewPitchDegrees;
                    break;
                case CameraViewMode.CombatAndPortalWideView:
                    targetOffset = CombatWideTopDownOffset;
                    targetPitch = GameConstants.CombatTacticalPitchDegrees;
                    break;
                default:
                    targetOffset = StandardTopDownOffset;
                    targetPitch = GameConstants.StandardTopDownPitchDegrees;
                    break;
            }

            // Smooth 0.4s Cinemachine-style blend
            _smoothedOffset = Vector3.SmoothDamp(
                _smoothedOffset,
                targetOffset,
                ref _currentOffsetVelocity,
                GameConstants.CameraBlendDurationSec);

            _smoothedPitch = Mathf.SmoothDampAngle(
                _smoothedPitch,
                targetPitch,
                ref _currentPitchVelocity,
                GameConstants.CameraBlendDurationSec);

            MainCamera.transform.position = LocalHero.transform.position + _smoothedOffset;
            MainCamera.transform.rotation = Quaternion.Euler(
                _smoothedPitch,
                0f,
                LocalHero.AreControlsInverted ? 4.5f * Mathf.Sin(Time.time * 6f) : 0f);
        }

        private void EvaluateTargetCameraMode()
        {
            if (_xRayOverrideRemainingSec > 0f)
            {
                _xRayOverrideRemainingSec -= Time.deltaTime;
                ActiveViewMode = CameraViewMode.XRayTwoRoomOverview;
                return;
            }

            bool isPvPActiveInRoom = (CurrentRoom != null && CurrentRoom.ActivePlayerCount >= 2);

            bool isNearPortal = false;
            if (NearestPortalTransform != null && LocalHero != null)
            {
                float sqrDist = (NearestPortalTransform.position - LocalHero.transform.position).sqrMagnitude;
                isNearPortal = sqrDist <= (GameConstants.PortalCameraZoomTriggerRadius * GameConstants.PortalCameraZoomTriggerRadius);
            }

            ActiveViewMode = (isPvPActiveInRoom || isNearPortal)
                ? CameraViewMode.CombatAndPortalWideView
                : CameraViewMode.StandardTopDownBrawlView;
        }
    }
}
