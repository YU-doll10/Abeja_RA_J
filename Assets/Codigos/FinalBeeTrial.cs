using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbejaRA
{
    [RequireComponent(typeof(CharacterController))]
    public class FinalBeeTrial : MonoBehaviour
    {
        public Terrain terrain;
        public Transform visual;
        public Camera followCamera;
        public Transform[] flowers;
        public Transform[] returnPoints;
        public Transform[] bridgeDecks;
        public Vector3 spawn;
        public float speed = 9f;
        public float timeLimit = 240;
        public int Collected { get; private set; }
        public float Remaining { get; private set; }
        public int Falls { get; private set; }
        public bool Finished { get; private set; }
        public bool IsFalling { get; private set; }
        public bool ReturningToPath { get; private set; }
        public Vector2 touchInput;
        public bool paused;
        public Vector3 Checkpoint { get; private set; }
        CharacterController body;
        Vector3 cameraVelocity;
        readonly RaycastHit[] cameraHits = new RaycastHit[16];
        Text progress, clock, hint, objective;
        GameObject resultPanel;
        Text resultText;
        float messageUntil;
        float respawnUntil;
        Vector3 visualRest;
        Quaternion visualRestRotation;
        float fallElapsed, fallStartY, fallEndY;
        Vector3 fallStartPosition, fallEndPosition;
        const float FallDuration = 1.15f;
        Transform returnTarget;

        void Start()
        {
            body = GetComponent<CharacterController>(); visualRest = visual.localPosition;
            visualRestRotation = visual.localRotation;
            BuildHUD(); RestartTrial();
            Application.runInBackground = true;
        }
        public float Ground(Vector3 p)
        {
            float h = terrain.SampleHeight(p) + terrain.transform.position.y;
            foreach (var deck in bridgeDecks)
            {
                Vector3 local = deck.InverseTransformPoint(p);
                // Raise the hover target before reaching the bridge lip, so it cannot trap the controller.
                if (Mathf.Abs(local.x) <= .5f && Mathf.Abs(local.z) <= .56f)
                    h = Mathf.Max(h, deck.position.y + deck.localScale.y * .5f);
            }
            return h;
        }
        public bool DangerousWater(Vector3 p)
        {
            bool first = Mathf.Abs(p.z - StreamCenter(p.x, false)) < 10;
            bool second = Mathf.Abs(p.z - StreamCenter(p.x, true)) < 10;
            return (first && Mathf.Abs(p.x - 225) > 3.2f) || (second && Mathf.Abs(p.x - 75) > 2.7f);
        }
        public static float StreamCenter(float x, bool second)
        { return second ? 190 + 5 * Mathf.Sin((x - 75) * .03f) : 95 + 4 * Mathf.Sin((x - 225) * .035f); }
        public void RestartTrial()
        {
            paused = false; Finished = false; IsFalling = false; ReturningToPath = false; Collected = 0; Falls = 0; Remaining = timeLimit;
            respawnUntil = 0; returnTarget = null;
            visual.localPosition = visualRest; visual.localRotation = visualRestRotation;
            Checkpoint = spawn;
            foreach (var flower in flowers) { var beacon = flower.Find("Baliza"); if (beacon) beacon.gameObject.SetActive(true); }
            if (returnPoints != null) foreach (var point in returnPoints) if (point) point.gameObject.SetActive(false);
            UpdateBeacons();
            body.enabled = false; transform.position = spawn; body.enabled = true;
            resultPanel.SetActive(false);
            Message("Sigue la baliza dorada. En los desvíos, vuelve a la baliza azul para continuar por el sendero. Cruza el agua por los puentes.", 9);
            Refresh();
        }
        public void TogglePause()
        {
            if (Finished) return;
            paused = !paused;
            resultPanel.SetActive(paused);
            resultText.text = "JUEGO EN PAUSA\nPulsa Continuar para seguir explorando.";
            resultPanel.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = "CONTINUAR";
        }
        public void ResultAction() { if (paused && !Finished) TogglePause(); else RestartTrial(); }
        public void Respawn()
        {
            if (Time.time < respawnUntil || Finished) return;
            IsFalling = false;
            Falls++; Remaining = Mathf.Max(0, Remaining - 12);
            body.enabled = false; transform.position = Checkpoint; body.enabled = true;
            visual.localPosition = visualRest; visual.localRotation = visualRestRotation;
            cameraVelocity = Vector3.zero;
            respawnUntil = Time.time + 1;
            Message("¡Corriente peligrosa! Vuelves al refugio. Penalización: 12 segundos.", 5);
        }
        void BeginFall()
        {
            if (IsFalling || Time.time < respawnUntil) return;
            IsFalling = true; fallElapsed = 0; fallStartY = transform.position.y;
            fallStartPosition = transform.position;
            bool second = Mathf.Abs(transform.position.z - StreamCenter(transform.position.x, true))
                < Mathf.Abs(transform.position.z - StreamCenter(transform.position.x, false));
            float riverZ = second ? 190 : 95;
            fallEndY = 10 + riverZ * .035f - 5.8f - 1.6f;
            fallEndPosition = fallStartPosition;
            fallEndPosition.z = Mathf.MoveTowards(fallStartPosition.z, StreamCenter(fallStartPosition.x, second), 2.5f);
            body.enabled = false;
            Message("¡CAÍSTE AL AGUA! La corriente te devuelve al último refugio.", 2);
            Debug.Log("[Nivel Final] Caída al agua iniciada en " + transform.position);
        }
        void TickFall(float dt)
        {
            fallElapsed += dt;
            float t = Mathf.Clamp01(fallElapsed / FallDuration);
            Vector3 p = Vector3.Lerp(fallStartPosition, fallEndPosition, Mathf.SmoothStep(0, 1, t));
            p.y = Mathf.Lerp(fallStartY, fallEndY, t * t);
            transform.position = p;
            visual.localRotation = Quaternion.Euler(t * 100, 0, Mathf.Sin(t * Mathf.PI) * 25);
            if (t >= 1) Respawn();
        }
        void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
                if (Keyboard.current.rKey.wasPressedThisFrame) RestartTrial();
            }
            if (paused || Finished) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (Remaining <= 0) { End(false); return; }
            if (IsFalling) { TickFall(Time.deltaTime); Refresh(); return; }
            Vector2 input = touchInput;
            var k = Keyboard.current;
            if (k != null)
            {
                input.x += (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                input.y += (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
            }
            Move(Vector2.ClampMagnitude(input, 1), Time.deltaTime);
            if (DangerousWater(transform.position)) { BeginFall(); Refresh(); return; }
            if (ReturningToPath && Vector3.Distance(transform.position, returnTarget.position + Vector3.up * 2.1f) < 4)
            {
                returnTarget.gameObject.SetActive(false); ReturningToPath = false; returnTarget = null;
                UpdateBeacons(); Message("De vuelta al sendero principal. Sigue la siguiente baliza dorada.", 4);
            }
            if (!ReturningToPath && Collected < flowers.Length && Vector3.Distance(transform.position, flowers[Collected].position + Vector3.up * 2.1f) < 3.8f)
            {
                flowers[Collected].Find("Baliza").gameObject.SetActive(false);
                int previous = Collected;
                Collected++;
                if (returnPoints != null && previous < returnPoints.Length && returnPoints[previous])
                {
                    ReturningToPath = true; returnTarget = returnPoints[previous]; returnTarget.gameObject.SetActive(true);
                    Message("Flor del desvío recogida. Regresa a la baliza azul del sendero principal para continuar.", 6);
                }
                else if (Collected == 6 || Collected == 12)
                {
                    Checkpoint = transform.position;
                    Message("REFUGIO ACTIVADO. Tu siguiente caída te devolverá aquí.", 5);
                }
                else Message("Flor polinizada. Sigue la siguiente baliza dorada.", 3);
                UpdateBeacons();
                if (Collected == flowers.Length) { End(true); return; }
            }
            visual.localPosition = visualRest + Vector3.up * Mathf.Sin(Time.time * 7) * .13f;
            if (!ReturningToPath && Collected < flowers.Length)
            {
                var beacon = flowers[Collected].Find("Baliza");
                beacon.localRotation = Quaternion.Euler(0, Time.time * 55, 0);
            }
            Refresh();
        }
        void UpdateBeacons()
        {
            for (int i = 0; i < flowers.Length; i++)
            {
                var beacon = flowers[i].Find("Baliza");
                if (beacon) beacon.gameObject.SetActive(!ReturningToPath && i == Collected);
            }
        }
        public void Move(Vector2 input, float dt)
        {
            if (IsFalling || !body.enabled) return;
            Vector3 delta = new Vector3(input.x, 0, input.y) * speed * dt;
            Vector3 next = transform.position + delta;
            next.x = Mathf.Clamp(next.x, 5, 295); next.z = Mathf.Clamp(next.z, 5, 295);
            bool onBridge = (Mathf.Abs(next.z - 95) < 18 && Mathf.Abs(next.x - 225) < 4) || (Mathf.Abs(next.z - 190) < 18 && Mathf.Abs(next.x - 75) < 3.5f);
            float slope = terrain.terrainData.GetSteepness(next.x / 300f, next.z / 300f);
            // River banks are deliberately descendable: steep-slope protection must not act as an invisible fence around the water.
            float riverDistance = Mathf.Min(Mathf.Abs(next.z - StreamCenter(next.x, false)), Mathf.Abs(next.z - StreamCenter(next.x, true)));
            bool riverBank = riverDistance < 23;
            if (slope > 42 && !onBridge && !riverBank && input.sqrMagnitude > 0)
            {
                delta = Vector3.zero;
                Message("Ladera demasiado empinada. Busca el sendero entre las rocas.", 2);
            }
            else { delta.x = next.x - transform.position.x; delta.z = next.z - transform.position.z; }
            float ground = Ground(transform.position + delta);
            delta.y = Mathf.Clamp(ground + 2.1f - transform.position.y, -14 * dt, 14 * dt);
            body.Move(delta);
            if (input.sqrMagnitude > .01f) visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(new Vector3(input.x, 0, input.y)), dt * 9);
        }
        void LateUpdate()
        {
            Vector3 focus = transform.position + Vector3.up;
            Vector3 target = transform.position + new Vector3(0, 34, -15);
            target.y = Mathf.Max(target.y, terrain.SampleHeight(target) + 9);
            followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position, target, ref cameraVelocity, .14f);
            Vector3 arm = followCamera.transform.position - focus;
            float distance = arm.magnitude;
            int hits = Physics.SphereCastNonAlloc(focus, .65f, arm.normalized, cameraHits, distance, ~0, QueryTriggerInteraction.Ignore);
            float safeDistance = distance;
            for (int i = 0; i < hits; i++)
                if (cameraHits[i].collider != body && cameraHits[i].distance > 1)
                    safeDistance = Mathf.Min(safeDistance, Mathf.Max(3, cameraHits[i].distance - 1));
            if (safeDistance < distance) followCamera.transform.position = focus + arm.normalized * safeDistance;
            followCamera.transform.LookAt(focus);
        }
        void Message(string text, float seconds) { if (hint) hint.text = text; messageUntil = Time.time + seconds; }
        void Refresh()
        {
            progress.text = "FLORES  " + Collected.ToString("00") + " / " + flowers.Length.ToString("00");
            int seconds = Mathf.CeilToInt(Remaining);
            clock.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            if (ReturningToPath) objective.text = "REGRESA AL SENDERO  ·  BALIZA AZUL  ·  " + Mathf.RoundToInt(Vector3.Distance(transform.position, returnTarget.position)) + " m";
            else if (Collected < flowers.Length) objective.text = "SIGUIENTE FLOR  " + (Collected + 1) + "  ·  " + Mathf.RoundToInt(Vector3.Distance(transform.position, flowers[Collected].position)) + " m";
            if (Time.time > messageUntil) hint.text = "WASD / flechas: mover   ·   Escape: pausa   ·   R: reiniciar   |   Los puentes son la única ruta segura sobre el agua.";
        }
        void End(bool won)
        {
            Finished = true; resultPanel.SetActive(true);
            resultText.text = won ? "¡ÚLTIMO NIVEL COMPLETADO!\nTodas las flores están polinizadas.\nCaídas: " + Falls : "SE ACABÓ EL TIEMPO\nLos refugios y las balizas te ayudarán a mejorar tu ruta.";
            resultPanel.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = "REINTENTAR";
        }
        RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = position; rt.sizeDelta = size; return rt;
        }
        Text Label(string name, Transform parent, string value, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            var t = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text = value; t.fontSize = fontSize; t.color = Color.white; t.alignment = align; t.raycastTarget = false; return t;
        }
        Button Button(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var rt = Rect(name, parent, anchor, pos, size); rt.gameObject.AddComponent<Image>().color = new Color(1, .74f, .2f);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.onClick.AddListener(click);
            Label("Label", rt, label, Vector2.one * .5f, Vector2.zero, size, 19, TextAnchor.MiddleCenter).color = new Color(.05f,.12f,.1f); return b;
        }
        void BuildHUD()
        {
            var root = new GameObject("HUD_Nivel_Final", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            var header = Rect("Header", root.transform, new Vector2(.5f, 1), new Vector2(0,-46), new Vector2(1280,92)); header.gameObject.AddComponent<Image>().color = new Color(.025f,.09f,.075f,.94f);
            Label("Title", header, "NIVEL FINAL  /  PASO DE LAS CUMBRES", Vector2.one * .5f, new Vector2(-235,14), new Vector2(760,34), 24, TextAnchor.MiddleLeft).color = new Color(1,.79f,.3f);
            objective = Label("Objective", header, "SIGUE LAS BALIZAS", Vector2.one * .5f, new Vector2(-235,-20), new Vector2(760,28), 17, TextAnchor.MiddleLeft);
            progress = Label("Progress", header, "", Vector2.one * .5f, new Vector2(310,0), new Vector2(240,60), 24, TextAnchor.MiddleCenter);
            clock = Label("Clock", header, "", Vector2.one * .5f, new Vector2(520,0), new Vector2(140,60), 34, TextAnchor.MiddleCenter);
            hint = Label("Hint", root.transform, "", new Vector2(.5f,0), new Vector2(-90,48), new Vector2(1020,74), 18, TextAnchor.MiddleLeft);
            var shadow = hint.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(1,-2);
            Button("Pause", root.transform, "PAUSA", new Vector2(1,0), new Vector2(-95,48), new Vector2(130,52), TogglePause);
            resultPanel = Rect("Result", root.transform, Vector2.one*.5f, Vector2.zero, new Vector2(760,340)).gameObject;
            resultPanel.AddComponent<Image>().color = new Color(.025f,.09f,.075f,.98f);
            resultText = Label("ResultText", resultPanel.transform, "", Vector2.one*.5f,new Vector2(0,55),new Vector2(690,190),27,TextAnchor.MiddleCenter);
            Button("ResultAction", resultPanel.transform,"REINTENTAR",Vector2.one*.5f,new Vector2(0,-105),new Vector2(290,58),ResultAction);
            if (Application.isMobilePlatform)
            {
                Vector2[] dirs = { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
                string[] labels = { "▲", "◀", "▼", "▶" };
                for(int i=0;i<4;i++)
                {
                    Vector2 p = new Vector2(145,170) + dirs[i] * 58;
                    var b=Button("Move"+i,root.transform,labels[i],Vector2.zero,p,new Vector2(54,54),()=>{});
                    var hold=b.gameObject.AddComponent<FinalBeeHold>(); hold.trial=this; hold.direction=dirs[i];
                }
            }
        }
    }
    public class FinalBeeHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public FinalBeeTrial trial; public Vector2 direction; bool held;
        public void OnPointerDown(PointerEventData e) { if(!held){held=true;trial.touchInput+=direction;} }
        public void OnPointerUp(PointerEventData e) { if(held){held=false;trial.touchInput-=direction;} }
        void OnDisable(){ if(held && trial){trial.touchInput-=direction;held=false;} }
    }
}
