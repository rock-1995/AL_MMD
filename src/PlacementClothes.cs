using AL.H;
using AL.H.UI;
using AL.H.UI.ClothesSettingMenu;
using Character;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ALMMD;

// Clone only the native right-top UI, never the HScene GameObject. Native menu
// controllers receive a minimal actor view and retain their own list/state UI.
internal sealed class PlacementClothes : IDisposable
{
    private GameObject root;
    // Keep these assets only while a placement menu owns them. Unity destroys
    // cloned UI at the frame boundary, so its bundle must outlive that boundary.
    private static AssetBundle cachedBundle;
    private static GameObject cachedPrefab;
    private static int assetUsers;
    private bool ownsAssets;
    private Transform originalRoot;
    private Human human;
    private DanceRoster roster;
    private bool[] accessoryStates;
    private HActor actor;
    private ClothesSetting source;
    private CanvasGroup container;
    private SelectClothesState clothes;
    private SelectAccessoryState accessories;
    private SelectCoodinateCard cards;
    private Texture cardPreview;
    private int open = -1, originalCoordinate;
    private bool statesInitialized, accessoriesInitialized, cardsInitialized, refreshAfterReload;
    public bool Reloading => roster != null ? roster.Reloading : human != null && human.IsReloading;
    private IEnumerable<Human> Targets => roster != null ? roster.UniqueSources().Select(c => c.Human) : new[] { human };

    public void Create(Transform parent, Human selected, TMP_FontAsset font)
    {
        human = selected; originalCoordinate = human.FileStatus.coordinateType;
        roster = Plugin.Session.Roster;
        accessoryStates = human.FileStatus.showAccessory.ToArray();
        if (!cachedBundle)
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "AL_MMD.NativeClothes.unity3d");
            cachedBundle = AssetBundle.LoadFromFile(path);
        }
        if (cachedBundle && !ownsAssets) { assetUsers++; ownsAssets = true; }
        if (!cachedPrefab && cachedBundle)
            cachedPrefab = cachedBundle.LoadAsset<GameObject>("assets/illgames/assetbundle/h/scene/hscene.prefab");
        var prefab = cachedPrefab;
        if (!prefab) throw new InvalidOperationException("Could not load the game's outfit menu.");
        // Unity's UnloadUnusedAssets does not see this CoreCLR static field.
        // Protect the source only while the menu is open; ReleaseAssets removes it.
        prefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        var option = prefab.GetComponent<HScene>()._option;
        source = option._clothesSetting;
        originalRoot = prefab.transform.Find("Canvas/ccr_dropShadow/Option/RightTopArea");
        root = UnityEngine.Object.Instantiate(originalRoot.gameObject, parent, false);
        root.name = "AL MMD native clothes";
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = font ? font : TMP_Settings.defaultFontAsset;
        container = Map(source._canvasGroup); container.gameObject.SetActive(true);
        foreach (var row in source._toggleCharas) Map(row._objectRoot).SetActive(false);
        Map(source._txtCharaSelect).text = human.Name;
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(HActor).TypeHandle);
        actor = new HActor(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<HActor>.NativeClassPtr)) {
            Human = human, Data = human.Data, Sex = human.Sex, Index = 0, Actor = Plugin.Session.Actor
        };
        clothes = new SelectClothesState { _canvasGroup = Map(source._selectClothesState._canvasGroup),
            _buttonAllChange = Clone(source._selectClothesState._buttonAllChange),
            _buttons = new Il2CppReferenceArray<Option.Selected>(source._selectClothesState._buttons.Select(Clone).ToArray()) };
        accessories = new SelectAccessoryState { _canvasGroup = Map(source._selectAccessoryState._canvasGroup),
            _toggleAll = Clone(source._selectAccessoryState._toggleAll), _view = Map(source._selectAccessoryState._view),
            _prefab = Map(source._selectAccessoryState._prefab), _sprites = source._selectAccessoryState._sprites,
            _colorsText = source._selectAccessoryState._colorsText };
        var card = source._selectCoodinateCard;
        cards = new SelectCoodinateCard { _canvasGroup = Map(card._canvasGroup),
            _sortKind = Clone(card._sortKind), _sortOrder = Clone(card._sortOrder),
            _view = Map(card._view), _scrollRect = Map(card._scrollRect), _clone = Map(card._clone),
            _emptyNode = Map(card._emptyNode), _cardImage = Map(card._cardImage), _cardImageDef = card._cardImageDef,
            _togglesParts = new Il2CppReferenceArray<Toggle>(card._togglesParts.Select(Map).ToArray()),
            _decide = Map(card._decide), _beforeCoode = Map(card._beforeCoode) };
        Bind(option._buttonClothes, 0); Bind(option._buttonAcses, 1); Bind(option._buttonClothCards, 2);
        Close();
        root.SetActive(true);
    }
    private T Map<T>(T original) where T : UnityEngine.Object
    {
        if (!original) return null;
        var go = original.TryCast<GameObject>(); var component = original.TryCast<Component>();
        var transform = go ? go.transform : component ? component.transform : null;
        if (!transform) return original;
        var path = new List<string>();
        while (transform && transform != originalRoot) { path.Add(transform.name); transform = transform.parent; }
        if (!transform) return original; // Shared sprite/texture or an external list prefab.
        path.Reverse(); var mapped = path.Count == 0 ? root.transform : root.transform.Find(string.Join("/", path));
        if (!mapped) throw new InvalidOperationException("The game's outfit menu is missing " + string.Join("/", path));
        return go ? mapped.gameObject.Cast<T>() : mapped.GetComponent(component.GetIl2CppType()).Cast<T>();
    }
    private Option.Selected Clone(Option.Selected value) => new() {
        _selectable = Map(value._selectable), _sprites = value._sprites, _spriteStates = value._spriteStates
    };
    private SelectCoodinateCard.SortButton Clone(SelectCoodinateCard.SortButton value) => new() {
        _button = Map(value._button), _spritesNormal = value._spritesNormal, _spritesHighlighted = value._spritesHighlighted
    };
    private void Bind(Option.Selected original, int index)
    {
        var selected = Map(original._selectable); selected.gameObject.SetActive(true); selected.interactable = true;
        var button = selected.GetComponent<Button>(); button.onClick.RemoveAllListeners();
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)(() => Toggle(index)));
    }
    private void EnsureStates()
    {
        if (statesInitialized) return;
        // Native Initialize dereferences HScene.Instance BEFORE AddEndable.
        // Placement has no HScene: bind its existing callbacks to our clone
        // directly, retaining native clothes rules, sprites and list handling.
        for (int i = 0; i < clothes._buttons.Length; i++)
        {
            var kind = (HumanCloth.Define.ClothesKind)i;
            BindClick(clothes._buttons[i]._selectable.GetComponent<Button>(),
                () => { clothes._Initialize_b__17_1(default, kind); SyncClothes(); });
        }
        BindClick(clothes._buttonAllChange._selectable.GetComponent<Button>(),
            () => { SelectClothesState.__c.__9._Initialize_b__17_0(default, clothes); SyncClothes(); });
        clothes.SetActor(actor); statesInitialized = true;
    }
    private void EnsureAccessories()
    {
        if (accessoriesInitialized) return;
        var toggle = accessories._toggleAll._selectable.GetComponent<Toggle>();
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)(Action<bool>)(value => {
            if (Reloading) return;
            try { SelectAccessoryState.__c.__9._Initialize_b__15_0(value, accessories); SyncAccessories(); }
            catch (Exception ex) { Plugin.LogError("Using the accessories menu", ex); }
        }));
        accessories.SetActor(actor); accessoriesInitialized = true;
    }
    private void EnsureCards()
    {
        if (cardsInitialized) return;
        // Equivalent menu setup without HScene registration or its reload
        // callbacks. The native virtualized card list still owns selection.
        cards._selectClothesState = clothes;
        cards._sortKind.SetMode(1); cards._sortOrder.SetMode(1);
        cards.coordinateInfos[0].Load(0); cards.coordinateInfos[1].Load(1);
        foreach (var toggle in cards._togglesParts)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)(Action<bool>)(_ => cards.InteractableDecide()));
        }
        BindClick(cards._sortKind._button,
            () => SelectCoodinateCard.__c.__9._Initialize_b__29_3(default, cards));
        BindClick(cards._sortOrder._button,
            () => SelectCoodinateCard.__c.__9._Initialize_b__29_4(default, cards));
        BindClick(cards._decide, LoadCard);
        BindClick(cards._beforeCoode, RestoreCoordinate);
        cards.SetActor(actor);
        // The native selection callback allocates a preview but leaves the
        // previous texture to scene-wide cleanup. Retire it on selection, not
        // via a per-frame scan. Register after the native UI callback.
        cards.Disposables.Add(R3.ObservableSubscribeExtensions.Subscribe<int>(cards._nowSelect,
            (Il2CppSystem.Action<int>)(Action<int>)(_ => UpdateCardPreview())));
        cards.InteractableDecide();
        cardsInitialized = true;
    }
    private void UpdateCardPreview()
    {
        var next = cards._cardImage.texture;
        if (cardPreview && cardPreview != next) UnityEngine.Object.Destroy(cardPreview);
        cardPreview = next != cards._cardImageDef ? next : null;
    }
    private void BindClick(Button button, Action action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)(() => {
            if (Reloading) return;
            try { action(); }
            catch (Exception ex) { Plugin.LogError("Using the outfit menu", ex); }
        }));
    }
    private void Toggle(int index)
    {
        if (Reloading) return;
        if (open == index) { Close(); return; }
        try
        {
            Visible(container, true);
            // CanvasGroup alpha alone leaves the native scrolling lists and
            // text behaviours running. Disable the two unused subtrees too.
            Show(clothes._canvasGroup, index == 0);
            Show(accessories._canvasGroup, index == 1);
            Show(cards._canvasGroup, index == 2);
            if (index != 1) EnsureStates();
            if (index == 1) EnsureAccessories();
            if (index == 2) EnsureCards();
            open = index;
        }
        catch (Exception ex) { Close(); Plugin.LogError("Opening the outfit menu", ex); }
    }
    private void LoadCard()
    {
        if (Reloading || string.IsNullOrEmpty(cards._filename)) return;
        try
        {
            var flags = HumanDataCoordinate.LoadLimited.Flags.None;
            if (cards._togglesParts[0].isOn) flags |= HumanDataCoordinate.LoadLimited.Flags.Hair | HumanDataCoordinate.LoadLimited.Flags.FaceMakeup | HumanDataCoordinate.LoadLimited.Flags.BodyMakeup;
            if (cards._togglesParts[1].isOn) flags |= HumanDataCoordinate.LoadLimited.Flags.Clothes;
            if (cards._togglesParts[2].isOn) flags |= HumanDataCoordinate.LoadLimited.Flags.Accessory;
            if (!human.Coorde.Now.LoadFileLimited(cards._filename, new Il2CppSystem.Nullable<byte>(human.Sex), flags)) return;
            foreach (var target in Targets)
            {
                if (target.Pointer == human.Pointer) continue;
                HumanDataCoordinate.CopyLimited(false, target.Coorde.Now, human.Coorde.Now, flags);
                target.ReloadCoordinate();
            }
            human.ReloadCoordinate(); refreshAfterReload = true;
        }
        catch (Exception ex) { Plugin.LogError("Applying the outfit card", ex); }
    }
    private void RestoreCoordinate()
    {
        if (Reloading) return;
        try
        {
            if (roster != null)
                foreach (var target in roster.UniqueSources())
                    target.Human.Coorde.ChangeCoordinateTypeAndReload((HumanCoordinate.Define.CoordinateType)target.OriginalCoordinate, true);
            else human.Coorde.ChangeCoordinateTypeAndReload((HumanCoordinate.Define.CoordinateType)originalCoordinate, true);
            refreshAfterReload = true;
        }
        catch (Exception ex) { Plugin.LogError("Restoring the original outfit", ex); }
    }
    public void Tick()
    {
        if (Reloading) return;
        SyncAccessories(); // Native per-accessory rows own their callbacks.
        if (!refreshAfterReload) return;
        refreshAfterReload = false;
        SyncClothes();
        if (statesInitialized) clothes.UpdateUI();
        if (accessoriesInitialized) accessories.UpdateUI();
        if (cardsInitialized) cards.UpdateUI();
    }
    private void SyncClothes()
    {
        foreach (var target in Targets)
            if (target.Pointer != human.Pointer) CopyClothesStates(human, target);
    }
    private void SyncAccessories()
    {
        var states = human.FileStatus.showAccessory;
        for (int i = 0; i < states.Length; i++)
        {
            if (i < accessoryStates.Length && accessoryStates[i] == states[i]) continue;
            foreach (var target in Targets)
                if (target.Pointer != human.Pointer && i < target.FileStatus.showAccessory.Length)
                    target.Acs.SetAccessoryState(i, states[i]);
        }
        if (accessoryStates.Length != states.Length) accessoryStates = new bool[states.Length];
        for (int i = 0; i < states.Length; i++) accessoryStates[i] = states[i];
    }
    private static void CopyClothesStates(Human source, Human target)
    {
        var states = source.FileStatus.clothesState;
        for (int i = 0; i < Math.Min(states.Length, target.FileStatus.clothesState.Length); i++)
            target.Cloth.SetClothesState((HumanCloth.Define.ClothesKind)i, states[i], false);
    }
    internal static void CopyStates(Human source, Human target)
    {
        CopyClothesStates(source, target);
        var states = source.FileStatus.showAccessory;
        for (int i = 0; i < Math.Min(states.Length, target.FileStatus.showAccessory.Length); i++)
            target.Acs.SetAccessoryState(i, states[i]);
    }
    private static void Visible(CanvasGroup group, bool visible)
    { group.alpha = visible ? 1f : 0f; group.interactable = visible; group.blocksRaycasts = visible; }
    private static void Show(CanvasGroup group, bool visible)
    { Visible(group, visible); group.gameObject.SetActive(visible); }
    public void Close()
    {
        open = -1;
        if (container) Visible(container, false);
        if (clothes != null) Show(clothes._canvasGroup, false);
        if (accessories != null) Show(accessories._canvasGroup, false);
        if (cards != null) Show(cards._canvasGroup, false);
    }
    public void Dispose()
    {
        if (root) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
        // The native card menu creates runtime preview textures. Its
        // CompositeDisposable releases subscriptions, not this Unity asset.
        if (cards != null && cards._cardImage)
        {
            var previewTexture = cards._cardImage.texture;
            if (previewTexture && previewTexture != cards._cardImageDef)
                UnityEngine.Object.Destroy(previewTexture);
        }
        clothes?.Dispose();
        accessories?.Dispose();
        cards?.Dispose();
        root = null; originalRoot = null; source = null; container = null;
        clothes = null; accessories = null; cards = null; actor = null; human = null;
        cardPreview = null;
        roster = null; accessoryStates = null;
        if (ownsAssets)
        {
            ownsAssets = false;
            if (--assetUsers == 0) Plugin.ReleasePlacementAssetsAfterFrame();
        }
    }

    internal static System.Collections.IEnumerator ReleaseAfterFrame(MonoBehaviour destroyAfter = null)
    {
        yield return null;
        // A new picker can open before the queued release runs. Its final
        // Dispose will queue another release instead of unloading live UI.
        if (assetUsers == 0) ReleaseAssets();
        if (destroyAfter) UnityEngine.Object.Destroy(destroyAfter);
    }

    internal static void ReleaseAssets()
    {
        if (cachedPrefab) cachedPrefab.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
        // UI destruction has completed: release both the bundle and its assets.
        if (cachedBundle) cachedBundle.Unload(true);
        cachedBundle = null; cachedPrefab = null;
    }
}
