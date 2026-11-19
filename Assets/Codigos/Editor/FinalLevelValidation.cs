using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AbejaRA
{
    public static class FinalLevelValidation
    {
        public static string ExpandedTestResult = "not started";
        static void Require(bool condition,string message)
        {if(!condition){ExpandedTestResult="FAILED: "+message;throw new Exception(message);}}
        static void Teleport(FinalBeeTrial trial,Vector3 p)
        {var cc=trial.GetComponent<CharacterController>();cc.enabled=false;trial.transform.position=p;cc.enabled=true;}
        public static void StartExpandedTest()
        {
            ExpandedTestResult="running";
            var trial=UnityEngine.Object.FindFirstObjectByType<FinalBeeTrial>();
            trial.StartCoroutine(ExpandedTest(trial));
        }
        static System.Collections.IEnumerator ExpandedTest(FinalBeeTrial trial)
        {
            trial.RestartTrial();
            Require(trial.flowers.Length==20,"Expected twenty flowers.");
            for(int i=0;i<20;i++)
            {
                Teleport(trial,trial.flowers[i].position+Vector3.up*2.1f);
                yield return null;yield return null;
                Require(trial.Collected==i+1,"Flower collection failed at "+(i+1));
                if(trial.returnPoints[i])
                {
                    Require(trial.ReturningToPath,"Side-route return was not required.");
                    if(i==1)
                    {
                        Teleport(trial,trial.flowers[i+1].position+Vector3.up*2.1f);
                        yield return null;yield return null;
                        Require(trial.Collected==i+1,"Player bypassed the mandatory return.");
                    }
                    Teleport(trial,trial.returnPoints[i].position+Vector3.up*2.1f);
                    yield return null;yield return null;
                    Require(!trial.ReturningToPath,"Return marker did not unlock the main trail.");
                }
            }
            Require(trial.Finished&&trial.Collected==20,"Final completion did not fire.");
            Debug.Log("EXPANDED_FLOWERS_PASS twenty collected; eight returns enforced; final completion reached.");
            for(int river=0;river<2;river++)
            {
                trial.RestartTrial();float x=river==0?215:85;
                float z=FinalBeeTrial.StreamCenter(x,river==1)-22;
                var start=new Vector3(x,0,z);start.y=trial.Ground(start)+2.1f;Teleport(trial,start);
                for(int step=0;step<180&&!trial.DangerousWater(trial.transform.position);step++)trial.Move(Vector2.up,1f/60);
                Require(trial.DangerousWater(trial.transform.position),"Bank blocked water entry for stream "+river);
                Debug.Log("WATER_ENTRY_PASS stream="+river+" position="+trial.transform.position);
                yield return null;yield return null;
                Require(trial.IsFalling,"Water entry did not trigger a fall.");
                float fallY=trial.transform.position.y;
                yield return new WaitForSeconds(.5f);
                Require(trial.IsFalling&&trial.transform.position.y<fallY-.1f,"Fall animation was not visible.");
                if(river==0)ScreenCapture.CaptureScreenshot("C:/Users/LENOVO/Desktop/Abejita_Ra/Abeja_RA_J/Assets/Temp/NivelFinal_CaidaAgua.png");
                yield return new WaitForSeconds(.9f);
                Require(!trial.IsFalling&&trial.Falls==1,"Water respawn did not finish exactly once.");
                Require(Vector3.Distance(trial.transform.position,trial.Checkpoint)<.3f,"Water did not return the bee to its checkpoint.");
                Require(trial.Remaining<trial.timeLimit-12,"Water penalty was not applied.");
                Debug.Log("WATER_FALL_AND_RESPAWN_PASS stream="+river+" falls="+trial.Falls+" time="+trial.Remaining);
            }
            trial.RestartTrial();ExpandedTestResult="PASSED";Debug.Log("EXPANDED_LEVEL_ALL_PASS");
        }
        public static void Validate()
        {
            var trial=UnityEngine.Object.FindFirstObjectByType<FinalBeeTrial>();
            if(!Application.isPlaying || !trial)throw new Exception("Run the level in Play Mode first.");
            if(trial.terrain.terrainData.size.x!=300 || trial.terrain.terrainData.size.z!=300)throw new Exception("Terrain dimensions incorrect.");
            var cc=trial.GetComponent<CharacterController>();cc.enabled=false;Physics.SyncTransforms();
            const int n=151;int count=n*n;var cache=new sbyte[count];var overlaps=new Collider[24];
            Func<int,bool> passable=index=>
            {
                if(cache[index]!=0)return cache[index]>0;
                float x=index%n*2,z=index/n*2;
                if(x<6||x>294||z<6||z>294){cache[index]=-1;return false;}
                var p=new Vector3(x,0,z);p.y=trial.Ground(p)+2.1f;
                bool bridge=(Mathf.Abs(x-225)<3.5f&&Mathf.Abs(z-95)<18)||(Mathf.Abs(x-75)<3&&Mathf.Abs(z-190)<18);
                if(trial.DangerousWater(p)||(!bridge&&trial.terrain.terrainData.GetSteepness(x/300,z/300)>42)){cache[index]=-1;return false;}
                int hits=Physics.OverlapCapsuleNonAlloc(p-Vector3.up*.2f,p+Vector3.up*.2f,.78f,overlaps,~0,QueryTriggerInteraction.Ignore);
                cache[index]=(sbyte)(hits==0?1:-1);return hits==0;
            };
            int start=Mathf.RoundToInt(trial.spawn.x/2)+Mathf.RoundToInt(trial.spawn.z/2)*n;float length=0;
            try
            {
                var goals=new List<Transform>();
                for(int i=0;i<trial.flowers.Length;i++){goals.Add(trial.flowers[i]);if(trial.returnPoints!=null && i<trial.returnPoints.Length && trial.returnPoints[i])goals.Add(trial.returnPoints[i]);}
                foreach(var flower in goals)
                {
                    var prev=new int[count];for(int j=0;j<count;j++)prev[j]=-1;
                    var queue=new int[count];int head=0,tail=0;queue[tail++]=start;prev[start]=start;int goal=-1;
                    while(head<tail)
                    {
                        int current=queue[head++];float x=current%n*2,z=current/n*2;
                        if(Vector2.Distance(new Vector2(x,z),new Vector2(flower.position.x,flower.position.z))<2.9f){goal=current;break;}
                        foreach(int delta in new[]{-1,1,-n,n})
                        {
                            int next=current+delta;
                            if(next<0||next>=count||Mathf.Abs(next%n-current%n)>1||prev[next]!=-1||!passable(next))continue;
                            prev[next]=current;queue[tail++]=next;
                        }
                    }
                    if(goal<0)throw new Exception("No collision-safe route to "+flower.name);
                    int cursor=goal;while(cursor!=start){length+=2;cursor=prev[cursor];}start=goal;
                }
                Debug.Log("FINAL_ROUTE_PASS all="+trial.flowers.Length+" goalsWithReturns="+goals.Count+" routeMeters="+length+" estimatedMovingSeconds="+length/trial.speed);
            }
            finally {cc.enabled=true;}
            // Test the actual CharacterController, not only an obstacle overlap query.
            Vector3 before=new Vector3(77,0,40);before.y=trial.Ground(before)+2.1f;
            cc.enabled=false;trial.transform.position=before;cc.enabled=true;
            for(int i=0;i<150;i++)trial.Move(Vector2.right,1f/60);
            if(trial.transform.position.x>82)throw new Exception("Bee crossed the large rock collider: "+trial.transform.position);
            if(trial.transform.position.x<77.3f)throw new Exception("Movement test did not advance.");
            Debug.Log("FINAL_COLLISION_PASS blockedX="+trial.transform.position.x);
            Vector3 bridgeStart=new Vector3(225,0,76);bridgeStart.y=trial.Ground(bridgeStart)+2.1f;
            cc.enabled=false;trial.transform.position=bridgeStart;cc.enabled=true;
            for(int i=0;i<290;i++)trial.Move(Vector2.up,1f/60);
            if(trial.transform.position.z<114)throw new Exception("Bridge passage blocked at "+trial.transform.position);
            if(trial.DangerousWater(new Vector3(225,20,95))||trial.DangerousWater(new Vector3(75,20,190)))throw new Exception("Bridge classified as dangerous water.");
            Debug.Log("FINAL_BRIDGE_PASS end="+trial.transform.position);
            trial.RestartTrial();trial.Respawn();
            if(trial.Falls!=1||Vector3.Distance(trial.transform.position,trial.Checkpoint)>.1f||trial.Remaining>trial.timeLimit-11.9f)throw new Exception("Checkpoint penalty failed.");
            Debug.Log("FINAL_CHECKPOINT_PASS falls="+trial.Falls+" remaining="+trial.Remaining);
            trial.RestartTrial();
        }
    }
}
