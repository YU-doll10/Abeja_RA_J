using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Linq;

namespace AbejaRA
{
    public static class BeeSceneSetup
    {
        static Transform root;
        static Material grass, soil, leaf, gold, cream, pink, lilac, stone;
        static Font font;
        static Material Mat(string name, Color color)
        {
            string path="Assets/AbejaRA/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color; m.SetFloat("_Smoothness",.22f); return m;
        }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        [MenuItem("Abeja RA/Preparar SampleScene")]
        public static void Build()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/SampleScene.unity") throw new System.Exception("Abre SampleScene antes de preparar el jardín.");
            if(GameObject.Find("AbejaRA_Game")) throw new System.Exception("La escena ya está preparada.");
            System.IO.Directory.CreateDirectory("Assets/AbejaRA/Materials");AssetDatabase.Refresh();
            if(!AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_Antes_RA.unity"))
                AssetDatabase.CopyAsset(scene.path,"Assets/Scenes/SampleScene_Antes_RA.unity");
            var originals=scene.GetRootGameObjects();
            var archive=new GameObject("EscenaOriginal_Conservada");
            foreach(var g in originals) {g.transform.SetParent(archive.transform,true);}
            archive.SetActive(false);
            grass=Mat("Cesped",new Color(.25f,.48f,.23f));soil=Mat("Tierra",new Color(.31f,.20f,.12f));
            leaf=Mat("Hojas",new Color(.13f,.33f,.20f)); gold=Mat("Miel",new Color(1,.65f,.10f));
            cream=Mat("Marfil",new Color(1,.92f,.65f));pink=Mat("Coral",new Color(.94f,.34f,.38f));
            lilac=Mat("Lavanda",new Color(.55f,.39f,.78f));stone=Mat("Piedra",new Color(.70f,.73f,.63f));
            var gameGO=new GameObject("AbejaRA_Game");var game=gameGO.AddComponent<BeeGardenGame>();
            root=new GameObject("Jardin_RA_120cm").transform;game.garden=root;
            Shape("Isla_Tierra",PrimitiveType.Cylinder,root,new Vector3(0,-.035f,0),new Vector3(1.2f,.035f,1.2f),soil);
            Shape("Cesped",PrimitiveType.Cylinder,root,new Vector3(0,-.009f,0),new Vector3(1.18f,.012f,1.18f),grass);
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI*2/20;var g=Shape("Borde_Piedra_"+i,PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*.57f,.007f,Mathf.Sin(a)*.57f),new Vector3(.075f,.038f,.055f),stone);
                g.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
            var flowers=new Transform[6];
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI*2/6+.3f;
                var f=new GameObject("Flor_"+(i+1)).transform;f.SetParent(root,false);f.localPosition=new Vector3(Mathf.Cos(a)*.36f,0,Mathf.Sin(a)*.36f);flowers[i]=f;
                Shape("Tallo",PrimitiveType.Cylinder,f,new Vector3(0,.036f,0),new Vector3(.008f,.036f,.008f),leaf);
                var l=Shape("Hoja",PrimitiveType.Sphere,f,new Vector3(.018f,.028f,0),new Vector3(.045f,.009f,.022f),leaf);l.transform.localRotation=Quaternion.Euler(0,0,25);
                for(int j=0;j<6;j++)
                {float p=j*Mathf.PI/3;Shape("Petalo",PrimitiveType.Sphere,f,new Vector3(Mathf.Cos(p)*.025f,.075f,Mathf.Sin(p)*.025f),new Vector3(.036f,.012f,.036f),i%3==0?pink:i%3==1?lilac:cream);}
                Shape("Centro",PrimitiveType.Sphere,f,new Vector3(0,.078f,0),new Vector3(.028f,.016f,.028f),gold);
                Shape("Nectar",PrimitiveType.Sphere,f,new Vector3(0,.12f,0),new Vector3(.018f,.018f,.018f),gold);
            }
            game.flowers=flowers;
            var hive=new GameObject("Colmena").transform;hive.SetParent(root,false);hive.localPosition=new Vector3(0,0,.12f);
            for(int i=0;i<4;i++) Shape("Anillo",PrimitiveType.Sphere,hive,new Vector3(0,.023f+i*.026f,0),new Vector3(.15f-i*.022f,.055f,.14f-i*.02f),gold);
            Shape("Entrada",PrimitiveType.Sphere,hive,new Vector3(0,.035f,-.063f),new Vector3(.035f,.042f,.01f),soil);
            for(int i=0;i<4;i++) Shape("Sendero",PrimitiveType.Sphere,root,new Vector3(0,.006f,-.08f-i*.09f),new Vector3(.05f,.01f,.04f),stone);
            game.bee=new GameObject("Abeja_Jugador").transform;game.bee.SetParent(root,false);game.bee.localPosition=new Vector3(0,.11f,-.12f);
            game.visual=new GameObject("Visual_Vuelo").transform;game.visual.SetParent(game.bee,false);
            string modelPath=AssetDatabase.FindAssets("t:Model",new[]{"Assets/Abeja"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith(".fbx"));
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));model.transform.SetParent(game.visual,false);
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float factor=.13f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            Vector3 localCenter=game.visual.InverseTransformPoint(bounds.center);
            model.transform.localScale=Vector3.one*factor;model.transform.localPosition=-localCenter*factor;
            var beeMat=Mat("Abeja_Albedo",Color.white);
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(modelPath.Replace(".fbx",".png"));beeMat.SetTexture("_BaseMap",tex);
            foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>beeMat).ToArray();
            var camGO=new GameObject("Main Camera");camGO.tag="MainCamera";var cam=camGO.AddComponent<Camera>();camGO.AddComponent<AudioListener>();game.view=cam;
            cam.transform.position=new Vector3(.85f,.95f,-1.1f);cam.transform.LookAt(new Vector3(0,.04f,0));cam.nearClipPlane=.01f;cam.farClipPlane=25;cam.fieldOfView=48;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.16f,.15f);
            var light=new GameObject("Luz_Suave").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.70f,.67f);RenderSettings.skybox=null;RenderSettings.fog=false;
            game.previewFloor=Shape("Suelo_Preview_Editor",PrimitiveType.Cylinder,null,new Vector3(0,-.09f,0),new Vector3(4,.015f,4),Mat("Fondo",new Color(.12f,.23f,.20f)));
            BuildUI(game);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)};
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=root.gameObject;SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,.03f,0),Vector3.one*1.5f),false);
            Debug.Log("[AbejaRA] SampleScene preparada; original conservada y copia de seguridad guardada.");
        }
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 size,Vector2 pos)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.sizeDelta=size;rt.anchoredPosition=pos;return rt;
        }
        static UnityEngine.UI.Text Label(string name,Transform parent,string value,int size,Color color,TextAnchor alignment,Vector2 min,Vector2 max,Vector2 dimensions,Vector2 pos)
        {
            var rt=Rect(name,parent,min,max,dimensions,pos);var t=rt.gameObject.AddComponent<UnityEngine.UI.Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=alignment;t.raycastTarget=false;return t;
        }
        static void BuildUI(BeeGardenGame game)
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var c=new GameObject("HUD_AbejaRA",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));var canvas=c.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.view;canvas.planeDistance=.2f;
            var s=c.GetComponent<UnityEngine.UI.CanvasScaler>();s.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1280,720);s.matchWidthOrHeight=.5f;
            var top=Rect("Cabecera",c.transform,new Vector2(0,1),Vector2.one,new Vector2(0,112),new Vector2(0,-56));
            top.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.035f,.10f,.075f,.94f);
            Label("Marca",top,"ABEJA  /  RA",28,new Color(1,.77f,.25f),TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(-60,-54),new Vector2(0,20));
            Label("Subtitulo",top,"UN PEQUEÑO JARDÍN, UNA GRAN MISIÓN",13,Color.white,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(-60,-54),new Vector2(0,-15));
            game.score=Label("Progreso",top,"00 / 06   FLORES",22,Color.white,TextAnchor.MiddleRight,Vector2.zero,Vector2.one,new Vector2(-60,-20),Vector2.zero);
            var bottom=Rect("Instruccion",c.transform,Vector2.zero,new Vector2(1,0),new Vector2(-40,72),new Vector2(0,52));bottom.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.035f,.10f,.075f,.94f);
            game.status=Label("Estado",bottom,"Toca el jardín para guiar a tu abeja hasta cada flor.",19,Color.white,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(-300,-16),new Vector2(-130,0));
            Button(bottom,"Reiniciar",new Vector2(-178,0),game,false);Button(bottom,"Colocar",new Vector2(-58,0),game,true);
            Label("AyudaEditor",c.transform,"EXPLORA · RECOLECTA · POLINIZA",14,new Color(1,.88f,.53f),TextAnchor.MiddleCenter,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(550,28),new Vector2(0,-136));
            var ev=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));ev.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        static void Button(Transform parent,string name,Vector2 pos,BeeGardenGame game,bool place)
        {
            var rt=Rect(name,parent,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(108,44),pos);
            rt.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(1,.72f,.20f);var b=rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            Label("Texto",rt,name,17,new Color(.08f,.14f,.09f),TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            if(place) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,game.Reposition);else UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,game.Restart);
        }
    }
}
