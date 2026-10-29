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
    public static class BeeUserAssetsSetup
    {
        const string Folder="Assets/AbejaRA/RecursosUsuario";
        const string Japanese="Assets/Waldemarst/JapaneseGardenPackage/Prefabs/";
        static readonly string[] TreeNames={"JapaneseMapleTree_D","BlackPineTree_E","SakuraTree_F"};
        static readonly int[] GroundNumbers={6,4,3,1,2};
        static BeeGardenGame game;
        static Vector2[] flowerPoints;
        static readonly Dictionary<string,Texture2D> textureCopies=new Dictionary<string,Texture2D>();
        static Transform Group(string name,Transform parent)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Texture2D ReadTexture(Texture source,int size)
        {
            var old=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            try
            {
                Graphics.Blit(source,rt);RenderTexture.active=rt;
                var t=new Texture2D(size,size,TextureFormat.RGBA32,false,false);
                t.ReadPixels(new Rect(0,0,size,size),0,0);t.Apply(false);return t;
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
        }
        static Texture2D SaveTexture(string name,Texture2D t,int size,bool alpha)
        {
            string path=Folder+"/"+name+".png";
            System.IO.File.WriteAllBytes(path,t.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.alphaIsTransparency=alpha;
            importer.mipMapsPreserveCoverage=alpha;importer.alphaTestReferenceValue=.3f;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;importer.maxTextureSize=size;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.compressionQuality=60;importer.isReadable=false;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D CopyAlbedo(Texture source,string name,int size,bool alpha)
        {
            if(!source)return null;
            string key=AssetDatabase.GetAssetPath(source)+"/"+size;
            if(textureCopies.TryGetValue(key,out var found))return found;
            var cpu=ReadTexture(source,size);var result=SaveTexture(name,cpu,size,alpha);Object.DestroyImmediate(cpu);textureCopies[key]=result;return result;
        }
        static Material Lit(string name,Texture texture,Color tint,bool cutout=false)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.08f);
            m.SetFloat("_Cull",cutout?0:2);m.SetFloat("_AlphaClip",cutout?1:0);m.SetFloat("_Cutoff",.3f);
            if(cutout){m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
            else{m.DisableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","Opaque");m.renderQueue=2000;}
            m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static float PathDistance(Vector2 p)
        {
            float result=999;
            for(int i=0;i<flowerPoints.Length;i++)
            {
                Vector2 a=flowerPoints[i],b=flowerPoints[(i+1)%flowerPoints.Length];float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);
                result=Mathf.Min(result,Vector2.Distance(p,Vector2.Lerp(a,b,t)));
            }
            return Mathf.Min(result,Vector2.Distance(p,new Vector2(0,Mathf.Clamp(p.y,-92,29))));
        }
        struct Sample
        {
            public Color32[] pixels;public int size;
            public Color Get(float x,float y)
            {
                float u=Mathf.Repeat(x,1)*size,v=Mathf.Repeat(y,1)*size;
                int ix=(int)u,iy=(int)v,ix1=(ix+1)%size,iy1=(iy+1)%size;
                return Color.Lerp(Color.Lerp(pixels[iy*size+ix],pixels[iy*size+ix1],u-ix),Color.Lerp(pixels[iy1*size+ix],pixels[iy1*size+ix1],u-ix),v-iy);
            }
        }
        static void Ground()
        {
            var samples=new Sample[5];var layers=new TerrainLayer[5];
            for(int i=0;i<5;i++)
            {
                int n=GroundNumbers[i];string path=$"Assets/ADG_Textures/ground_vol1/ground{n}/ground{n}_Diffuse.tga";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)throw new InvalidOperationException("Falta textura "+path);
                var cpu=ReadTexture(texture,512);samples[i]=new Sample{pixels=cpu.GetPixels32(),size=512};Object.DestroyImmediate(cpu);
                var layer=new TerrainLayer{diffuseTexture=texture,tileSize=new Vector2(10,10),metallic=0,smoothness=0,name="ADG_ground"+n};
                AssetDatabase.CreateAsset(layer,Folder+"/ADG_ground"+n+".terrainlayer");layers[i]=layer;
            }
            const int size=2048,alphaSize=256;var pixels=new Color32[size*size];var tex=new Texture2D(size,size,TextureFormat.RGBA32,false,false);
            var weights=new float[alphaSize,alphaSize,5];
            var scratchWeights=new float[5];
            float[] Weights(float x,float z)
            {
                float h=game.GroundHeight(x,z),s=game.groundData.GetSteepness((x+150)/300,(z+150)/300),noise=Mathf.PerlinNoise((x+170)*.02f,(z+270)*.02f);
                float rocky=Mathf.Clamp01((s-21)/27+(h-25)/50);
                float worn=Mathf.Clamp01((noise-.40f)*.9f)*(1-rocky);
                float trail=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,4.3f,PathDistance(new Vector2(x,z)))))*.94f;
                float sand=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.5f,3.7f,PathDistance(new Vector2(x,z)))))*.28f;
                scratchWeights[0]=(1-rocky-worn)*(1-trail);scratchWeights[1]=worn*(1-trail);scratchWeights[2]=rocky*(1-trail);scratchWeights[3]=trail*(1-sand);scratchWeights[4]=trail*sand;
                return scratchWeights;
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float px=x/(size-1f)*300-150,pz=y/(size-1f)*300-150;float[] w=Weights(px,pz);Color c=Color.black;
                for(int i=0;i<5;i++)if(w[i]>.001f)c+=samples[i].Get(px/10,pz/10)*w[i];
                c.a=1;pixels[y*size+x]=c;
            }
            tex.SetPixels32(pixels);tex.Apply(false);var baked=SaveTexture("Terreno_ADG_Pasto_Tierra_Roca",tex,size,false);Object.DestroyImmediate(tex);
            game.groundCollider.GetComponent<MeshRenderer>().sharedMaterial=Lit("Terreno_ADG",baked,Color.white);
            for(int y=0;y<alphaSize;y++)for(int x=0;x<alphaSize;x++)
            {var w=Weights(x/(alphaSize-1f)*300-150,y/(alphaSize-1f)*300-150);for(int i=0;i<5;i++)weights[y,x,i]=w[i];}
            game.groundData.terrainLayers=layers;game.groundData.alphamapResolution=alphaSize;game.groundData.SetAlphamaps(0,0,weights);EditorUtility.SetDirty(game.groundData);
        }
        static Mesh OrientedMesh(Mesh mesh,Matrix4x4 transform,string name,float maxSize)
        {
            var copy=Object.Instantiate(mesh);copy.name=name;var vertices=copy.vertices;var normals=copy.normals;
            for(int i=0;i<vertices.Length;i++)vertices[i]=transform.MultiplyPoint3x4(vertices[i]);
            for(int i=0;i<normals.Length;i++)normals[i]=transform.inverse.transpose.MultiplyVector(normals[i]).normalized;
            copy.vertices=vertices;copy.normals=normals;copy.RecalculateBounds();
            if(maxSize>0)
            {
                var b=copy.bounds;float factor=maxSize/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));var offset=new Vector3(b.center.x,b.min.y,b.center.z);
                for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-offset)*factor;copy.vertices=vertices;copy.RecalculateBounds();
            }
            if(normals.Length==0)copy.RecalculateNormals();return copy;
        }
        static void Flowers(Transform archive)
        {
            var old=game.flowers;var replacements=new Transform[old.Length];var meshes=new Mesh[4];var materials=new Material[4];
            for(int i=0;i<4;i++)
            {
                string directory="Assets/Flor"+(i+1);
                string path=AssetDatabase.FindAssets("t:Model",new[]{directory}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase));
                var filter=DecimatedFlower(i+1);
                var mesh=OrientedMesh(filter.sharedMesh,filter.transform.localToWorldMatrix,"Flor"+(i+1)+"_Movil",19);
                AssetDatabase.CreateAsset(mesh,Folder+"/Flor"+(i+1)+"_Movil.asset");meshes[i]=mesh;
                string texturePath=path.Replace(".fbx",".png");var source=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                materials[i]=Lit("Flor"+(i+1)+"_URP",CopyAlbedo(source,"Flor"+(i+1)+"_Albedo",1024,false),Color.white);
                Debug.Log($"[AbejaRA] Flor{i+1}: {mesh.triangles.Length/3:N0} triángulos, modelo móvil con UV originales.");
            }
            for(int i=0;i<old.Length;i++)
            {
                Vector3 location=game.garden.InverseTransformPoint(old[i].position);
                int index=i%4;var t=Group("Flor"+(index+1)+"_"+(i+1).ToString("00"),game.garden);t.localPosition=location;replacements[i]=t;
                var visual=Group("Modelo_Flor"+(index+1),t);visual.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[index];visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials[index];
                var nectar=old[i].Find("Nectar");
                if(nectar){nectar.SetParent(t,true);nectar.localPosition=new Vector3(0,meshes[index].bounds.max.y+3,0);nectar.localScale=Vector3.one*1.9f;}
                old[i].SetParent(archive,true);old[i].gameObject.SetActive(false);
            }
            game.flowers=replacements;
        }
        static MeshFilter DecimatedFlower(int number)
        {
            string path="Assets/AbejaRA/ModelosFlorMoviles/Flor"+number+".fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!model)throw new InvalidOperationException("Falta el modelo móvil de Blender: "+path);
            return model.GetComponentsInChildren<MeshFilter>(true).First();
        }
        public static void ApplyDecimatedFlowers()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Sal de Play Mode.");
            var g=Object.FindFirstObjectByType<BeeGardenGame>();
            for(int i=1;i<=4;i++)
            {
                var filter=DecimatedFlower(i);
                var replacement=OrientedMesh(filter.sharedMesh,filter.transform.localToWorldMatrix,"Flor"+i+"_Movil",19);
                var target=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Flor"+i+"_Movil.asset");
                if(!target)throw new InvalidOperationException("No existe la referencia de flor "+i);
                EditorUtility.CopySerialized(replacement,target);EditorUtility.SetDirty(target);Object.DestroyImmediate(replacement);
            }
            foreach(var flower in g.flowers)
            {
                var nectar=flower.Find("Nectar");var mesh=flower.Find("Modelo_"+flower.name.Split('_')[0]).GetComponent<MeshFilter>().sharedMesh;
                if(nectar)nectar.localPosition=new Vector3(0,mesh.bounds.max.y+3,0);
            }
            EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void Trees(Transform archive)
        {
            var old=game.garden.Find("Arboles_72");if(!old)throw new InvalidOperationException("No se encontró el bosque actual.");
            var positions=old.Cast<Transform>().Where((t,i)=>i%3!=2).Select(t=>new{p=t.localPosition,r=t.localRotation,s=t.localScale.x}).ToArray();
            var forest=Group("Arboles_JapaneseGarden_48",game.garden);
            // Render 48 trees in spatial batches. No per-tree wind controllers, LOD cross-fades,
            // property blocks, or extra shadow passes. Original prefabs stay untouched.
            var rootMeshes=new Mesh[3];var mats=new Material[3][];
            var trunk=new Dictionary<string,List<CombineInstance>>();var leaves=new Dictionary<string,List<CombineInstance>>();
            var markers=Group("Posiciones_de_los_Arboles",forest);
            for(int kind=0;kind<3;kind++)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Japanese+TreeNames[kind]+".prefab");
                var filter=source.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="LOD_1");
                rootMeshes[kind]=OrientedMesh(filter.sharedMesh,filter.transform.localToWorldMatrix,TreeNames[kind]+"_LOD1",0);
                AssetDatabase.CreateAsset(rootMeshes[kind],Folder+"/"+TreeNames[kind]+"_LOD1.asset");
                var sm=filter.GetComponent<MeshRenderer>().sharedMaterials;
                var bark=CopyAlbedo(sm[0].mainTexture,TreeNames[kind]+"_Corteza",512,false);
                var atlas=CopyAlbedo(sm[1].mainTexture,TreeNames[kind]+"_Hojas",512,true);
                mats[kind]=new[]{Lit(TreeNames[kind]+"_Tronco_URP",bark,Color.white),Lit(TreeNames[kind]+"_Hojas_URP",atlas,Color.white,true)};
            }
            for(int i=0;i<positions.Length;i++)
            {
                var spot=positions[i];int kind=i%3;int quadrant=(spot.p.x>0?1:0)+(spot.p.z>0?2:0);
                string key=kind+"_"+quadrant;
                if(!trunk.ContainsKey(key)){trunk[key]=new List<CombineInstance>();leaves[key]=new List<CombineInstance>();}
                var mesh=rootMeshes[kind];float scale=(21+5*spot.s)/mesh.bounds.size.y;
                Vector3 centerOffset=new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z);
                Matrix4x4 matrix=Matrix4x4.TRS(spot.p,spot.r,Vector3.one*scale)*Matrix4x4.Translate(-centerOffset);
                trunk[key].Add(new CombineInstance{mesh=mesh,subMeshIndex=0,transform=matrix});
                for(int s=1;s<mesh.subMeshCount;s++)leaves[key].Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=matrix});
                var marker=Group(TreeNames[kind]+"_"+(i+1).ToString("00"),markers);marker.localPosition=spot.p;marker.localRotation=spot.r;
            }
            foreach(var key in trunk.Keys)
            {
                int kind=int.Parse(key.Substring(0,1));
                var a=new Mesh {indexFormat=IndexFormat.UInt32};a.CombineMeshes(trunk[key].ToArray(),true,true);
                var b=new Mesh {indexFormat=IndexFormat.UInt32};b.CombineMeshes(leaves[key].ToArray(),true,true);
                var combined=new Mesh {name="Bosque_"+key,indexFormat=IndexFormat.UInt32};
                combined.CombineMeshes(new[]{new CombineInstance{mesh=a,transform=Matrix4x4.identity},new CombineInstance{mesh=b,transform=Matrix4x4.identity}},false,true);
                Object.DestroyImmediate(a);Object.DestroyImmediate(b);AssetDatabase.CreateAsset(combined,Folder+"/Bosque_"+key+".asset");
                var batch=Group(TreeNames[kind]+"_Sector"+key.Substring(2),forest);batch.gameObject.AddComponent<MeshFilter>().sharedMesh=combined;
                var r=batch.gameObject.AddComponent<MeshRenderer>();r.sharedMaterials=mats[kind];r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
                r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
            }
            old.SetParent(archive,true);old.gameObject.SetActive(false);
        }
        [MenuItem("Abeja RA/Usar texturas y modelos del usuario")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Sal de Play Mode.");
            game=Object.FindFirstObjectByType<BeeGardenGame>();
            if(!game||game.gameObject.scene.path!="Assets/Scenes/SampleScene.unity")throw new InvalidOperationException("Abre SampleScene.");
            if(AssetDatabase.IsValidFolder(Folder))throw new InvalidOperationException("Ya existen los recursos del usuario; no se reemplazarán sin revisión.");
            EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.CopyAsset(game.gameObject.scene.path,"Assets/Scenes/SampleScene_Antes_RecursosUsuario.unity");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();textureCopies.Clear();
            var archive=Group("Elementos_Procedurales_Anteriores",game.garden);archive.gameObject.SetActive(false);
            flowerPoints=game.flowers.Select(f=>game.garden.InverseTransformPoint(f.position)).Select(p=>new Vector2(p.x,p.z)).ToArray();
            Ground();Flowers(archive);Trees(archive);
            // Grass stays as one mesh; scenery rocks need no additional shadow draws on mobile.
            var rocks=game.garden.Find("Rocas");if(rocks)foreach(var r in rocks.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            var mobile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if(mobile){var so=new SerializedObject(mobile);so.FindProperty("m_SoftShadowsSupported").boolValue=true;so.FindProperty("m_ShadowDistance").floatValue=6;so.FindProperty("m_MainLightShadowmapResolution").intValue=1024;so.ApplyModifiedProperties();}
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("[AbejaRA] ADG + Flor1–Flor4 + Japanese Garden Pack aplicados. 48 árboles agrupados sin controladores de viento.");
        }
        public static void FinalizeScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Sal de Play Mode.");
            var g=Object.FindFirstObjectByType<BeeGardenGame>();
            if(!g || !AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_Antes_RecursosUsuario.unity"))throw new InvalidOperationException("Se requiere el respaldo antes de retirar objetos archivados.");
            var scene=g.gameObject.scene;
            foreach(var root in scene.GetRootGameObjects().Where(r=>r.name=="EscenaOriginal_Conservada"&&!r.activeSelf))Undo.DestroyObjectImmediate(root);
            foreach(string name in new[]{"Jardin_Anterior_Conservado","Elementos_Procedurales_Anteriores"})
            {
                var obsolete=g.garden.Find(name);
                if(obsolete&&!obsolete.gameObject.activeSelf)Undo.DestroyObjectImmediate(obsolete.gameObject);
            }
            foreach(string asset in new[]{"PC_RPAsset","Mobile_RPAsset"})
            {
                var rp=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/"+asset+".asset");
                if(!rp)continue;
                var so=new SerializedObject(rp);so.FindProperty("m_SoftShadowsSupported").boolValue=true;
                so.FindProperty("m_ShadowDistance").floatValue=6;so.FindProperty("m_ShadowCascadeCount").intValue=1;
                so.FindProperty("m_MainLightShadowmapResolution").intValue=1024;so.ApplyModifiedProperties();
            }
            foreach(string name in TreeNames)
            {
                var importer=AssetImporter.GetAtPath(Folder+"/"+name+"_Hojas.png") as TextureImporter;
                if(importer&&!importer.mipMapsPreserveCoverage)
                {importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.3f;importer.SaveAndReimport();}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
