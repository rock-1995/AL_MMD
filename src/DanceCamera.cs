using AL;
using ILLGAMES.Unity;
using ILLGAMES.Unity.Component;
using UnityEngine;

namespace ALMMD;

internal sealed class DanceCamera
{
    private readonly RestoreScope state = new();
    private readonly PointPickerCamera freeCamera = new();
    private Camera camera;
    private CameraControllerEX controller;
    private readonly Il2CppSystem.Func<bool> blockInput = (Func<bool>)(() => true);
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Transform Target => camera ? camera.transform : null;
    public bool IsFree { get; private set; }

    public void CaptureFree(ActionMapCameraManager manager, Vector3 waist, Quaternion facing, float distance)
    {
        camera = manager ? manager.NowCamera : Camera.main;
        if (!camera) throw new InvalidOperationException("No map camera");
        controller = manager ? manager._camCtrl : camera.GetComponent<CameraControllerEX>();
        IsFree = true;
        freeCamera.Capture(camera, controller, manager ? manager._cineBrain : null,
            waist, facing * Quaternion.Euler(0, 180, 0), distance, 0);
    }

    public void Tick() { if (IsFree) freeCamera.Tick(); }
    public void ApplyFree() => freeCamera.Apply();

    public void Capture(ActionMapCameraManager manager)
    {
        CursorLockMode lockMode = Cursor.lockState;
        bool visible = Cursor.visible, nativeLock = GameCursor.IsLock, mouseLocked = GameCursor.MouseLocked;
        state.Add(() =>
        {
            GameCursor.IsLock = nativeLock; GameCursor.MouseLocked = mouseLocked;
            Cursor.lockState = lockMode; Cursor.visible = visible;
        });
        camera = manager ? manager.NowCamera : Camera.main;
        if (!camera) throw new InvalidOperationException("No map camera");
        var saved = camera;
        var t = saved.transform;
        Vector3 position = t.position;
        Quaternion rotation = t.rotation;
        float fov = saved.fieldOfView, near = saved.nearClipPlane, orthoSize = saved.orthographicSize;
        bool ortho = saved.orthographic;
        state.Add(() =>
        {
            if (!saved) return;
            t.SetPositionAndRotation(position, rotation);
            saved.fieldOfView = fov; saved.nearClipPlane = near;
            saved.orthographic = ortho; saved.orthographicSize = orthoSize;
        });
        camera.nearClipPlane = Mathf.Min(near, 0.03f);
        var brain = manager ? manager._cineBrain : null;
        if (brain)
        {
            bool enabled = brain.enabled;
            state.Add(() => { if (brain) brain.enabled = enabled; });
            brain.enabled = false;
        }
        controller = manager ? manager._camCtrl : camera.GetComponent<CameraControllerEX>();
        if (controller)
        {
            var c = controller;
            Vector3 pivot = c.CameraPos, direction = c.CameraDir, angles = c.CameraRot;
            var target = c._targetTrans;
            var noCtrl = c.NoCtrlCondition;
            bool locked = c.IsCursorLock;
            state.Add(() =>
            {
                if (!c) return;
                c.NoCtrlCondition = noCtrl; c.IsCursorLock = locked;
                c.CameraPos = pivot; c.CameraDir = direction; c.CameraRot = angles; c._targetTrans = target;
            });
            c.NoCtrlCondition = blockInput;
            c.IsCursorLock = false;
            c.SetCtrlNow(false);
            c.ResetIsControlNow();
            if ((GameCursor.IsLock || GameCursor.MouseLocked) && c._gameCursor)
            {
                c._gameCursor.SetCursorLock(false);
                c._gameCursor.UnLockCursor();
            }
        }
        GameCursor.IsLock = false;
        GameCursor.MouseLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    public void Apply(CameraPose pose, Vector3 origin, Quaternion stageRotation, float scale)
    {
        if (!camera) return;
        var p = pose.Position;
        var r = pose.RotationRadians;
        var raw = Quaternion.Euler(-r.X * Mathf.Rad2Deg, -r.Y * Mathf.Rad2Deg, -r.Z * Mathf.Rad2Deg);
        var localRotation = Quaternion.Euler(0, 180, 0) * raw;
        Rotation = stageRotation * localRotation;
        // Scale the complete MMD camera about the stage origin: interest point
        // and orbit distance share the same calibrated conversion scale.
        Vector3 localPosition = (new Vector3(-p.X, p.Y, -p.Z) +
            localRotation * (Vector3.forward * pose.Distance)) * scale;
        Position = origin + stageRotation * localPosition;
        if (controller)
        {
            controller._targetTrans = null;
            controller.CameraPos = Position;
            controller.CameraDir = Vector3.zero;
            var basis = controller._transBase;
            controller.CameraRot = (basis ? Quaternion.Inverse(basis.rotation) * Rotation : Rotation).eulerAngles;
        }
        camera.transform.SetPositionAndRotation(Position, Rotation);
        camera.orthographic = !pose.Perspective;
        camera.fieldOfView = pose.FieldOfView;
        if (!pose.Perspective)
            camera.orthographicSize = Mathf.Abs(pose.Distance * scale) * Mathf.Tan(pose.FieldOfView * Mathf.Deg2Rad * 0.5f);
    }

    public void Restore()
    {
        freeCamera.Dispose();
        state.Restore();
        IsFree = false; camera = null; controller = null;
    }
}
