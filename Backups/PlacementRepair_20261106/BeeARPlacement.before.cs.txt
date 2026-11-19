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
        static readonly List<ARRaycastHit> hits=new List<ARRaycastHit>();
        void Awake()
        {
            bool ar=!Application.isEditor && (Application.platform==RuntimePlatform.Android || Application.platform==RuntimePlatform.IPhonePlayer);
            session.enabled=ar;raycasts.enabled=ar;planes.enabled=ar;cameraManager.enabled=ar;background.enabled=ar;poseDriver.enabled=ar;
            if(ar){game.view.transform.localPosition=Vector3.zero;game.view.transform.localRotation=Quaternion.identity;}
        }
        void Update()
        {
            if(!game.deviceAR || game.placed) return;
            if(ARSession.state==ARSessionState.Unsupported)
            { game.status.text="Este dispositivo no admite realidad aumentada con ARCore.";return; }
            var touch=Touchscreen.current;
            if(touch==null || !touch.primaryTouch.press.wasPressedThisFrame) return;
            Vector2 point=touch.primaryTouch.position.ReadValue();
            if(game.OverUI(point)) return;
            if(raycasts.Raycast(point,hits,TrackableType.PlaneWithinPolygon))
            {
                var p=hits[0].pose;
                Vector3 facing=game.view.transform.forward;facing.y=0;
                p.rotation=Quaternion.LookRotation(facing.sqrMagnitude>.001f?facing:Vector3.forward,Vector3.up);
                game.Place(p);
            }
            else game.status.text="No hay superficie aquí. Mueve el móvil y toca una mesa despejada.";
        }
    }
}
