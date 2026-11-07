using UnityEngine;

public class VidasAbeja : MonoBehaviour
{
    [Min(1)] public int vidasMaximas = 10;
    [Min(0)] public int vidasActuales = 10;
    public bool esJugador = true;
    [Min(0.1f)] public float intervaloDanioContacto = 1f;

    private float siguienteDanioContacto;

    public void Inicializar(bool jugador)
    {
        esJugador = jugador;
        vidasMaximas = jugador ? 10 : 5;
        vidasActuales = vidasMaximas;
    }

    void OnCollisionStay(Collision colision)
    {
        if (!esJugador || Time.time < siguienteDanioContacto) return;
        MovimientoAbejaEnemiga enemigoEnContacto = colision.collider.GetComponentInParent<MovimientoAbejaEnemiga>();
        if (enemigoEnContacto == null || !enemigoEnContacto.enabled) return;

        siguienteDanioContacto = Time.time + intervaloDanioContacto;
        RecibirDanio();
    }

    public void RecibirDanio(int cantidad = 1)
    {
        if (vidasActuales <= 0) return;
        vidasActuales = Mathf.Max(0, vidasActuales - Mathf.Max(1, cantidad));
        if (vidasActuales == 0)
        {
            MovimientoAbejaEnemiga enemigo = GetComponent<MovimientoAbejaEnemiga>();
            if (enemigo != null)
            {
                Destroy(gameObject);
                return;
            }
            MovimientoAbeja3D jugador = GetComponent<MovimientoAbeja3D>();
            if (jugador != null) jugador.enabled = false;
        }
    }

    void OnGUI()
    {
        if (!esJugador) return;
        GUIStyle estilo = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 8, 4, 4)
        };
        GUI.Box(new Rect(12, 12, 250, 36), "Abeja: " + vidasActuales + " / " + vidasMaximas + " vidas", estilo);

        VidasAbeja[] abejas = FindObjectsByType<VidasAbeja>(FindObjectsSortMode.None);
        foreach (VidasAbeja abeja in abejas)
        {
            if (!abeja.esJugador)
            {
                GUI.Box(new Rect(12, 54, 250, 36), "Enemiga: " + abeja.vidasActuales + " / " + abeja.vidasMaximas + " vidas", estilo);
                break;
            }
        }
    }
}




