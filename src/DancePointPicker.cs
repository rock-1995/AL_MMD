using AL;
using AL.H;
using ILLGAMES.Unity;
using ILLGAMES.Unity.Component;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ALMMD;

// Reuses the current map's native H-point prefab, without constructing an HScene.
internal sealed class DancePointPicker : IDisposable
{
    private readonly RestoreScope restore = new();
    private readonly Dictionary<int, HPoint> points = new();
    private GameObject pointObject, overlay;
    private HPointCtrl pointCtrl;
    private Camera camera;
    private readonly PointPickerCamera freeCamera = new();
    private int mask;
    private bool disposed, nativeMode, hasCandidate;
    private int openedFrame;
    private readonly SurfacePicker surfaces = new();
    private HPoint candidate;
    private TextMeshProUGUI instructions;
    private Vector3 candidatePosition, markerOffset;
    private float placementBaseY;
    private Quaternion markerRotation;
    private readonly PlacementPreview preview = new();
    private readonly FacingPanel facingPanel = new();
    private readonly PlacementClothes clothes = new();
    public bool Facing { get; private set; }
    private Quaternion candidateRotation;
    public bool Completed { get; private set; }
    public bool Cancelled { get; private set; }
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;

    public void Begin()
    {
        // MapManager inherits the async singleton; the synchronous generic holds a different static instance.
        var map = Manager.MapManager._instance;
        if (!map) throw new InvalidOperationException("No current map for position selection");
        var assets = new Il2CppSystem.Collections.Generic.Dictionary<int, AL.H.AssetBundleInfo>();
        HScene.Resource.LoadHPointInfo("h/list/hpoint/object/", assets);
        int mapId = map.MapID;
        if (!assets.ContainsKey(mapId))
            throw new InvalidOperationException($"Map {mapId} has no native H position markers");
        // AssetBundleInfo is a native struct wrapped as a managed class. This interop version
        // marshals generic out values incorrectly; the indexer returns the correctly boxed struct.
        var info = assets[mapId];
        pointObject = CommonLib.LoadAsset<GameObject>(info.Bundle, info.Asset, true, info.Manifest);
        if (!pointObject) throw new InvalidOperationException("Native position prefab could not be loaded");
        pointCtrl = pointObject.GetComponent<HPointCtrl>();
        if (!pointCtrl) throw new InvalidOperationException("Native position prefab has no HPointCtrl");
        var nativePoints = new Il2CppSystem.Collections.Generic.Dictionary<Collider, HPoint>();
        pointCtrl.GetPointAll(nativePoints);
        var iterator = nativePoints.GetEnumerator();
        while (iterator.MoveNext())
        {
            var pair = iterator.Current;
            if (!pair.Key || !pair.Value) continue;
            points[pair.Key.GetInstanceID()] = pair.Value;
            mask |= 1 << pair.Key.gameObject.layer;
        }
        if (points.Count == 0) throw new InvalidOperationException("Current map has no selectable position markers");
        pointCtrl.HideAll();
        CreateCandidate(assets);
        surfaces.Capture(pointObject.transform);
        openedFrame = Time.frameCount;
        Physics.SyncTransforms();
        CaptureUI();
        CaptureCamera();
        CreateHint();
        clothes.Create(overlay.transform, Plugin.Session.Human, instructions.font);
        Plugin.Logger.LogInfo($"Surface-first position picker: map={mapId}, points={points.Count}, asset={info.Asset}");
    }

    private void CaptureUI()
    {
        DanceUI.Hide(restore);
        foreach (var raycaster in UnityEngine.Object.FindObjectsOfType<GraphicRaycaster>())
        {
            var saved = raycaster; bool enabled = raycaster.enabled;
            restore.Add(() => { if (saved) saved.enabled = enabled; });
            raycaster.enabled = false;
        }
        var events = EventSystem.current;
        if (events)
        {
            bool navigation = events.sendNavigationEvents;
            var selected = events.currentSelectedGameObject;
            restore.Add(() => { if (events) { events.sendNavigationEvents = navigation; events.SetSelectedGameObject(selected); } });
            events.sendNavigationEvents = false;
            events.SetSelectedGameObject(null);
        }
    }

    private void CaptureCamera()
    {
        var manager = SingletonInitializer<ActionMapCameraManager>._instance;
        camera = manager ? manager.NowCamera : Camera.main;
        var controller = manager ? manager._camCtrl : camera?.GetComponent<CameraControllerEX>();
        if (!camera || !controller) throw new InvalidOperationException("No native map camera controller");
        Vector3 direction = controller.CameraDir;
        Quaternion rot = camera.transform.rotation;
        // The daily-life camera may hide layers used only by the H scene's markers.
        int markerMask = mask;
        foreach (var renderer in pointObject.GetComponentsInChildren<Renderer>(true))
            markerMask |= 1 << renderer.gameObject.layer;
        Transform actorRoot = Plugin.Session.Human.Transform;
        Vector3 actorPosition = actorRoot.position;
        float height = 0;
        foreach (var bone in actorRoot.GetComponentsInChildren<Transform>(true))
            if (bone.name == "cf_j_head") { height = Vector3.Distance(bone.position, actorPosition); break; }
        // AL uses larger world units than metres; frame the markers relative to the actual actor.
        float viewDistance = height > 0 ? height * 3f : Mathf.Abs(direction.z);
        if (viewDistance <= 0) viewDistance = 6;
        freeCamera.Capture(camera, controller, manager ? manager._cineBrain : null,
            actorPosition + Vector3.up * (viewDistance * 0.1f),
            Quaternion.Euler(30, rot.eulerAngles.y, 0), viewDistance, markerMask);
    }

    private void CreateHint()
    {
        TMP_FontAsset font = null;
        foreach (var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
            if (text.font) { font = text.font; break; }
        overlay = new GameObject("AL MMD Position Picker", new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        var canvas = overlay.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
        var scaler = overlay.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        overlay.AddComponent<GraphicRaycaster>();
        var panel = UIObject("Position instructions", overlay.transform, new Vector2(1400, 96), new Vector2(-230, -20));
        panel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.88f);
        var label = UIObject("Instructions", panel.transform, new Vector2(1200, 96), new Vector2(-80, 0));
        var textComponent = label.AddComponent<TextMeshProUGUI>();
        if (font) textComponent.font = font;
        instructions = textComponent;
        instructions.enableAutoSizing = true; instructions.fontSizeMin = 18; instructions.fontSizeMax = 24;
        UpdateHint();
        textComponent.fontSize = 24; textComponent.alignment = TextAlignmentOptions.Center; textComponent.raycastTarget = false;
        var back = UIObject("Return", panel.transform, new Vector2(150, 44), new Vector2(610, -24));
        var image = back.AddComponent<Image>(); image.color = new Color(0.22f, 0.28f, 0.36f, 1);
        var button = back.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)Cancel);
        var buttonLabel = UIObject("Label", back.transform, new Vector2(150, 44), Vector2.zero);
        var buttonText = buttonLabel.AddComponent<TextMeshProUGUI>();
        if (font) buttonText.font = font;
        buttonText.text = "Back"; buttonText.fontSize = 24; buttonText.alignment = TextAlignmentOptions.Center; buttonText.raycastTarget = false;
    }

    private static GameObject UIObject(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name, new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return obj;
    }

    private void CreateCandidate(Il2CppSystem.Collections.Generic.Dictionary<int, AL.H.AssetBundleInfo> assets)
    {
        HPoint template = points.Values.FirstOrDefault(p => p.PlaceType == PlaceType.BeachChair);
        GameObject extra = null;
        try
        {
            // The game's BeachChair marker uses sp_al_tex_Hpoint_04_Bed.
            // Indoor maps have no such point, so borrow the same native visual from
            // a map that has it; never change that map or move its original points.
            if (!template)
            {
                var iterator = assets.GetEnumerator();
                while (iterator.MoveNext())
                {
                    var info = iterator.Current.Value;
                    extra = CommonLib.LoadAsset<GameObject>(info.Bundle, info.Asset, true, info.Manifest);
                    if (!extra) continue;
                    template = extra.GetComponentsInChildren<HPoint>(true).FirstOrDefault(p => p.PlaceType == PlaceType.BeachChair);
                    if (template) break;
                    extra.SetActive(false); UnityEngine.Object.Destroy(extra); extra = null;
                }
            }
            if (!template) throw new InvalidOperationException("Native bed marker was not found");
            candidate = UnityEngine.Object.Instantiate(template.gameObject, pointObject.transform).GetComponent<HPoint>();
            candidate.gameObject.name = "AL MMD surface bed marker";
            // Normalize the visual around its native anchor before assigning a new point.
            markerOffset = Quaternion.Inverse(template.Rotation) * (template.transform.position - template.Position);
            markerRotation = Quaternion.Inverse(template.Rotation) * template.transform.rotation;

            candidate.Active = false;
        }
        finally { if (extra) { extra.SetActive(false); UnityEngine.Object.Destroy(extra); } }
    }

    private void UpdateHint()
    {
        if (!instructions) return;
        string key = DanceSettings.Label(DanceSettings.ToggleKey);
        string action = Facing ? $"Set facing and height: sliders, +/- or enter values | {key}: start dance"
            : nativeMode ? $"Native positions: click a marker | {key}: back"
            : hasCandidate ? $"Click bed marker: set facing | Click surface to move it | {key}: clear marker"
            : $"Click a surface to place a bed marker | {key}: native positions";
        instructions.text = action + "\nRMB: rotate | MMB: pan | Wheel: move";
    }

    public void Back()
    {
        if (disposed || Completed) return;
        if (clothes.Reloading) return;
        if (Facing) { facingPanel.Commit(); Completed = true; return; }
        if (nativeMode) { Cancel(); return; }
        if (hasCandidate)
        {
            hasCandidate = false; candidate.Active = false;
        }
        else
        {
            nativeMode = true; pointCtrl.VisibleAll(); Physics.SyncTransforms();
            Plugin.Logger.LogInfo("Position picker switched to native H points.");
        }
        UpdateHint();
    }

    private void SetCandidate(Vector3 position)
    {
        candidatePosition = position;
        candidateRotation = Quaternion.Euler(0, camera.transform.eulerAngles.y + 180, 0);
        // These native marker prefabs use their own transform as the placement anchor.
        candidate.Active = false;
        candidate.transform.SetPositionAndRotation(position + candidateRotation * markerOffset, candidateRotation * markerRotation); // Clear world-space particles before moving the effect.
        candidate.Active = true;
        hasCandidate = true; Physics.SyncTransforms(); UpdateHint();
        Plugin.Logger.LogInfo($"Surface candidate: {position.ToString("F3")}");
    }

    public void Tick()
    {
        if (Completed || disposed) return;
        clothes.Tick();
        freeCamera.Tick();
        if (Facing) { preview.Apply(); return; }
        if (!Application.isFocused || freeCamera.Dragging || Time.frameCount <= openedFrame) return;
        if (!Input.GetMouseButtonDown(0) || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
        Pick(camera.ScreenPointToRay(Input.mousePosition));
    }

    internal void Pick(Ray ray)
    {
        if (Facing || Completed || disposed || clothes.Reloading) return;
        if (!nativeMode)
        {
            bool hitSurface = surfaces.TryPick(ray, camera.transform.position, camera.farClipPlane, out var point);
            if (hasCandidate && candidate.Colider.Raycast(ray, out var markerHit, camera.farClipPlane) &&
                (!hitSurface || markerHit.distance <= Vector3.Distance(ray.origin, point)))
            {
                EnterFacing(candidatePosition, candidateRotation);
            }
            else if (hitSurface) SetCandidate(point);
            return;
        }
        HPoint selected = null; float distance = float.MaxValue;
        foreach (var hit in Physics.RaycastAll(ray, camera.farClipPlane, mask, QueryTriggerInteraction.Collide))
        {
            var collider = hit.collider;
            if (collider && hit.distance < distance && points.TryGetValue(collider.GetInstanceID(), out var point))
            { selected = point; distance = hit.distance; }
        }
        if (!selected) return;
        EnterFacing(selected.Position, selected.Rotation);
        Plugin.Logger.LogInfo($"Selected native position: type={selected.PlaceType}, id={selected.ID}, position={Position.ToString("F3")}");
    }

    private void EnterFacing(Vector3 position, Quaternion rotation)
    {
        Position = position; Rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
        placementBaseY = position.y;
        pointCtrl.HideAll(); candidate.Active = false; hasCandidate = false;
        surfaces.Dispose();
        preview.Begin(Plugin.Session.Actor, Plugin.Session.Human, Position, Rotation);
        Facing = true;
        facingPanel.Create(overlay.transform, instructions.font, Rotation.eulerAngles.y, SetFacing, SetHeight);
        UpdateHint();
    }

    private void SetFacing(float angle)
    {
        if (!Facing || disposed || Completed) return;
        Rotation = Quaternion.Euler(0, angle, 0);
        preview.SetPlacement(Position, Rotation);
    }

    private void SetHeight(float offset)
    {
        if (!Facing || disposed || Completed) return;
        Position = new Vector3(Position.x, placementBaseY + offset, Position.z);
        preview.SetPlacement(Position, Rotation);
    }

    private void Cancel()
    {
        if (clothes.Reloading) return;
        Cancelled = true; Completed = true;
    }
    public void BeforeRender()
    {
        if (disposed || Completed) return;
        freeCamera.Apply();
        if (Facing) preview.Apply();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        facingPanel.Dispose();
        clothes.Dispose();
        preview.Dispose();
        if (overlay) { overlay.SetActive(false); UnityEngine.Object.Destroy(overlay); }
        surfaces.Dispose();
        if (pointObject) { pointObject.SetActive(false); UnityEngine.Object.Destroy(pointObject); }
        freeCamera.Dispose();
        restore.Restore();
        points.Clear(); camera = null; pointCtrl = null; candidate = null; instructions = null;
    }
}
