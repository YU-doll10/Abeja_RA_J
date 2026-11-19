using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace AbejaRA
{
    public static class BeeARSetup
    {
        [MenuItem("Abeja RA/Configurar realidad aumentada Android")]
        public static void Setup()
        {
            var game=Object.FindFirstObjectByType<BeeGardenGame>();
            if(!game)throw new System.Exception("Primero prepara el jardín.");
            var originGO=GameObject.Find("XR Origin")??new GameObject("XR Origin");
            var origin=originGO.GetComponent<XROrigin>()??originGO.AddComponent<XROrigin>();
            var offset=originGO.transform.Find("Camera Offset");if(!offset){offset=new GameObject("Camera Offset").transform;offset.SetParent(originGO.transform,false);}
            game.view.transform.SetParent(offset,true);origin.Camera=game.view;origin.CameraFloorOffsetObject=offset.gameObject;
            origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;origin.CameraYOffset=0;
            var sessionGO=GameObject.Find("AR Session")??new GameObject("AR Session");
            var session=sessionGO.GetComponent<ARSession>()??sessionGO.AddComponent<ARSession>();
            var input=sessionGO.GetComponent<ARInputManager>()??sessionGO.AddComponent<ARInputManager>();
            var planes=originGO.GetComponent<ARPlaneManager>()??originGO.AddComponent<ARPlaneManager>();planes.requestedDetectionMode=PlaneDetectionMode.Horizontal;
            var rays=originGO.GetComponent<ARRaycastManager>()??originGO.AddComponent<ARRaycastManager>();
            var cm=game.view.GetComponent<ARCameraManager>()??game.view.gameObject.AddComponent<ARCameraManager>();
            var bg=game.view.GetComponent<ARCameraBackground>()??game.view.gameObject.AddComponent<ARCameraBackground>();
            var driver=game.view.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>()??game.view.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            driver.positionInput=new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Position",binding:"<XRHMD>/centerEyePosition"));
            driver.rotationInput=new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Rotation",binding:"<XRHMD>/centerEyeRotation"));
            driver.trackingStateInput=new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Tracking",binding:"<XRHMD>/trackingState"));
            var bridge=game.GetComponent<BeeARPlacement>()??game.gameObject.AddComponent<BeeARPlacement>();
            bridge.game=game;bridge.raycasts=rays;bridge.planes=planes;bridge.session=session;bridge.cameraManager=cm;bridge.background=bg;bridge.poseDriver=driver;
            // Keep the Editor preview independent of XR providers. Enabled on device in Awake.
            session.enabled=false;planes.enabled=false;rays.enabled=false;cm.enabled=false;bg.enabled=false;driver.enabled=false;
            var settings=(XRGeneralSettingsPerBuildTarget)typeof(XRGeneralSettingsPerBuildTarget).GetMethod("GetOrCreate",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            if(!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var manager=settings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!XRPackageMetadataStore.AssignLoader(manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android)) throw new System.Exception("No fue posible asignar ARCore.");
            EditorUtility.SetDirty(manager);EditorUtility.SetDirty(settings);
            foreach(var guid in AssetDatabase.FindAssets("t:UniversalRendererData",new[]{"Assets/Settings"}))
            {
                var rd=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if(!rd.rendererFeatures.Exists(f=>f is ARBackgroundRendererFeature))
                {
                    var feature=ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();feature.name="AR Camera Background";
                    AssetDatabase.AddObjectToAsset(feature,rd);rd.rendererFeatures.Add(feature);EditorUtility.SetDirty(rd);
                }
            }
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.abejara.jardin");
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.Android.forceInternetPermission=false;
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("[AbejaRA] AR Foundation + ARCore configurados para Android ARM64.");
        }
    }
}
