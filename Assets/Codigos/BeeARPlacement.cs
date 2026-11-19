using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace AbejaRA
{
    [DefaultExecutionOrder(-100)]
    public class BeeARPlacement : MonoBehaviour
    {
        public BeeGardenGame game;
        public ARRaycastManager raycasts;
        public ARPlaneManager planes;
        public ARSession session;
        public ARCameraManager cameraManager;
        public ARCameraBackground background;
        public UnityEngine.InputSystem.XR.TrackedPoseDriver poseDriver;
        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        float nextHint;

        void Awake()
        {
            bool ar = !Application.isEditor && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
            session.enabled = ar; raycasts.enabled = ar; planes.enabled = ar;
            cameraManager.enabled = ar; background.enabled = ar; poseDriver.enabled = ar;
            if (ar) { game.view.transform.localPosition = Vector3.zero; game.view.transform.localRotation = Quaternion.identity; }
        }

        public void PlaceFromCenter()
        {
            if (!game.deviceAR)
            {
                game.Restart();
                return;
            }
            TryPlace(new Vector2(Screen.width * .5f, Screen.height * .5f));
        }

        bool TryPlace(Vector2 point)
        {
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                Hint("AR todavía está iniciando. Mueve el teléfono lentamente hacia el piso o una mesa.");
                return false;
            }
            if (!raycasts.Raycast(point, hits, TrackableType.PlaneWithinPolygon))
            {
                Hint("No se detectó superficie en ese punto. Apunta al piso o una mesa con buena luz y pulsa Colocar.");
                return false;
            }
            var pose = hits[0].pose;
            Vector3 facing = game.view.transform.forward; facing.y = 0;
            pose.rotation = Quaternion.LookRotation(facing.sqrMagnitude > .001f ? facing : Vector3.forward, Vector3.up);
            game.Restart();
            game.Place(pose);
            Debug.Log("[AbejaRA] Jardín colocado en superficie AR: " + pose.position);
            return true;
        }

        void Hint(string message)
        {
            if (game.status) game.status.text = message;
            nextHint = Time.unscaledTime + 4;
        }

        void Update()
        {
            if (!game.deviceAR || game.placed) return;
            if (Time.unscaledTime >= nextHint)
            {
                nextHint = Time.unscaledTime + .5f;
                if (game.status)
                    game.status.text = ARSession.state == ARSessionState.Unsupported
                        ? "Este dispositivo no admite realidad aumentada con ARCore."
                        : ARSession.state != ARSessionState.SessionTracking
                        ? "Iniciando AR. Mueve el teléfono lentamente hacia el piso o una mesa con buena luz."
                        : planes.trackables.count > 0
                        ? "Superficie detectada. Apunta hacia ella y pulsa Colocar, o tócala en la pantalla."
                        : "Busca una superficie: mueve el teléfono lentamente sobre el piso o una mesa con textura.";
            }
            var touch = Touchscreen.current;
            if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame) return;
            Vector2 point = touch.primaryTouch.position.ReadValue();
            if (!game.OverUI(point)) TryPlace(point);
        }
    }
}
