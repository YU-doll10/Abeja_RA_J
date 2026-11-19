using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float velocidad = 25f;
    public float velocidadRotacion = 10f;

    void Update()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y = 1;

            if (Keyboard.current.sKey.isPressed)
                input.y = -1;

            if (Keyboard.current.aKey.isPressed)
                input.x = -1;

            if (Keyboard.current.dKey.isPressed)
                input.x = 1;
        }

        Vector3 movimiento = new Vector3(input.x, 0, input.y);

        if (movimiento.magnitude > 0)
        {
            // Movimiento
            transform.Translate(
                movimiento.normalized * velocidad * Time.deltaTime,
                Space.World
            );

            // Rotación 
            Quaternion rotacionObjetivo = Quaternion.LookRotation(movimiento);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                velocidadRotacion * Time.deltaTime
            );
        }
    }
}