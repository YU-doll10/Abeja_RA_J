using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class TerrenoYUBeeController : MonoBehaviour
{
    public float speed = 8f;
    public float hoverHeight = 2f;
    public float climbSpeed = 5f;
    public Transform visual;
    public Camera followCamera;
    private CharacterController controller;
    private Vector3 cameraVelocity;
    private float currentHover;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHover = hoverHeight;
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        bool up = false, down = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            input.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                    - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
            input.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0)
                    - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0);
            up = keyboard.spaceKey.isPressed;
            down = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        up = Input.GetKey(KeyCode.Space);
        down = Input.GetKey(KeyCode.LeftShift);
#endif
        input = Vector2.ClampMagnitude(input, 1f);
        currentHover = Mathf.MoveTowards(currentHover, up ? 3.5f : down ? 1.2f : hoverHeight, climbSpeed * Time.deltaTime);
        Vector3 horizontal = new Vector3(input.x, 0, input.y) * speed;
        var terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 next = transform.position + horizontal * Time.deltaTime;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            next.x = Mathf.Clamp(next.x, origin.x + 1, origin.x + size.x - 1);
            next.z = Mathf.Clamp(next.z, origin.z + 1, origin.z + size.z - 1);
            horizontal.x = (next.x - transform.position.x) / Mathf.Max(Time.deltaTime, .0001f);
            horizontal.z = (next.z - transform.position.z) / Mathf.Max(Time.deltaTime, .0001f);
            float ground = terrain.SampleHeight(next) + origin.y;
            horizontal.y = Mathf.Clamp((ground + currentHover - transform.position.y) * 8f, -climbSpeed, climbSpeed);
        }
        controller.Move(horizontal * Time.deltaTime);
        if (visual != null && input.sqrMagnitude > .01f)
            visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(new Vector3(input.x, 0, input.y)), 9 * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (followCamera == null) return;
        Vector3 desired = transform.position + new Vector3(0, 11, -16);
        var terrain = Terrain.activeTerrain;
        if (terrain != null)
            desired.y = Mathf.Max(desired.y, terrain.SampleHeight(desired) + terrain.transform.position.y + 3);
        followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position, desired, ref cameraVelocity, .18f);
        followCamera.transform.LookAt(transform.position + Vector3.up * .5f);
    }
}
