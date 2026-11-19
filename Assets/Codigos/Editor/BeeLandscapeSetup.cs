using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace AbejaRA
{
    // TerrainData remains editable at 300 x 300. A baked mesh displays it at AR scale,
    // because Unity Terrain itself does not support scaled/rotated AR roots.
    public static class BeeLandscapeSetup
    {
        const string Folder="Assets/AbejaRA/Paisaje300";
        const float Width=300, Height=100, ARScale=.006f;
        static TerrainData data;
        static Transform garden;
        static Material soil,bark,foliage,pine,rock,grass;
        static System.Random random;
        static readonly Vector2[] flowerPoints={new Vector2(-66,-40),new Vector2(-78,14),new Vector2(-40,54),new Vector2(35,54),new Vector2(78,12),new Vector2(65,-42)};
        static float Rand(float low,float high)=>low+(float)random.NextDouble()*(high-low);
        static float HeightAt(float x,float z)=>data.GetInterpolatedHeight((x+150)/300,(z+150)/300);
        static Material Material(string name,Color color)
        {
            var path=Folder+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.05f);m.enableInstancing=true;return m;
        }
        static Transform Group(string name,Transform parent)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
        }
        static Mesh SaveMesh(string name,Mesh m)
        {
            m.name=name;AssetDatabase.CreateAsset(m,Folder+"/"+name+".asset");return m;
        }
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,params Material[] materials)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterials=materials;return g;
        }
        static float Peak(float x,float z,float cx,float cz,float radius,float h)
        {
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(cx,cz))/radius;
            float shape=Mathf.Pow(Mathf.Clamp01(1-d),1.35f);
            return shape*h;
        }
        static float PathDistance(float x,float z)
        {
            float best=999;
            for(int i=0;i<flowerPoints.Length;i++)
            {
                var a=flowerPoints[i];var b=flowerPoints[(i+1)%flowerPoints.Length];var p=new Vector2(x,z);
                float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);
                best=Mathf.Min(best,Vector2.Distance(p,Vector2.Lerp(a,b,t)));
            }
            // Footpath from the foreground through the hive clearing.
            best=Mathf.Min(best,Vector2.Distance(new Vector2(x,z),new Vector2(0,Mathf.Clamp(z,-92,29))));
            return best;
        }
        static void CreateHeightData()
        {
            data=new TerrainData {name="Terreno_300x300",heightmapResolution=129,size=new Vector3(Width,Height,Width)};
            var h=new float[129,129];
            for(int z=0;z<129;z++)for(int x=0;x<129;x++)
            {
                float px=x/128f*300-150,pz=z/128f*300-150;
                float meadow=1.8f+Mathf.PerlinNoise((px+315)*.012f,(pz+447)*.012f)*3.8f;
                float peaks=Peak(px,pz,-112,106,64,62)+Peak(px,pz,-49,131,51,78)+Peak(px,pz,22,128,55,57)+Peak(px,pz,104,109,63,70);
                peaks+=Peak(px,pz,-137,13,47,30)+Peak(px,pz,140,4,46,26)+Peak(px,pz,116,-121,40,15);
                float roughness=Mathf.PerlinNoise((px+123)*.08f,(pz+217)*.08f);
                float value=meadow+peaks*(.80f+roughness*.35f);
                h[z,x]=value/Height;
            }
            data.SetHeights(0,0,h);AssetDatabase.CreateAsset(data,Folder+"/Terreno_300x300.asset");
        }
        static Texture2D GroundTexture()
        {
            const int n=512;string path=Folder+"/Mapa_Pasto_Roca_Senderos.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool existing=tex!=null;
            if(!tex)tex=new Texture2D(n,n,TextureFormat.RGB24,true,false){name="Mapa_Pasto_Roca_Senderos",wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=x/(n-1f),v=y/(n-1f),px=u*300-150,pz=v*300-150;
                float height=data.GetInterpolatedHeight(u,v),slope=data.GetSteepness(u,v);
                float noise=Mathf.PerlinNoise(u*26+17,v*26+31);
                Color c=Color.Lerp(new Color(.23f,.39f,.12f),new Color(.48f,.62f,.25f),noise);
                float stoneMask=Mathf.Clamp01((height-20)/32+Mathf.Max(0,(slope-23)/33));
                var stoneColor=Color.Lerp(new Color(.37f,.40f,.34f),new Color(.70f,.70f,.58f),height/70);
                c=Color.Lerp(c,stoneColor,stoneMask);
                c=Color.Lerp(c,new Color(.88f,.88f,.76f),Mathf.Clamp01((height-58)/12)*.7f);
                float d=PathDistance(px,pz),pathMask=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,4.5f,d));
                c=Color.Lerp(c,new Color(.66f,.56f,.33f)*(1+noise*.09f),pathMask*.92f);
                colors[y*n+x]=c;
            }
            tex.SetPixels(colors);tex.Apply(true);if(!existing)AssetDatabase.CreateAsset(tex,path);else EditorUtility.SetDirty(tex);return tex;
        }
        public static void RefreshGroundTexture()
        {
            data=Object.FindFirstObjectByType<BeeGardenGame>().groundData;
            GroundTexture();AssetDatabase.SaveAssets();
        }
        static Mesh BuildGroundMesh()
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            // Flat shaded triangles give the mountains a faceted low-poly silhouette.
            void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int s=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                uv.Add(new Vector2((a.x+150)/300,(a.z+150)/300));uv.Add(new Vector2((b.x+150)/300,(b.z+150)/300));uv.Add(new Vector2((c.x+150)/300,(c.z+150)/300));
                triangles.Add(s);triangles.Add(s+1);triangles.Add(s+2);
            }
            const int res=65;
            for(int z=0;z<res-1;z++)for(int x=0;x<res-1;x++)
            {
                float x0=x/(res-1f)*300-150,x1=(x+1)/(res-1f)*300-150,z0=z/(res-1f)*300-150,z1=(z+1)/(res-1f)*300-150;
                Vector3 a=new Vector3(x0,HeightAt(x0,z0),z0),b=new Vector3(x0,HeightAt(x0,z1),z1),c=new Vector3(x1,HeightAt(x1,z0),z0),d=new Vector3(x1,HeightAt(x1,z1),z1);
                Triangle(a,b,c);Triangle(c,b,d);
            }
            var mesh=new Mesh {name="Superficie_300x300"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh("Superficie_300x300",mesh);
        }
        static Mesh BuildSides()
        {
            var v=new List<Vector3>();var t=new List<int>();
            Vector2[] corners={new Vector2(-150,-150),new Vector2(150,-150),new Vector2(150,150),new Vector2(-150,150)};
            for(int side=0;side<4;side++)for(int i=0;i<64;i++)
            {
                var a=Vector2.Lerp(corners[side],corners[(side+1)%4],i/64f);var b=Vector2.Lerp(corners[side],corners[(side+1)%4],(i+1)/64f);
                int s=v.Count;v.Add(new Vector3(a.x,-8,a.y));v.Add(new Vector3(a.x,HeightAt(a.x,a.y),a.y));v.Add(new Vector3(b.x,HeightAt(b.x,b.y),b.y));v.Add(new Vector3(b.x,-8,b.y));
                t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});
            }
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return SaveMesh("Corte_Tierra",m);
        }
        sealed class MeshParts
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<int> first=new List<int>(),second=new List<int>();
            public void Tri(Vector3 a,Vector3 b,Vector3 c,int material)
            {
                int s=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);var list=material==0?first:second;list.Add(s);list.Add(s+1);list.Add(s+2);
            }
            public void Cone(Vector3 bottom,float radius,float height,int sides,int material,float topRadius=0)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    var p=bottom+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);var q=bottom+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                    var r=bottom+new Vector3(Mathf.Cos(a)*topRadius,height,Mathf.Sin(a)*topRadius);var s=bottom+new Vector3(Mathf.Cos(b)*topRadius,height,Mathf.Sin(b)*topRadius);
                    Tri(p,r,q,material);if(topRadius>0)Tri(q,r,s,material);
                }
            }
            public void Blob(Vector3 center,Vector3 size,int material)
            {
                Vector3 P(int ring,int slice)
                {float phi=ring*Mathf.PI/5,theta=slice*Mathf.PI*2/8;return center+Vector3.Scale(new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta)),size);}
                for(int r=0;r<5;r++)for(int s=0;s<8;s++){Tri(P(r,s),P(r,s+1),P(r+1,s),material);Tri(P(r,s+1),P(r+1,s+1),P(r+1,s),material);}
            }
            public Mesh Finish(string name)
            {
                var mesh=new Mesh();if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.subMeshCount=second.Count>0?2:1;mesh.SetTriangles(first,0);if(second.Count>0)mesh.SetTriangles(second,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(name,mesh);
            }
        }
        static void Populate()
        {
            var broad=new MeshParts();broad.Cone(Vector3.zero,1.25f,13,7,0,.75f);
            broad.Blob(new Vector3(0,18,0),new Vector3(8,9,7),1);broad.Blob(new Vector3(-5,14,1),new Vector3(6,6,6),1);broad.Blob(new Vector3(5,15,1),new Vector3(6,7,6),1);
            var broadMesh=broad.Finish("Arbol_Copa");
            var conifer=new MeshParts();conifer.Cone(Vector3.zero,1.1f,18,7,0,.45f);
            conifer.Cone(new Vector3(0,6,0),7,14,8,1);conifer.Cone(new Vector3(0,12,0),5.7f,12,8,1);conifer.Cone(new Vector3(0,18,0),4,10,8,1);
            var pineMesh=conifer.Finish("Arbol_Pino");
            var rocks=new MeshParts();rocks.Blob(new Vector3(0,.45f,0),new Vector3(1,.85f,.8f),0);var rockMesh=rocks.Finish("Roca_Facetada");
            var treeGroup=Group("Arboles_72",garden);int placed=0;
            for(int i=0;i<1500&&placed<72;i++)
            {
                float x=Rand(-139,139),z=Rand(-136,115),h=HeightAt(x,z);
                float e=x*x/(96*96)+(z-5)*(z-5)/(80*80);
                if(e<1 || h>40 || PathDistance(x,z)<13 || data.GetSteepness((x+150)/300,(z+150)/300)>35)continue;
                bool p=z>45||placed%3==0;
                var go=MeshObject((p?"Pino_":"Arbol_")+(placed+1).ToString("00"),treeGroup,p?pineMesh:broadMesh,bark,p?pine:foliage);
                go.transform.localPosition=new Vector3(x,h-.4f,z);go.transform.localRotation=Quaternion.Euler(0,Rand(0,360),0);go.transform.localScale=Vector3.one*Rand(.66f,1.26f);placed++;
            }
            treeGroup.name="Arboles_"+placed;
            var rockGroup=Group("Rocas",garden);int count=0;
            for(int i=0;i<300&&count<64;i++)
            {
                float x=Rand(-144,144),z=Rand(-142,145),h=HeightAt(x,z);
                if(PathDistance(x,z)<10 || flowerPoints.Any(p=>Vector2.Distance(p,new Vector2(x,z))<18) || new Vector2(x,z-28).magnitude<23)continue;
                var go=MeshObject("Roca_"+(count+1).ToString("00"),rockGroup,rockMesh,rock);go.transform.localPosition=new Vector3(x,h,z);
                go.transform.localScale=new Vector3(Rand(2,6),Rand(1.5f,4.8f),Rand(2,5));go.transform.localRotation=Quaternion.Euler(Rand(-12,12),Rand(0,360),Rand(-10,10));count++;
            }
            var blades=new MeshParts();int clumps=0;
            for(int i=0;i<6000&&clumps<2200;i++)
            {
                float x=Rand(-147,147),z=Rand(-147,147),h=HeightAt(x,z);
                if(h>34 || PathDistance(x,z)<5 || flowerPoints.Any(p=>Vector2.Distance(p,new Vector2(x,z))<9))continue;
                for(int j=0;j<3;j++)
                {
                    float a=Rand(0,Mathf.PI*2),w=Rand(.3f,.7f),tall=Rand(1.2f,3);
                    Vector3 c=new Vector3(x,h+.05f,z),side=new Vector3(Mathf.Cos(a)*w,0,Mathf.Sin(a)*w),tip=c+new Vector3(Mathf.Sin(a)*.6f,tall,Mathf.Cos(a)*.6f);
                    blades.Tri(c-side,tip,c+side,0);blades.Tri(c+side,tip,c-side,0);
                }
                clumps++;
            }
            MeshObject("Pasto_"+clumps+"_Matas",garden,blades.Finish("Pasto_Agrupado"),grass);
        }
        [MenuItem("Abeja RA/Ampliar paisaje a 300x300")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Sal de Play Mode antes de ampliar.");
            var game=Object.FindFirstObjectByType<BeeGardenGame>();
            if(!game || game.gameObject.scene.path!="Assets/Scenes/SampleScene.unity")throw new InvalidOperationException("Abre SampleScene de Abeja_RA_J.");
            if(game.groundData)throw new InvalidOperationException("El paisaje ya existe; edita sus elementos sin reconstruirlo.");
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.CopyAsset(game.gameObject.scene.path,"Assets/Scenes/SampleScene_Antes_300x300.unity");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            random=new System.Random(302910);garden=game.garden;
            var old=Group("Jardin_Anterior_Conservado",garden);
            var keep=new HashSet<Transform>(game.flowers){game.bee};var hive=garden.Find("Colmena");keep.Add(hive);
            foreach(var child in garden.Cast<Transform>().ToArray())if(child!=old&&!keep.Contains(child))child.SetParent(old,false);
            old.gameObject.SetActive(false);
            garden.name="Paisaje_RA_300x300";garden.localScale=Vector3.one*ARScale;
            CreateHeightData();
            soil=Material("Tierra_Corte",new Color(.30f,.23f,.13f));bark=Material("Troncos",new Color(.27f,.17f,.075f));
            foliage=Material("Copas_Verdes",new Color(.35f,.52f,.15f));pine=Material("Pinos",new Color(.11f,.32f,.20f));
            rock=Material("Rocas",new Color(.48f,.51f,.43f));grass=Material("Pasto",new Color(.40f,.57f,.16f));grass.SetFloat("_Cull",0);
            var surface=Material("Pradera_y_Montanas",Color.white);surface.SetTexture("_BaseMap",GroundTexture());
            var ground=MeshObject("Terreno_300x300",garden,BuildGroundMesh(),surface);var collider=ground.AddComponent<MeshCollider>();collider.sharedMesh=ground.GetComponent<MeshFilter>().sharedMesh;
            MeshObject("Bordes_Tierra",garden,BuildSides(),soil);
            Populate();
            for(int i=0;i<game.flowers.Length;i++)
            {
                var p=flowerPoints[i];game.flowers[i].localPosition=new Vector3(p.x,HeightAt(p.x,p.y)+.3f,p.y);game.flowers[i].localScale=Vector3.one*105;
            }
            hive.localPosition=new Vector3(0,HeightAt(0,28),28);hive.localScale=Vector3.one*150;
            game.visual.localScale=Vector3.one*125;
            game.groundData=data;game.groundCollider=collider;game.squareBounds=true;game.movementHalfExtents=new Vector2(147,147);
            game.speed=45;game.hoverHeight=16;game.keyboardLookahead=22;game.collectRadius=12;game.hoverBob=.7f;
            game.spawnPosition=new Vector3(0,HeightAt(0,-35)+16,-35);game.bee.localPosition=game.spawnPosition;
            var sourceGO=Terrain.CreateTerrainGameObject(data);sourceGO.name="Fuente_Editable_Terreno_300x300";sourceGO.transform.position=new Vector3(-150,0,-150);
            sourceGO.SetActive(false);
            game.view.transform.position=new Vector3(1.65f,1.75f,-2.25f);game.view.transform.LookAt(new Vector3(0,.12f,0));game.view.fieldOfView=45;game.view.farClipPlane=30;
            if(game.previewFloor){game.previewFloor.transform.localScale=new Vector3(6,.015f,6);game.previewFloor.transform.position=new Vector3(0,-.08f,0);}
            var subtitle=Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Subtitulo");
            if(subtitle)subtitle.text="BOSQUE DE POLINIZACIÓN · 300 × 300";
            foreach(var r in garden.GetComponentsInChildren<Renderer>()) {r.shadowCastingMode=r.name.StartsWith("Pasto_")?ShadowCastingMode.Off:ShadowCastingMode.On;}
            var sun=Object.FindFirstObjectByType<Light>();if(sun){sun.intensity=1.35f;sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.shadowBias=.025f;sun.shadowNormalBias=.04f;}
            // Avoid Intel Iris Xe / DX11 white-dot artifacts with the Mobile URP profile.
            var mobile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if(mobile){var so=new SerializedObject(mobile);so.FindProperty("m_SoftShadowsSupported").boolValue=true;so.ApplyModifiedProperties();}
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=ground;SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,.18f,0),new Vector3(2.2f,.8f,2.2f)),false);
            Debug.Log("[AbejaRA] Paisaje 300x300 creado: maqueta AR 1.8x1.8 m, 72 árboles, 64 rocas, 2200 matas de pasto y cordillera.");
        }
    }
}
