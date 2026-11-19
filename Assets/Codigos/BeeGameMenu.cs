using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AbejaRA
{
    // The menu gates gameplay scripts; AR tracking stays live while a session is paused.
    [DefaultExecutionOrder(200)]
    public class BeeGameMenu : MonoBehaviour
    {
        public BeeGardenGame game;
        public BeeARPlacement placement;
        public GameObject gameplayHud;
        public Texture2D gardenPreview;
        public string CurrentPage { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsPlaying => CurrentPage == "Game";
        public float SavedVolume => PlayerPrefs.GetFloat("AbejaRA.Volume", .8f);
        public int SavedFPS => PlayerPrefs.GetInt("AbejaRA.FPS", 30);
        public float SavedScale => PlayerPrefs.GetFloat("AbejaRA.GardenSize", 1f);

        Canvas canvas;
        RectTransform safeArea;
        GameObject main, settings, help, pause, quit, pauseButton;
        Text playLabel, volumeLabel, fpsLabel, sizeLabel;
        Slider volumeSlider, sizeSlider;
        string returnPage = "Main";
        Vector3 baseScale;
        Font font;
        Sprite rounded;
        float oldVolume, oldTimeScale;
        int oldFPS;
        readonly Color ink = new Color(.035f, .105f, .09f);
        readonly Color panel = new Color(.075f, .18f, .145f);
        readonly Color cream = new Color(.96f, .97f, .9f);
        readonly Color muted = new Color(.66f, .76f, .68f);
        readonly Color honey = new Color(1f, .76f, .27f);

        void Start()
        {
            oldTimeScale = Time.timeScale; oldVolume = AudioListener.volume; oldFPS = Application.targetFrameRate;
            baseScale = game.garden.localScale;
            BuildInterface();
            ApplySettings();
            Show("Main");
        }

        void Update()
        {
            if (canvas == null) return;
            Rect r = Screen.safeArea;
            safeArea.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsPlaying) PauseGame();
                else if (CurrentPage == "Pause") ResumeGame();
                else if (CurrentPage == "Main") Show("Quit");
                else Back();
            }
        }

        void OnApplicationPause(bool suspended)
        {
            if (suspended && HasStarted && IsPlaying) PauseGame();
            if (suspended) PlayerPrefs.Save();
        }

        void OnDestroy()
        {
            if (canvas == null) return;
            Time.timeScale = oldTimeScale; AudioListener.volume = oldVolume;
            Application.targetFrameRate = oldFPS;
            if (canvas) Destroy(canvas.gameObject);
            if (rounded) { Destroy(rounded.texture); Destroy(rounded); }
        }

        public void StartGame() { HasStarted = true; Show("Game"); }
        public void PauseGame() { if (HasStarted) Show("Pause"); }
        public void ResumeGame() { Show("Game"); }
        public void MainMenu() { Show("Main"); }
        public void OpenSettings() { returnPage = CurrentPage; SyncControls(); Show("Settings"); }
        public void OpenHelp() { returnPage = CurrentPage; Show("Help"); }
        public void Back() { PlayerPrefs.Save(); Show(returnPage); }
        public void RestartGame() { game.Restart(); Show("Game"); }
        public void QuitGame() { PlayerPrefs.Save(); Application.Quit(); }

        void Show(string page)
        {
            CurrentPage = page;
            main.SetActive(page == "Main"); settings.SetActive(page == "Settings");
            help.SetActive(page == "Help"); pause.SetActive(page == "Pause"); quit.SetActive(page == "Quit");
            pauseButton.SetActive(page == "Game");
            bool playing = page == "Game";
            game.enabled = playing;
            if (placement) placement.enabled = playing;
            if (gameplayHud) gameplayHud.SetActive(playing);
            Time.timeScale = playing ? 1 : 0;
            if (playLabel) playLabel.text = HasStarted ? "CONTINUAR  >" : "JUGAR  >";
            Debug.Log("[AbejaRA Menu] " + page);
        }

        public void SetVolume(float value)
        {
            PlayerPrefs.SetFloat("AbejaRA.Volume", Mathf.Clamp01(value));
            AudioListener.volume = value;
            if (volumeLabel) volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }
        public void ToggleFPS()
        {
            PlayerPrefs.SetInt("AbejaRA.FPS", SavedFPS == 30 ? 60 : 30);
            Application.targetFrameRate = SavedFPS;
            SyncControls();
        }
        public void SetGardenSize(float value)
        {
            PlayerPrefs.SetFloat("AbejaRA.GardenSize", Mathf.Clamp(value, .5f, 1.5f));
            game.garden.localScale = baseScale * SavedScale;
            if (sizeLabel) sizeLabel.text = Mathf.RoundToInt(SavedScale * 100) + "%";
        }
        public void RestoreDefaults()
        {
            PlayerPrefs.SetFloat("AbejaRA.Volume", .8f);
            PlayerPrefs.SetInt("AbejaRA.FPS", 30);
            PlayerPrefs.SetFloat("AbejaRA.GardenSize", 1f);
            ApplySettings(); PlayerPrefs.Save();
        }
        void ApplySettings()
        {
            AudioListener.volume = SavedVolume;
            Application.targetFrameRate = SavedFPS;
            game.garden.localScale = baseScale * SavedScale;
            SyncControls();
        }
        void SyncControls()
        {
            if (volumeSlider) volumeSlider.SetValueWithoutNotify(SavedVolume);
            if (sizeSlider) sizeSlider.SetValueWithoutNotify(SavedScale);
            if (volumeLabel) volumeLabel.text = Mathf.RoundToInt(SavedVolume * 100) + "%";
            if (sizeLabel) sizeLabel.text = Mathf.RoundToInt(SavedScale * 100) + "%";
            if (fpsLabel) fpsLabel.text = SavedFPS == 30 ? "30 FPS  /  AHORRO" : "60 FPS  /  FLUIDO";
        }

        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y); return rt;
        }
        Image Box(string name, Transform parent, float x, float y, float w, float h, Color color, bool block = false)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = block; return image;
        }
        Text Label(string name, Transform parent, string value, int size, Color color, float x, float y, float w, float h, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = align; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false; return text;
        }
        Button Button(string name, Transform parent, string title, float x, float y, float w, float h, bool primary, UnityEngine.Events.UnityAction action)
        {
            var image = Box(name, parent, x, y, w, h, primary ? honey : new Color(.12f, .25f, .20f), true);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, 1, 1, .9f); colors.pressedColor = new Color(.75f, .8f, .7f); button.colors = colors;
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            Label("Label", image.transform, title, primary ? 23 : 18, primary ? ink : cream, 0, 0, w - 24, h - 8, TextAnchor.MiddleCenter, true);
            button.onClick.AddListener(action); return button;
        }
        Slider Slider(string name, Transform parent, float y, float min, float max, UnityEngine.Events.UnityAction<float> action)
        {
            var rt = Rect(name, parent, 150, y, 500, 48);
            var touchArea = rt.gameObject.AddComponent<Image>(); touchArea.color = Color.clear; touchArea.raycastTarget = true;
            var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.minValue = min; slider.maxValue = max;
            Box("Track", rt, 0, 0, 500, 8, new Color(.17f, .3f, .24f));
            var fillArea = Rect("FillArea", rt, 0, 0, 476, 8);
            var fill = Box("Fill", fillArea, 0, 0, 476, 8, honey);
            fill.rectTransform.sizeDelta = Vector2.zero;
            slider.fillRect = fill.rectTransform;
            var handleArea = Rect("HandleArea", rt, 0, 0, 476, 36);
            var handle = Box("Handle", handleArea, 0, 0, 30, 30, cream, true);
            handle.rectTransform.sizeDelta = new Vector2(30, 0);
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            var nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;
            slider.onValueChanged.AddListener(action); return slider;
        }
        GameObject Page(string name)
        {
            var page = Rect(name, safeArea, 0, 0, 1280, 720).gameObject;
            // Overscan covers wider phone displays while content stays inside the safe area.
            Box("Backdrop", page.transform, 0, 0, 5000, 3000, ink, true);
            return page;
        }
        void Heading(Transform parent, string eyebrow, string title, string subtitle)
        {
            Label("Eyebrow", parent, eyebrow, 15, honey, 0, 276, 1040, 28, TextAnchor.MiddleLeft, true);
            Label("Title", parent, title, 44, cream, 0, 210, 1040, 70, TextAnchor.MiddleLeft, true);
            Label("Description", parent, subtitle, 18, muted, 0, 150, 1040, 45);
        }
        Sprite RoundedSprite()
        {
            const int size = 64; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "MenuRoundedCorner"; texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(15 - x, x - 48, 0), dy = Mathf.Max(15 - y, y - 48, 0);
                float alpha = Mathf.Clamp01(16 - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply(); return Sprite.Create(texture, new UnityEngine.Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 17);
        }
        void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); rounded = RoundedSprite();
            var go = new GameObject("Menu_AbejaRA", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            safeArea = Rect("SafeArea", go.transform, 0, 0, 0, 0); safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
            main = Page("Main");
            Label("Brand", main.transform, "ABEJA  /  REALIDAD AUMENTADA", 16, honey, -326, 286, 480, 32, TextAnchor.MiddleLeft, true);
            Label("HeroTitle", main.transform, "EL JARDÍN\nTE ESPERA.", 59, cream, -326, 160, 480, 170, TextAnchor.MiddleLeft, true);
            Label("HeroDescription", main.transform, "Una pequeña abeja. Una gran misión.\nExplora el bosque y poliniza cada flor.", 21, muted, -326, 25, 480, 76);
            var play = Button("Play", main.transform, "JUGAR  >", -326, -88, 480, 72, true, StartGame);
            playLabel = play.GetComponentInChildren<Text>();
            Button("Settings", main.transform, "CONFIGURACIÓN", -451, -174, 230, 58, false, OpenSettings);
            Button("Help", main.transform, "CÓMO JUGAR", -201, -174, 230, 58, false, OpenHelp);
            Button("Exit", main.transform, "SALIR", -496, -253, 140, 44, false, () => Show("Quit"));
            Label("Footer", main.transform, "EXPLORA  ·  RECOLECTA  ·  POLINIZA", 13, muted, -176, -253, 450, 32);
            var art = Box("GardenCard", main.transform, 300, 40, 520, 500, panel);
            if (gardenPreview)
            {
                var raw = Rect("GardenPreview", art.transform, 0, 25, 492, 366).gameObject.AddComponent<RawImage>();
                raw.texture = gardenPreview; raw.raycastTarget = false;
            }
            Box("Tag", art.transform, -113, 220, 240, 34, honey);
            Label("TagText", art.transform, "TU MUNDO EN MINIATURA", 13, ink, -113, 220, 228, 30, TextAnchor.MiddleCenter, true);
            Label("CardTitle", art.transform, "UN BOSQUE LLENO DE VIDA", 23, cream, 0, -174, 464, 40, TextAnchor.MiddleLeft, true);
            Label("CardCaption", art.transform, "Árboles, montañas y flores por descubrir.", 17, muted, 0, -212, 464, 35);
            Label("ARNote", main.transform, "Coloca el jardín en tu espacio y comienza a explorar.", 16, muted, 300, -255, 520, 50, TextAnchor.MiddleCenter);

            settings = Page("Settings");
            Heading(settings.transform, "A TU MANERA", "Configuración", "Los cambios se guardan automáticamente en este dispositivo.");
            Label("VolumeTitle", settings.transform, "VOLUMEN", 18, cream, -340, 52, 360, 32, TextAnchor.MiddleLeft, true);
            volumeLabel = Label("VolumeValue", settings.transform, "80%", 18, honey, 460, 52, 120, 32, TextAnchor.MiddleRight);
            volumeSlider = Slider("Volume", settings.transform, 52, 0, 1, SetVolume);
            Label("FPSTitle", settings.transform, "FLUIDEZ", 18, cream, -340, -42, 360, 32, TextAnchor.MiddleLeft, true);
            var fps = Button("FPS", settings.transform, "30 FPS  /  AHORRO", 150, -42, 500, 54, false, ToggleFPS); fpsLabel = fps.GetComponentInChildren<Text>();
            Label("FPSHelp", settings.transform, "60 FPS puede consumir más batería.", 15, muted, 150, -87, 500, 28);
            Label("ScaleTitle", settings.transform, "TAMAÑO DEL JARDÍN", 18, cream, -340, -146, 360, 35, TextAnchor.MiddleLeft, true);
            sizeLabel = Label("ScaleValue", settings.transform, "100%", 18, honey, 460, -146, 120, 32, TextAnchor.MiddleRight);
            sizeSlider = Slider("GardenSize", settings.transform, -146, .5f, 1.5f, SetGardenSize);
            Button("SettingsBack", settings.transform, "GUARDAR Y VOLVER", -330, -268, 380, 58, true, Back);
            Button("Defaults", settings.transform, "RESTABLECER", 260, -268, 280, 58, false, RestoreDefaults);

            help = Page("Help");
            Heading(help.transform, "EMPIEZA A EXPLORAR", "Cómo jugar", "Tu cámara convierte una superficie real en un pequeño jardín.");
            string[] titles = { "01  ENCUENTRA TU ESPACIO", "02  COLOCA EL JARDÍN", "03  GUÍA A TU ABEJA" };
            string[] descriptions = { "Permite usar la cámara. Apunta a una mesa\no al piso con buena luz y mueve el teléfono\nlentamente para detectar la superficie.", "Cuando indique Superficie detectada,\napunta hacia ella y pulsa Colocar.\nEl mapa se queda fijo en ese lugar.", "Toca el terreno para mover a la abeja.\nAcércate a cada flor para polinizarla.\nCompleta el contador de flores." };
            for (int i = 0; i < 3; i++)
            {
                float x = -355 + i * 355; var card = Box("Step" + i, help.transform, x, -18, 330, 220, panel);
                Label("StepTitle", card.transform, titles[i], 17, honey, 0, 65, 286, 48, TextAnchor.MiddleLeft, true);
                Label("StepBody", card.transform, descriptions[i], 18, cream, 0, -20, 286, 130);
            }
            Label("EditorHint", help.transform, "En computadora: WASD o flechas para moverte. Escape abre la pausa.", 17, muted, 0, -177, 1040, 46);
            Button("HelpBack", help.transform, "ENTENDIDO", -330, -268, 380, 58, true, Back);

            pause = Page("Pause");
            Heading(pause.transform, "TÓMATE UN RESPIRO", "Juego en pausa", "Tu partida se conserva mientras vuelves a explorar.");
            Button("Resume", pause.transform, "CONTINUAR", -280, 36, 480, 68, true, ResumeGame);
            Button("PauseSettings", pause.transform, "CONFIGURACIÓN", 280, 36, 480, 68, false, OpenSettings);
            Button("Restart", pause.transform, "REINICIAR PARTIDA", -280, -56, 480, 64, false, RestartGame);
            Button("PauseHelp", pause.transform, "CÓMO JUGAR", 280, -56, 480, 64, false, OpenHelp);
            Button("Home", pause.transform, "MENÚ PRINCIPAL", -280, -148, 480, 64, false, MainMenu);

            quit = Page("Quit");
            Heading(quit.transform, "HASTA LA PRÓXIMA", "¿Salir del juego?", "La configuración quedará guardada. La partida actual no se guardará.");
            Button("CancelQuit", quit.transform, "VOLVER", -280, -32, 480, 68, true, () => Show("Main"));
            Button("ConfirmQuit", quit.transform, "SALIR", 280, -32, 480, 68, false, QuitGame);
            pauseButton = Button("PauseButton", safeArea, "PAUSA  II", 0, 0, 150, 48, false, PauseGame).gameObject;
            var pauseRect = pauseButton.GetComponent<RectTransform>(); pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1, 1);
            pauseRect.pivot = Vector2.one; pauseRect.anchoredPosition = new Vector2(-26, -126);
        }
    }
}
