using UnityEngine;

public class FlorRecolectable : MonoBehaviour
{
    private static AudioClip sonidoRecoleccion;
    private bool recogida;

    void OnTriggerEnter(Collider otro)
    {
        if (recogida) return;
        MovimientoAbeja3D abeja = otro.GetComponentInParent<MovimientoAbeja3D>();
        if (abeja == null || !abeja.controlJugador) return;

        recogida = true;
        ReproducirSonidoRecoleccion();
        Destroy(gameObject);
    }

    private void ReproducirSonidoRecoleccion()
    {
        if (sonidoRecoleccion == null)
            sonidoRecoleccion = Resources.Load<AudioClip>("Audio/1campana");

        if (sonidoRecoleccion == null)
        {
            Debug.LogWarning("No se encontró el sonido de recolección en Resources/Audio/1campana.mp3.", this);
            return;
        }

        AudioListener oyente = FindFirstObjectByType<AudioListener>();
        AudioSource reproductor = oyente != null ? oyente.GetComponent<AudioSource>() : null;
        if (reproductor == null && oyente != null)
            reproductor = oyente.gameObject.AddComponent<AudioSource>();

        if (reproductor != null)
        {
            reproductor.playOnAwake = false;
            reproductor.loop = false;
            reproductor.mute = false;
            reproductor.volume = 1f;
            reproductor.pitch = 1f;
            reproductor.spatialBlend = 0f;
            reproductor.bypassEffects = true;
            reproductor.bypassListenerEffects = true;
            reproductor.bypassReverbZones = true;
            reproductor.PlayOneShot(sonidoRecoleccion, 1.4f);
        }
        else
        {
            AudioSource.PlayClipAtPoint(sonidoRecoleccion,
                oyente != null ? oyente.transform.position : transform.position, 1.4f);
        }
    }
}
