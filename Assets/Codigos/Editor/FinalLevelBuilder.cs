using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace AbejaRA
{
    public static class FinalLevelBuilder
    {
        const string Folder = "Assets/AbejaRA/NivelFinal";
        const string User = "Assets/AbejaRA/RecursosUsuario/";
        static Terrain terrain;
        static Transform root, nature, obstacles;
        static Material rockMat, woodMat, waterMat, goldMat, foamMat;
        public static readonly Vector3[] MainTrail = {
            new Vector3(40,11.225f,35),new Vector3(115,11.75f,50),new Vector3(215,12.275f,65),new Vector3(225,12.73f,78),
            new Vector3(225,13.92f,112),new Vector3(225,14.375f,125),new Vector3(175,15.075f,145),new Vector3(80,15.53f,158),
            new Vector3(75,16.055f,173),new Vector3(75,17.245f,207),new Vector3(75,17.63f,218),
            new Vector3(110,18.05f,230),new Vector3(110,30,251),new Vector3(140,43,251),new Vector3(140,56,270),
            new Vector3(170,56,270),new Vector3(170,43,248),new Vector3(205,32,248),new Vector3(205,21,230),
            new Vector3(232,18.4f,240),new Vector3(245,19.38f,268)
        };
        public static readonly Vector2[] Route = MainTrail.Select(p=>new Vector2(p.x,p.z)).ToArray();
        public static readonly Vector3[][] Branches = {
            new[]{new Vector3(70,11.43f,41),new Vector3(65,14,49),new Vector3(67,19,61)},
            new[]{new Vector3(165,12.01f,57.5f),new Vector3(172,17,44),new Vector3(180,23,37)},
            new[]{new Vector3(225,14.375f,125),new Vector3(242,19,132),new Vector3(250,26,150)},
            new[]{new Vector3(150,15.2f,148.4f),new Vector3(140,20,136),new Vector3(140,30,123)},
            new[]{new Vector3(80,15.53f,158),new Vector3(65,18,150),new Vector3(50,22,158)},
            new[]{new Vector3(110,30,251),new Vector3(93,34,251),new Vector3(93,40,264)},
            new[]{new Vector3(140,56,270),new Vector3(124,59,270),new Vector3(124,62,281)},
            new[]{new Vector3(170,56,270),new Vector3(185,59,270),new Vector3(185,63,283)}
        };
        public static readonly Vector2[] FlowerPoints = {
            new Vector2(50,37),new Vector2(67,61),new Vector2(115,50),new Vector2(180,37),new Vector2(205,64),new Vector2(225,122),
            new Vector2(250,150),new Vector2(170,146),new Vector2(140,123),new Vector2(90,157),new Vector2(50,158),new Vector2(75,217),
            new Vector2(110,244),new Vector2(93,264),new Vector2(137,251),new Vector2(124,281),new Vector2(167,270),new Vector2(185,283),
            new Vector2(205,235),new Vector2(245,268)
        };
        public static readonly int[] SideFlowerIndices = {1,3,6,8,10,13,15,17};
        static readonly Vector3[][] AllTrails = new[]{MainTrail}.Concat(Branches).ToArray();
        public static float PathDistance(Vector2 p)
        {
            return TrailSample(p,out _);
        }
        static float TrailSample(Vector2 p,out float height)
        {
            float d=1000;float y=10;
            foreach(var path in AllTrails)
            for(int i=0;i<path.Length-1;i++)
            {
                var a=new Vector2(path[i].x,path[i].z);var b=new Vector2(path[i+1].x,path[i+1].z);
                float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);float candidate=Vector2.Distance(p,Vector2.Lerp(a,b,t));
                if(candidate<d){d=candidate;y=Mathf.Lerp(path[i].y,path[i+1].y,t);}
            }
            height=y;return d;
        }
        static float Gaussian(float x,float z,float cx,float cz,float sx,float sz,float h)
        {return h*Mathf.Exp(-((x-cx)*(x-cx)/(sx*sx)+(z-cz)*(z-cz)/(sz*sz)));}
        static float Base(float z){return 10+z*.035f;}
        public static float Height(float x,float z)
        {
            float h=Base(z)+(Mathf.PerlinNoise(x*.025f,z*.025f)-.5f)*3;
            h+=Gaussian(x,z,16,32,23,28,52)+Gaussian(x,z,18,145,24,35,57)+Gaussian(x,z,22,244,25,29,64);
            h+=Gaussian(x,z,76,283,32,22,62)+Gaussian(x,z,165,285,30,22,57)+Gaussian(x,z,267,278,29,24,66);
            h+=Gaussian(x,z,282,200,23,31,67)+Gaussian(x,z,281,61,24,32,58);
            h+=Gaussian(x,z,211,17,31,24,49)+Gaussian(x,z,111,16,28,22,43);
            h+=(Mathf.PerlinNoise(x*.08f,z*.08f)-.5f)*Mathf.Clamp01((h-25)/35)*9;
            h+=Gaussian(x,z,85,105,35,24,49)+Gaussian(x,z,150,104,30,24,57);
            h+=Gaussian(x,z,235,188,38,26,64)+Gaussian(x,z,160,195,30,24,48);
            h+=Gaussian(x,z,35,260,28,28,36)+Gaussian(x,z,150,280,35,22,48);
            h+=Gaussian(x,z,280,100,25,40,35)+Gaussian(x,z,55,45,17,17,14);
            float d=TrailSample(new Vector2(x,z),out float trailHeight);
            h=Mathf.Lerp(trailHeight,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,12,d)));
            foreach(float river in new[]{95f,190f})
            {
                float dz=Mathf.Abs(z-FinalBeeTrial.StreamCenter(x,river>150));
                if(dz<17)
                {
                    float channel=Base(river)-9+Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,17,dz))*9;
                    h=Mathf.Lerp(channel,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,17,dz)));
                }
            }
            return h;
        }
        static Transform Group(string name,Transform parent)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Material Material(string name,Material source,Color fallback)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name;
            if(source)
            {
                Texture t=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture;
                if(t)m.SetTexture("_BaseMap",t);
            }
            m.color=fallback;m.SetFloat("_Smoothness",.15f);m.enableInstancing=true;
            AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
        }
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            g.isStatic=true;return g;
        }
        static Vector3 Point(Vector2 p,float offset=0){return new Vector3(p.x,terrain.SampleHeight(new Vector3(p.x,0,p.y))+offset,p.y);}
        static void Rock(Vector2 p,Vector3 size,string name)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/AbejaRA/Paisaje300/Roca_Facetada.asset");
            var g=new GameObject(name);g.transform.SetParent(obstacles,false);g.transform.position=Point(p,-.6f);
            var b=mesh.bounds;g.transform.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=rockMat;
            var col=g.AddComponent<BoxCollider>();col.center=b.center;col.size=b.size*.88f;g.isStatic=true;
        }
        static void Tree(Vector2 p,int type,float height)
        {
            string[] names={"JapaneseMapleTree_D","BlackPineTree_E","SakuraTree_F"};string n=names[type];
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(User+n+"_LOD1.asset");
            var g=Group("Arbol_"+n,nature);g.position=Point(p);
            float factor=height/mesh.bounds.size.y;
            var visual=Group("Modelo",g);visual.localScale=Vector3.one*factor;
            visual.localPosition=-new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z)*factor;
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=visual.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(User+n+"_Tronco_URP.mat"),AssetDatabase.LoadAssetAtPath<Material>(User+n+"_Hojas_URP.mat")};
            var col=g.gameObject.AddComponent<CapsuleCollider>();col.radius=.85f;col.height=height*.65f;col.center=Vector3.up*height*.325f;
            g.gameObject.isStatic=true;visual.gameObject.isStatic=true;
        }
        static Transform Bridge(float x,float z,float width,string name)
        {
            var group=Group(name,root);float y=Base(z)+.5f;
            var deck=Cube("Tablero_Transitable",group,new Vector3(x,y,z),new Vector3(width,.7f,36),woodMat);
            for(int i=0;i<24;i++)Cube("Tablon_"+i,group,new Vector3(x,y+.4f,z-17.25f+i*1.5f),new Vector3(width-.15f,.12f,1.32f),woodMat,false);
            for(int side=-1;side<=1;side+=2)
            {
                Cube("Baranda",group,new Vector3(x+side*(width*.5f-.15f),y+3.2f,z),new Vector3(.35f,.35f,36),woodMat);
                for(int j=0;j<7;j++)Cube("Poste",group,new Vector3(x+side*(width*.5f-.15f),y+1.7f,z-17+j*5.66f),new Vector3(.5f,3.4f,.5f),woodMat);
            }
            foreach(float end in new[]{-17f,17f})
                Cube("Estribo_Piedra",group,new Vector3(x,y-2,z+end),new Vector3(width+2,4,3),rockMat);
            return deck.transform;
        }
        static void Stream(float z)
        {
            var group=Group("Arroyo_Corriente_"+z,root); float y=Base(z)-5.8f;
            var water=Group("Agua",group); water.gameObject.AddComponent<MeshFilter>().sharedMesh=StreamMesh(z);
            water.gameObject.AddComponent<MeshRenderer>().sharedMaterial=waterMat;
            for(int i=0;i<24;i++)
            {
                float x=8+i*12;
                var flow=Cube("Espuma",group,new Vector3(x,y+.14f,FinalBeeTrial.StreamCenter(x,z>150)+Mathf.Sin(i*1.9f)*6),new Vector3(5,.025f,.2f),foamMat,false);
                flow.transform.rotation=Quaternion.Euler(0,-15,0);
            }
        }
        static Mesh StreamMesh(float z)
        {
            var vertices=new Vector3[130];var uv=new Vector2[130];var triangles=new int[64*6];float y=Base(z)-5.8f;
            for(int i=0;i<=64;i++)
            {
                float x=5+i*290f/64;float center=FinalBeeTrial.StreamCenter(x,z>150);
                vertices[i*2]=new Vector3(x,y,center-9.5f);vertices[i*2+1]=new Vector3(x,y,center+9.5f);
                uv[i*2]=new Vector2(i*.5f,0);uv[i*2+1]=new Vector2(i*.5f,1);
                if(i<64){int k=i*6,a=i*2;triangles[k]=a;triangles[k+1]=a+1;triangles[k+2]=a+2;triangles[k+3]=a+1;triangles[k+4]=a+3;triangles[k+5]=a+2;}
            }
            var mesh=new Mesh{name="Arroyo_Curvo_"+z,vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/Arroyo_"+z+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        public static void RefineLandscape()
        {
            terrain=GameObject.Find("Terreno_Final_300x300").GetComponent<Terrain>();var data=terrain.terrainData;
            var heights=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)heights[z,x]=Height(x*300f/256,z*300f/256)/110;data.SetHeights(0,0,heights);
            var layer=data.terrainLayers[2];layer.diffuseRemapMin=Vector4.zero;layer.diffuseRemapMax=new Vector4(.48f,.51f,.46f,1);layer.tileSize=new Vector2(23,23);EditorUtility.SetDirty(layer);
            var alpha=new float[256,256,5];
            for(int z=0;z<256;z++)for(int x=0;x<256;x++)
            {
                float px=x*300f/255,pz=z*300f/255;float slope=data.GetSteepness(x/255f,z/255f);float stone=Mathf.Clamp01((slope-23)/28)*.85f;
                float riverDist=Mathf.Min(Mathf.Abs(pz-FinalBeeTrial.StreamCenter(px,false)),Mathf.Abs(pz-FinalBeeTrial.StreamCenter(px,true)));
                float trail=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,8,PathDistance(new Vector2(px,pz)))))*(1-stone)*Mathf.Clamp01((riverDist-10)/7);
                float shore=Mathf.Clamp01(1-riverDist/17)*(1-stone-trail);float rest=1-stone-trail-shore;
                alpha[z,x,0]=rest*.76f;alpha[z,x,1]=rest*.24f;alpha[z,x,2]=stone;alpha[z,x,3]=trail;alpha[z,x,4]=shore;
            }
            data.SetAlphamaps(0,0,alpha);
            var level=GameObject.Find("Nivel_Final_Cumbres_300x300").transform;
            foreach(var t in level.GetComponentsInChildren<Transform>())
            {
                if(t.parent && (t.parent.name=="Bosque_JapaneseGarden" || t.parent.name=="Rocas_y_Bloqueos"))
                {var p=t.position;t.position=new Vector3(p.x,terrain.SampleHeight(p)+(t.name.StartsWith("Arbol")?0:t.name.Contains("Tronco")?2:-.6f),p.z);}
                if(t.parent && t.parent.name=="Flores_Objetivo_En_Orden") {var p=t.position;t.position=new Vector3(p.x,terrain.SampleHeight(p),p.z);}
            }
            foreach(float z in new[]{95f,190f})
            {
                var group=level.Find("Arroyo_Corriente_"+z);var water=group.Find("Agua");water.localPosition=Vector3.zero;water.localScale=Vector3.one;water.GetComponent<MeshFilter>().sharedMesh=StreamMesh(z);
                int i=0;foreach(Transform child in group)if(child.name=="Espuma"){float x=8+i*12;child.position=new Vector3(x,Base(z)-5.66f,FinalBeeTrial.StreamCenter(x,z>150)+Mathf.Sin(i*1.9f)*6);i++;}
            }
            EditorUtility.SetDirty(data);EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);EditorSceneManager.SaveScene(terrain.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void InstallTwentyFlowers()
        {
            var trial=UnityEngine.Object.FindFirstObjectByType<FinalBeeTrial>();
            if(!trial || (trial.flowers.Length==20 && trial.returnPoints!=null && trial.returnPoints.Length==20))throw new Exception("El nivel no está disponible o ya tiene las 20 flores con regresos.");
            RefineLandscape();
            root=GameObject.Find("Nivel_Final_Cumbres_300x300").transform;
            nature=root.Find("Bosque_JapaneseGarden");obstacles=root.Find("Rocas_y_Bloqueos");
            rockMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Roca_ADG_Final.mat");
            goldMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/AbejaRA/Materials/Miel.mat");
            // Keep scenery away from the newly carved hiking trail and its flower alcoves.
            foreach(var parent in new[]{nature,obstacles})foreach(Transform t in parent)
            {
                if(!(t.name.StartsWith("Arbol_")||t.name.StartsWith("Roca_Grande_")))continue;
                Vector2 p=new Vector2(t.position.x,t.position.z);
                if(PathDistance(p)<8)
                {
                    Vector2 best=p;float bestDistance=-1;
                    foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                    {
                        var candidate=p+direction*15;candidate.x=Mathf.Clamp(candidate.x,8,292);candidate.y=Mathf.Clamp(candidate.y,8,292);
                        float d=PathDistance(candidate);if(d>bestDistance){bestDistance=d;best=candidate;}
                    }
                    t.position=Point(best,t.name.StartsWith("Arbol_")?0:-.6f);
                }
            }
            var old=root.Find("Flores_Objetivo_En_Orden");old.name="Flores_Anteriores_Conservadas";old.gameObject.SetActive(false);
            var group=Group("Flores_Objetivo_En_Orden",root);var returnGroup=Group("Regresos_Al_Sendero",root);
            var flowers=new Transform[20];var returns=new Transform[20];
            var cyan=Material("Baliza_Regreso_Azul",null,new Color(.08f,.75f,1));
            for(int i=0;i<20;i++)
            {
                int type=i%4+1;var f=Group("Flor_"+(i+1).ToString("00")+"_Tipo"+type,group);f.position=Point(FlowerPoints[i]);flowers[i]=f;
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(User+"Flor"+type+"_Movil.asset");
                var model=Group("Flor_Usuario",f);float scale=4.3f/mesh.bounds.size.y;model.localScale=Vector3.one*scale;
                model.localPosition=-new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z)*scale;
                model.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;model.gameObject.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(User+"Flor"+type+"_URP.mat");
                var beacon=Group("Baliza",f);beacon.localPosition=Vector3.up*7;
                var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Nectar_Dorado";orb.transform.SetParent(beacon,false);orb.transform.localScale=Vector3.one*1.6f;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());orb.GetComponent<Renderer>().sharedMaterial=goldMat;
                Cube("Haz_Referencia",beacon,beacon.position-Vector3.up*2,new Vector3(.18f,4,.18f),goldMat,false);
            }
            for(int j=0;j<SideFlowerIndices.Length;j++)
            {
                int i=SideFlowerIndices[j];var p=Branches[j][0];
                var marker=Group("Regreso_Flor_"+(i+1).ToString("00"),returnGroup);marker.position=Point(new Vector2(p.x,p.z));returns[i]=marker;
                var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Baliza_Azul";orb.transform.SetParent(marker,false);orb.transform.localPosition=Vector3.up*5;orb.transform.localScale=Vector3.one*1.8f;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());orb.GetComponent<Renderer>().sharedMaterial=cyan;
                Cube("Poste_Regreso",marker,marker.position+Vector3.up*2,new Vector3(.2f,4,.2f),cyan,false);
                marker.gameObject.SetActive(false);
            }
            Rock(new Vector2(108,233),new Vector3(4,7,5),"Bloqueo_Subida_01");
            Rock(new Vector2(122,248),new Vector3(4,7,4),"Bloqueo_Subida_02");
            Rock(new Vector2(131,254),new Vector3(4,7,4),"Bloqueo_Subida_03");
            Rock(new Vector2(137,262),new Vector3(4,7,5),"Bloqueo_Subida_04");
            Rock(new Vector2(173,258),new Vector3(4,7,5),"Bloqueo_Bajada_01");
            Rock(new Vector2(202,240),new Vector3(4,7,4),"Bloqueo_Bajada_02");
            trial.flowers=flowers;trial.returnPoints=returns;trial.timeLimit=300;
            EditorUtility.SetDirty(trial);EditorSceneManager.MarkSceneDirty(trial.gameObject.scene);EditorSceneManager.SaveScene(trial.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("TWENTY_FLOWERS_INSTALLED flowers="+flowers.Length+" sideRoutes="+SideFlowerIndices.Length+" summitHeight=63");
        }
        [MenuItem("Abeja RA/Crear nivel final difícil 300x300")]
        public static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/Terreno YU + dificil.unity")throw new Exception("Abre Terreno YU + dificil antes de generar.");
            if(GameObject.Find("Nivel_Final_Cumbres_300x300"))throw new Exception("El nivel final ya existe. Se conserva el trabajo actual.");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            root=Group("Nivel_Final_Cumbres_300x300",null);nature=Group("Bosque_JapaneseGarden",root);obstacles=Group("Rocas_y_Bloqueos",root);
            foreach(var old in scene.GetRootGameObjects().Where(o=>o.GetComponent<Terrain>())){old.name="Terreno_Anterior_Conservado";old.SetActive(false);}
            var data=new TerrainData{heightmapResolution=257,alphamapResolution=256,baseMapResolution=512,size=new Vector3(300,110,300)};
            float[,] heights=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)heights[z,x]=Height(x*300f/256,z*300f/256)/110;
            data.SetHeights(0,0,heights);AssetDatabase.CreateAsset(data,Folder+"/Terreno_Final_300x300.asset");
            int[] layerIds={6,4,3,1,2};var layers=new TerrainLayer[5];
            for(int i=0;i<5;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>(User+"ADG_ground"+layerIds[i]+".terrainlayer");
                if(!source)throw new Exception("No se encontró capa ADG "+layerIds[i]);
                layers[i]=UnityEngine.Object.Instantiate(source);layers[i].tileSize=new Vector2(13,13);AssetDatabase.CreateAsset(layers[i],Folder+"/ADG_Final_"+layerIds[i]+".terrainlayer");
            }
            data.terrainLayers=layers;
            float[,,] alpha=new float[256,256,5];
            for(int z=0;z<256;z++)for(int x=0;x<256;x++)
            {
                float px=x*300f/255,pz=z*300f/255;float slope=data.GetSteepness(x/255f,z/255f);
                float stone=Mathf.Clamp01((slope-23)/28);float trail=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,8,PathDistance(new Vector2(px,pz)))))*(1-stone);
                float shore=Mathf.Max(1-Mathf.Abs(pz-95)/17,1-Mathf.Abs(pz-190)/17);shore=Mathf.Clamp01(shore)*(1-stone-trail);
                float rest=1-stone-trail-shore;
                alpha[z,x,0]=rest*.76f;alpha[z,x,1]=rest*.24f;alpha[z,x,2]=stone;alpha[z,x,3]=trail;alpha[z,x,4]=shore;
            }
            data.SetAlphamaps(0,0,alpha);
            var tg=Terrain.CreateTerrainGameObject(data);tg.name="Terreno_Final_300x300";tg.transform.SetParent(root,false);terrain=tg.GetComponent<Terrain>();
            terrain.heightmapPixelError=7;terrain.basemapDistance=350;terrain.drawInstanced=true;terrain.detailObjectDistance=0;
            var terrainMat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(terrainMat,Folder+"/Terreno_ADG_URP.mat");terrain.materialTemplate=terrainMat;
            rockMat=Material("Roca_ADG_Final",AssetDatabase.LoadAssetAtPath<Material>("Assets/AbejaRA/Paisaje300/Rocas.mat"),new Color(.56f,.57f,.5f));rockMat.SetTexture("_BaseMap",layers[2].diffuseTexture);
            woodMat=Material("Madera_Puentes",AssetDatabase.LoadAssetAtPath<Material>(User+"BlackPineTree_E_Tronco_URP.mat"),new Color(.58f,.37f,.19f));
            waterMat=Material("Agua_Arroyos",AssetDatabase.LoadAssetAtPath<Material>("Assets/Low Poly Nature Pack/Materials/BDStuidos_Water.mat"),new Color(.03f,.28f,.32f));waterMat.SetFloat("_Smoothness",.65f);
            goldMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/AbejaRA/Materials/Miel.mat");foamMat=Material("Espuma",null,new Color(.7f,.93f,.89f));
            Stream(95);Stream(190);var bridge1=Bridge(225,95,8,"Puente_01_Desfiladero");var bridge2=Bridge(75,190,7,"Puente_02_Estrecho");
            UnityEngine.Random.InitState(71126);
            for(int i=0;i<160;i++)
            {
                Vector2 p=new Vector2(UnityEngine.Random.Range(23,277),UnityEngine.Random.Range(24,279));
                if(PathDistance(p)<11 || Mathf.Abs(p.y-95)<18 || Mathf.Abs(p.y-190)<18 || data.GetSteepness(p.x/300,p.y/300)>35)continue;
                Tree(p,i%3,UnityEngine.Random.Range(10,19));
            }
            for(int i=0;i<50;i++)
            {
                Vector2 p=new Vector2(UnityEngine.Random.Range(14,286),UnityEngine.Random.Range(22,281));
                if(PathDistance(p)<11 || Mathf.Abs(p.y-95)<17 || Mathf.Abs(p.y-190)<17)continue;
                Rock(p,new Vector3(UnityEngine.Random.Range(7,15),UnityEngine.Random.Range(9,19),UnityEngine.Random.Range(6,13)),"Roca_Grande_"+i);
            }
            Vector2[] blockers={new Vector2(85,40),new Vector2(100,56),new Vector2(150,53),new Vector2(167,69),new Vector2(208,126),new Vector2(192,147),new Vector2(140,146),new Vector2(122,163),new Vector2(100,224),new Vector2(147,241),new Vector2(171,232),new Vector2(215,253),new Vector2(239,251)};
            foreach(var p in blockers)Rock(p,new Vector3(8,11,8),"Bloqueo_Paso_Estrecho");
            // Fallen trunks and standing stone pairs force lateral precision without closing the route.
            foreach(var p in new[]{new Vector2(60,40),new Vector2(185,67),new Vector2(108,157),new Vector2(220,245)})
            {
                var log=Cube("Tronco_Caido_Bloqueo",obstacles,Point(p,2),new Vector3(4,5,10),woodMat);log.transform.rotation=Quaternion.Euler(0,28,0);
            }
            var flowerGroup=Group("Flores_Objetivo_En_Orden",root);var flowers=new Transform[FlowerPoints.Length];
            for(int i=0;i<flowers.Length;i++)
            {
                int n=i%4+1;var f=Group("Flor_"+(i+1).ToString("00")+"_Tipo"+n,flowerGroup);f.position=Point(FlowerPoints[i]);flowers[i]=f;
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(User+"Flor"+n+"_Movil.asset");
                var model=Group("Flor_Usuario",f);float scale=4.3f/mesh.bounds.size.y;model.localScale=Vector3.one*scale;model.localPosition=-new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z)*scale;
                model.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;model.gameObject.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(User+"Flor"+n+"_URP.mat");
                var beacon=Group("Baliza",f);beacon.localPosition=Vector3.up*7;
                var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Nectar_Dorado";orb.transform.SetParent(beacon,false);orb.transform.localScale=Vector3.one*1.6f;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());orb.GetComponent<Renderer>().sharedMaterial=goldMat;
                Cube("Haz_Referencia",beacon,beacon.position-Vector3.up*2,new Vector3(.18f,4,.18f),goldMat,false);
            }
            foreach(int index in new[]{5,11})
            {
                var p=FlowerPoints[index];Cube("Refugio_Checkpoint_"+index,root,Point(p,-.05f),new Vector3(10,.18f,10),AssetDatabase.LoadAssetAtPath<Material>("Assets/AbejaRA/Materials/Piedra.mat"),false);
            }
            var player=Group("Abeja_Nivel_Final",root);player.position=Point(Route[0],2.1f);
            var cc=player.gameObject.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.7f;cc.stepOffset=.3f;cc.slopeLimit=42;
            var visualBee=Group("Visual_Abeja",player);
            var beeAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abeja/Meshy_AI_Crear_un_modelo_3D_de_0925233536_texture.fbx");
            var bee=(GameObject)PrefabUtility.InstantiatePrefab(beeAsset);bee.transform.SetParent(visualBee,false);
            foreach(var col in bee.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
            var renderers=bee.GetComponentsInChildren<Renderer>();var bb=renderers[0].bounds;foreach(var r in renderers)bb.Encapsulate(r.bounds);
            float beeScale=3.1f/Mathf.Max(bb.size.x,bb.size.y,bb.size.z);var center=visualBee.InverseTransformPoint(bb.center);bee.transform.localScale*=beeScale;bee.transform.localPosition=-center*beeScale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(bee.transform);
            foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>AssetDatabase.LoadAssetAtPath<Material>("Assets/AbejaRA/Materials/Abeja_Albedo.mat")).ToArray();
            var cam=Camera.main;if(!cam)cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.tag="MainCamera";
            cam.transform.position=player.position+new Vector3(0,34,-15);cam.transform.LookAt(player.position);cam.fieldOfView=55;cam.nearClipPlane=.2f;cam.farClipPlane=600;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.58f,.76f,.79f);
            var light=UnityEngine.Object.FindFirstObjectByType<Light>();if(light){light.transform.rotation=Quaternion.Euler(48,-32,0);light.intensity=1.3f;light.shadows=LightShadows.Soft;}
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.72f,.73f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=cam.backgroundColor;RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=420;
            var trial=player.gameObject.AddComponent<FinalBeeTrial>();trial.terrain=terrain;trial.visual=visualBee;trial.followCamera=cam;trial.flowers=flowers;trial.bridgeDecks=new[]{bridge1,bridge2};trial.spawn=player.position;
            if(!UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
            {
                var e=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));e.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            InstallTwentyFlowers();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=tg;SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(150,25,150),new Vector3(330,100,330)),false);
            Debug.Log("FINAL_LEVEL_BUILT terrain="+data.size+" flowers="+flowers.Length+" trees="+nature.childCount+" obstacleGroups="+obstacles.childCount);
        }
    }
}
