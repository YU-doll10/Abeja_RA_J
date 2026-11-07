using UnityEngine;

public class FlorRecolectable : MonoBehaviour
{
    private bool recogida;

    void OnTriggerEnter(Collider otro)
    {
        if (recogida) return;
        MovimientoAbeja3D abeja = otro.GetComponentInParent<MovimientoAbeja3D>();
        if (abeja == null || !abeja.controlJugador) return;

        recogida = true;
        Destroy(gameObject);
    }
}
