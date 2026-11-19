using UnityEngine;

public class DisparoAbeja : MonoBehaviour
{
    [Min(1)] public int disparosMaximos = 30;
    public int disparosRestantes = 30;
    public float velocidadProyectil = 24f;
    public float intervaloEntreDisparos = 0.25f;

    private MovimientoAbeja3D movimiento;
    private VidasAbeja vidas;
    private CapsuleCollider colisionAbeja;
    private float siguienteDisparo;
    private static Material materialAzul;
    private AudioSource audioDisparo;
    private AudioClip sonidoDisparo;
    private const float volumenSonidoDisparo = 1.5f;

    void Awake()
    {
        movimiento = GetComponent<MovimientoAbeja3D>();
        vidas = GetComponent<VidasAbeja>();
        colisionAbeja = GetComponent<CapsuleCollider>();
        sonidoDisparo = movimiento != null ? movimiento.sonidoDisparo : null;
        if (sonidoDisparo != null && sonidoDisparo.loadState != AudioDataLoadState.Loaded)
            sonidoDisparo.LoadAudioData();

        // Reproduce el efecto desde la cámara para evitar que la escala o distancia
        // de la abeja reduzca el volumen en escenas con objetos muy grandes.
        AudioListener oyente = FindFirstObjectByType<AudioListener>();
        audioDisparo = oyente != null ? oyente.GetComponent<AudioSource>() : null;
        if (audioDisparo == null) audioDisparo = GetComponent<AudioSource>();
        if (audioDisparo == null)
            audioDisparo = (oyente != null ? oyente.gameObject : gameObject).AddComponent<AudioSource>();
        audioDisparo.playOnAwake = false;
        audioDisparo.loop = false;
        audioDisparo.mute = false;
        audioDisparo.volume = 1f;
        audioDisparo.pitch = 1f;
        audioDisparo.dopplerLevel = 0f;
        audioDisparo.spatialBlend = 0f;
        audioDisparo.bypassEffects = true;
        audioDisparo.bypassListenerEffects = true;
        audioDisparo.bypassReverbZones = true;
        disparosRestantes = disparosMaximos;
    }

    void Update()
    {
        if (movimiento == null || !movimiento.enabled || (vidas != null && vidas.vidasActuales <= 0)) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)) Disparar();
    }

    void Disparar()
    {
        if (disparosRestantes <= 0 || Time.time < siguienteDisparo) return;
        siguienteDisparo = Time.time + intervaloEntreDisparos;
        disparosRestantes--;
        if (sonidoDisparo != null && audioDisparo != null)
        {
            if (sonidoDisparo.loadState != AudioDataLoadState.Loaded) sonidoDisparo.LoadAudioData();
            if (sonidoDisparo.loadState == AudioDataLoadState.Loaded)
                audioDisparo.PlayOneShot(sonidoDisparo, volumenSonidoDisparo);
            else
                Debug.LogWarning("El clip de sonido del disparo no se pudo cargar: " + sonidoDisparo.name, this);
        }
        else
        {
            Debug.LogWarning("Asigna un clip en Sonido Disparo del componente Movimiento Abeja 3D.", this);
        }

        colisionAbeja = GetComponent<CapsuleCollider>();
        Vector3 adelante = transform.forward;
        Vector3 centro = colisionAbeja != null ? colisionAbeja.bounds.center : transform.position;
        Renderer[] partes = GetComponentsInChildren<Renderer>();
        Bounds limites = new Bounds(centro, Vector3.one);
        if (partes.Length > 0)
        {
            limites = partes[0].bounds;
            for (int i = 1; i < partes.Length; i++) limites.Encapsulate(partes[i].bounds);
        }
        float radioProyectil = Mathf.Max(0.45f, limites.extents.magnitude * 0.11f);
        float distanciaSalida = limites.extents.magnitude + radioProyectil + 0.25f;
        Vector3 origen = centro + adelante * distanciaSalida;
        origen.y += Mathf.Max(0f, limites.extents.y * 0.12f);
        VidasAbeja objetivo = BuscarEnemigoMasCercano();
        Vector3 direccion = adelante.normalized;

        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "DisparoAzul_Abeja";
        esfera.transform.position = origen;
        esfera.transform.rotation = Quaternion.LookRotation(direccion);
        esfera.transform.localScale = Vector3.one * (radioProyectil * 2f);
        SphereCollider collider = esfera.GetComponent<SphereCollider>();
        if (collider != null) collider.enabled = false;
        Renderer render = esfera.GetComponent<Renderer>();
        if (render != null)
        {
            if (materialAzul == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader != null)
                {
                    materialAzul = new Material(shader);
                    materialAzul.color = new Color(0.02f, 0.28f, 1f, 1f);
                    if (materialAzul.HasProperty("_EmissionColor"))
                    {
                        materialAzul.EnableKeyword("_EMISSION");
                        materialAzul.SetColor("_EmissionColor", new Color(0.02f, 0.2f, 1f) * 2f);
                    }
                }
            }
            if (materialAzul != null) render.sharedMaterial = materialAzul;
        }

        TrailRenderer estela = esfera.AddComponent<TrailRenderer>();
        estela.time = 0.7f;
        estela.startWidth = 0.32f;
        estela.endWidth = 0.04f;
        estela.minVertexDistance = 0.02f;
        estela.startColor = new Color(0.08f, 0.42f, 1f, 1f);
        estela.endColor = new Color(0.05f, 0.2f, 1f, 0f);
        if (materialAzul != null) estela.sharedMaterial = materialAzul;

        esfera.AddComponent<ProyectilAbeja>().Inicializar(objetivo, direccion, velocidadProyectil, 30f, radioProyectil);
    }

    VidasAbeja BuscarEnemigoMasCercano()
    {
        MovimientoAbejaEnemiga[] enemigos = FindObjectsByType<MovimientoAbejaEnemiga>(FindObjectsSortMode.None);
        VidasAbeja masCercano = null;
        float menorDistancia = float.MaxValue;
        foreach (MovimientoAbejaEnemiga enemigo in enemigos)
        {
            if (enemigo == null || !enemigo.enabled || !enemigo.gameObject.activeInHierarchy) continue;
            VidasAbeja salud = enemigo.GetComponent<VidasAbeja>();
            if (salud == null || salud.vidasActuales <= 0) continue;
            float distancia = (enemigo.transform.position - transform.position).sqrMagnitude;
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                masCercano = salud;
            }
        }
        return masCercano;
    }

    Vector3 CentroObjetivo(VidasAbeja objetivo)
    {
        CapsuleCollider collider = objetivo.GetComponent<CapsuleCollider>();
        return collider != null && collider.enabled ? collider.bounds.center : objetivo.transform.position;
    }

    void OnGUI()
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 8, 4, 4)
        };
        GUI.Box(new Rect(Screen.width - 262, 12, 250, 36), "Disparos: " + disparosRestantes + " / " + disparosMaximos, estilo);
    }
}







