using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbejaRA
{
    public class BeeGardenGame : MonoBehaviour
    {
        public Transform garden, bee, visual;
        public Camera view;
        public Transform[] flowers;
        public Text status, score;
        public GameObject previewFloor;
        public float speed = .22f;
        public Vector2 movementHalfExtents = new Vector2(.55f,.55f);
        public bool squareBounds;
        public float hoverHeight = .11f;
        public float keyboardLookahead = .12f;
        public float collectRadius = .075f;
        public float hoverBob = .007f;
        public Vector3 spawnPosition = new Vector3(0,.11f,-.12f);
        public TerrainData groundData;
        public MeshCollider groundCollider;
        public bool placed;
        public bool deviceAR;
        Vector3 target;
        Vector3 visualRest;
        bool[] collected;
        int count;
        public Vector3 Target => target;
        public int Collected => count;
        public float GroundHeight(float x,float z)
        {
            if(!groundData) return 0;
            return groundData.GetInterpolatedHeight(Mathf.Clamp01((x+groundData.size.x*.5f)/groundData.size.x),Mathf.Clamp01((z+groundData.size.z*.5f)/groundData.size.z));
        }
        void Awake()
        {
            Application.runInBackground = true;
            collected = new bool[flowers.Length];
            visualRest = visual.localPosition;
            target = bee.localPosition;
            deviceAR = !Application.isEditor && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
            placed = !deviceAR;
            garden.gameObject.SetActive(placed);
            if(previewFloor) previewFloor.SetActive(!deviceAR);
            Refresh();
        }
        public void Place(Pose pose)
        {
            garden.SetPositionAndRotation(pose.position, pose.rotation);
            garden.gameObject.SetActive(true);
            placed = true;
            target = bee.localPosition;
            Refresh();
        }
        public void SetTarget(Vector3 world)
        {
            var p = garden.InverseTransformPoint(world);
            Vector2 xz = squareBounds ? new Vector2(Mathf.Clamp(p.x,-movementHalfExtents.x,movementHalfExtents.x),Mathf.Clamp(p.z,-movementHalfExtents.y,movementHalfExtents.y)) : Vector2.ClampMagnitude(new Vector2(p.x,p.z),movementHalfExtents.x);
            target = new Vector3(xz.x,GroundHeight(xz.x,xz.y)+hoverHeight,xz.y);
        }
        void Update()
        {
            if(!placed) return;
            Vector2 input = Vector2.zero;
            var k = Keyboard.current;
            if(k != null)
            {
                input.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1:0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1:0);
                input.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1:0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1:0);
                if(k.rKey.wasPressedThisFrame) Restart();
            }
            if(input.sqrMagnitude > 0) SetTarget(garden.TransformPoint(bee.localPosition + new Vector3(input.x,0,input.y).normalized * keyboardLookahead));
            bool down = false; Vector2 screen = Vector2.zero;
            if(Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { down = true; screen = Touchscreen.current.primaryTouch.position.ReadValue(); }
            else if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { down = true; screen = Mouse.current.position.ReadValue(); }
            if(down && !OverUI(screen))
            {
                Ray ray = view.ScreenPointToRay(screen);
                if(groundCollider)
                {
                    if(groundCollider.Raycast(ray,out RaycastHit hit,100)) SetTarget(hit.point);
                }
                else
                {
                    var plane = new Plane(garden.up,garden.position);
                    if(plane.Raycast(ray,out float distance)) SetTarget(ray.GetPoint(distance));
                }
            }
            Vector3 delta = target-bee.localPosition;
            delta.y=0;
            Vector3 next = Vector3.MoveTowards(bee.localPosition, new Vector3(target.x,bee.localPosition.y,target.z),speed*Time.deltaTime);
            next.y=GroundHeight(next.x,next.z)+hoverHeight;
            bee.localPosition=next;
            if(delta.sqrMagnitude>.00002f)
                bee.localRotation = Quaternion.Slerp(bee.localRotation,Quaternion.LookRotation(new Vector3(delta.x,0,delta.z)),Time.deltaTime*8);
            visual.localPosition = visualRest + Vector3.up * (Mathf.Sin(Time.time*6)*hoverBob);
            visual.localRotation = Quaternion.Euler(Mathf.Sin(Time.time*7)*3,0,Mathf.Sin(Time.time*8)*3);
            for(int i=0;i<flowers.Length;i++)
            {
                Vector3 flowerPoint=garden.InverseTransformPoint(flowers[i].position);
                if(collected[i] || Vector2.Distance(new Vector2(bee.localPosition.x,bee.localPosition.z),new Vector2(flowerPoint.x,flowerPoint.z))>collectRadius) continue;
                collected[i]=true; count++;
                var glow = flowers[i].Find("Nectar");
                if(glow) glow.gameObject.SetActive(false);
                Refresh();
            }
        }
        public bool OverUI(Vector2 point)
        {
            if(!EventSystem.current) return false;
            var data = new PointerEventData(EventSystem.current){position=point};
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(data,hits);
            return hits.Count>0;
        }
        public void Restart()
        {
            count=0;
            for(int i=0;i<flowers.Length;i++) { collected[i]=false; var n=flowers[i].Find("Nectar"); if(n)n.gameObject.SetActive(true); }
            bee.localPosition=new Vector3(spawnPosition.x,GroundHeight(spawnPosition.x,spawnPosition.z)+hoverHeight,spawnPosition.z); target=bee.localPosition;
            Refresh();
        }
        public void Reposition()
        {
            Restart();
            if(deviceAR) {placed=false;garden.gameObject.SetActive(false);Refresh();}
        }
        public void Refresh()
        {
            if(score) score.text = count.ToString("00") + " / " + flowers.Length.ToString("00") + "   FLORES";
            if(status) status.text = !placed ? "Mueve el móvil para detectar una mesa. Toca para colocar el jardín." : count == flowers.Length ? "¡Jardín polinizado! Pulsa Reiniciar para volver a jugar." : "Toca el jardín para guiar a tu abeja hasta cada flor.";
        }
    }
}
