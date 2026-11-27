using ILLGAMES.Unity;
using ILLGAMES.Unity.Component;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ALMMD;

// A temporary view for placement. Do not feed input through the conversation
// controller: its native input/rotation rules still apply with limits disabled.
internal sealed class PointPickerCamera : IDisposable
{
    private readonly RestoreScope restore = new();
    private Camera camera;
    private Vector3 pivot, previousMouse;
    private Quaternion rotation;
    private float distance;
    private bool rotating, panning;
    public bool Dragging => rotating || panning;

    public void Capture(Camera view, CameraControllerEX controller, Behaviour brain,
        Vector3 center, Quaternion angle, float viewDistance, int markerMask)
    {
        camera = view;
        var position = view.transform.position;
        var orientation = view.transform.rotation;
        float fov = view.fieldOfView, near = view.nearClipPlane;
        bool orthographic = view.orthographic;
        int mask = view.cullingMask;
        var cursorLock = Cursor.lockState;
        bool visible = Cursor.visible, nativeLock = GameCursor.IsLock, mouseLock = GameCursor.MouseLocked;
        restore.Add(() =>
        {
            GameCursor.IsLock = nativeLock; GameCursor.MouseLocked = mouseLock;
            Cursor.lockState = cursorLock; Cursor.visible = visible;
        });
        // Restore the view before re-enabling its original writers.
        if (brain)
        {
            bool enabled = brain.enabled;
            restore.Add(() => { if (brain) brain.enabled = enabled; });
            brain.enabled = false;
        }
        if (controller)
        {
            bool enabled = controller.enabled;
            restore.Add(() => { if (controller) controller.enabled = enabled; });
            controller.enabled = false;
        }
        restore.Add(() =>
        {
            if (!view) return;
            view.transform.SetPositionAndRotation(position, orientation);
            view.fieldOfView = fov; view.nearClipPlane = near;
            view.orthographic = orthographic; view.cullingMask = mask;
        });
        pivot = center; rotation = angle; distance = viewDistance;
        previousMouse = Input.mousePosition;
        view.fieldOfView = 45;
        view.nearClipPlane = Mathf.Min(near, 0.05f);
        view.orthographic = false;
        view.cullingMask |= markerMask;
        Apply();
    }

    public void Tick()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 delta = mouse - previousMouse;
        previousMouse = mouse;
        if (!Application.isFocused) { rotating = panning = false; return; }
        bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(1) && !overUI) { rotating = true; delta = Vector3.zero; }
        if (Input.GetMouseButtonDown(2) && !overUI) { panning = true; delta = Vector3.zero; }
        if (!Input.GetMouseButton(1)) rotating = false;
        if (!Input.GetMouseButton(2)) panning = false;
        Move(delta.x, delta.y, rotating, panning, overUI ? 0 : Input.mouseScrollDelta.y, Screen.height);
        Apply();
    }

    // Pitch crosses both poles; yaw can make complete turns. Pan and dolly move
    // the pivot as well as the eye, so the view is never tethered to the actor.
    internal void Move(float dx, float dy, bool orbit, bool pan, float wheel, int screenHeight)
    {
        float pixels = Mathf.Max(1, screenHeight);
        if (orbit)
            rotation = Quaternion.AngleAxis(dx * 180f / pixels, Vector3.up) * rotation *
                Quaternion.AngleAxis(-dy * 180f / pixels, Vector3.right);
        float unitsPerPixel = 2f * distance * Mathf.Tan(22.5f * Mathf.Deg2Rad) / pixels;
        if (pan) pivot -= rotation * new Vector3(dx * unitsPerPixel, dy * unitsPerPixel, 0);
        pivot += rotation * Vector3.forward * (wheel * distance * 0.1f);
    }

    public void Apply()
    {
        if (!camera) return;
        camera.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
        GameCursor.IsLock = false; GameCursor.MouseLocked = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void Dispose()
    {
        camera = null;
        rotating = panning = false;
        restore.Restore();
    }
}
