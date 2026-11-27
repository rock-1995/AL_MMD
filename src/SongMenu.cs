using System.Globalization;
using System.Text;
using UnityEngine;

namespace ALMMD;

internal sealed partial class SongMenu : IDisposable
{
    private GUIStyle title, row, hint, option;
    private Font font;
    private SongCatalog.Song pending;
    private readonly Dictionary<string, string> wrapped = new();
    private float cachedWidth = -1, rowHeight;
    private int cachedCount = -1;
    public void Reset() { pending = null; cachedWidth = -1; wrapped.Clear(); choosingSlot = -1; }
    public SongCatalog.Song TakeSelection() { var result = pending; pending = null; return result; }

    private void Styles(GUISkin skin = null)
    {
        if (title != null) return;
        if (!skin) skin = GUI.skin;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Yu Gothic", "Arial" }, 20);
        title = new GUIStyle { font = font, fontSize = 22, alignment = TextAnchor.MiddleLeft, richText = false };
        row = new GUIStyle { font = font, fontSize = 20, alignment = TextAnchor.MiddleLeft,
            wordWrap = true, richText = false };
        hint = new GUIStyle { font = font, fontSize = 17, alignment = TextAnchor.MiddleLeft, richText = false };
        title.normal.textColor = row.normal.textColor = hint.normal.textColor = Color.white;
        row.hover.textColor = row.active.textColor = Color.white;
        row.normal.background = skin.button.normal.background;
        row.hover.background = skin.button.hover.background;
        row.active.background = skin.button.active.background;
        row.padding.left = row.padding.right = 16; row.padding.top = row.padding.bottom = 10;
        row.border.left = row.border.right = row.border.top = row.border.bottom = 4;
        option = new GUIStyle { font = font, fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = false };
        option.normal.textColor = option.hover.textColor = option.active.textColor = Color.white;
        option.normal.background = skin.button.normal.background;
        option.hover.background = skin.button.hover.background;
        option.active.background = skin.button.active.background;
        option.padding.top = option.padding.bottom = 1;
        option.padding.left = option.padding.right = 6;
    }

    // Explicit wrapping also handles names without spaces, including CJK and long filenames.
    private string Wrap(string value, float width)
    {
        var result = new StringBuilder(); var line = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (element == "\r") continue;
            if (element == "\n") { result.Append(line).Append('\n'); line.Clear(); continue; }
            if (line.Length > 0 && row.CalcSize(new GUIContent(line.ToString() + element)).x > width)
            { result.Append(line).Append('\n'); line.Clear(); }
            line.Append(element);
        }
        return result.Append(line).ToString();
    }

    internal sealed class Layout
    {
        public float Width, ButtonWidth, RowHeight, Height, X, Y, SongTop;
        public int Columns;
        public Rect SongRect(int i) => new Rect(X + 16 + (i % Columns) * (ButtonWidth + 10),
            Y + SongTop + (i / Columns) * (RowHeight + 10), ButtonWidth, RowHeight);
    }

    internal Layout PrepareLayout(SongCatalog catalog, int screenWidth, int screenHeight, GUISkin skin)
    {
        Styles(skin);
        float width = Mathf.Min(1280, screenWidth - 32);
        int columns = Math.Max(1, Math.Min(3, (int)(width / 400)));
        float buttonWidth = (width - 32 - (columns - 1) * 10) / columns;
        float available = Mathf.Max(60, screenHeight - 164 - OptionsHeight);
        if (cachedWidth != buttonWidth || cachedCount != catalog.Count)
        {
            cachedWidth = buttonWidth; cachedCount = catalog.Count; wrapped.Clear(); rowHeight = 60;
            foreach (var song in catalog.All)
            {
                string text = Wrap(song.Name, buttonWidth - 4);
                wrapped[song.Name] = text;
                rowHeight = Mathf.Max(rowHeight, row.CalcHeight(new GUIContent(text), buttonWidth) + 4);
            }
        }
        int rows = Math.Max(1, (int)((available + 10) / (rowHeight + 10)));
        catalog.FitPage(rows * columns);
        int visibleRows = Math.Max(1, (catalog.VisibleCount + columns - 1) / columns);
        float height = visibleRows * (rowHeight + 10) + 112 + OptionsHeight;
        float x = (screenWidth - width) / 2, y = 20;
        return new Layout { Width = width, ButtonWidth = buttonWidth, RowHeight = rowHeight,
            Height = height, X = x, Y = y, Columns = columns, SongTop = 76 + OptionsHeight };
    }

    public void Draw(SongCatalog catalog)
    {
        var layout = PrepareLayout(catalog, Screen.width, Screen.height, GUI.skin);
        float x = layout.X, y = layout.Y, width = layout.Width, height = layout.Height;
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && choosingSlot < 0;
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Label(new Rect(x + 16, y + 8, width - 32, 32), $"AL MMD   Page {catalog.Page + 1}/{catalog.Pages}", title);
        GUI.Label(new Rect(x + 16, y + 40, width - 32, 28), "Click a song to choose its position", hint);
        DrawOptions(x + 16, y + 76, width - 32);
        if (catalog.Count == 0)
            GUI.Label(new Rect(x + 16, y + layout.SongTop, width - 32, 40), "No songs. Add Songs/01_SongName.", hint);
        for (int i = 0; i < catalog.VisibleCount; ++i)
        {
            var song = catalog.At(i);
            var rect = layout.SongRect(i);
            if (GUI.Button(rect, wrapped[song.Name], row)) pending = song;
        }
        float footerY = y + height - 40;
        float pageButtonWidth = Mathf.Min(160, (width - 32) / 3);
        if (GUI.Button(new Rect(x + 16, footerY, pageButtonWidth, 30), "< Prev")) catalog.ChangePage(-1);
        if (GUI.Button(new Rect(x + 24 + pageButtonWidth, footerY, pageButtonWidth, 30), "Next >")) catalog.ChangePage(1);
        GUI.Label(new Rect(x + 32 + pageButtonWidth * 2, footerY, width - 48 - pageButtonWidth * 2, 30),
            $"{DanceSettings.Label(DanceSettings.ToggleKey)}: cancel", hint);
        GUI.enabled = wasEnabled;
        DrawActorChoice();
    }

    public void DrawProgress(string song, float progress)
    {
        Styles();
        float width = Mathf.Min(620, Screen.width - 32), x = (Screen.width - width) / 2;
        string name = Wrap(song, width - 32);
        float nameHeight = Mathf.Max(30, row.CalcHeight(new GUIContent(name), width - 32));
        GUI.Box(new Rect(x, 24, width, nameHeight + 52), "");
        GUI.Label(new Rect(x + 16, 30, width - 32, nameHeight), name, hint);
        GUI.Label(new Rect(x + 16, 34 + nameHeight, width - 32, 26), $"Preparing {progress:P0}    {DanceSettings.Label(DanceSettings.ToggleKey)}: cancel", hint);
    }

    public void DrawRestore(int remaining)
    {
        Styles();
        float width = Mathf.Min(620, Screen.width - 32), x = (Screen.width - width) / 2;
        GUI.Box(new Rect(x, 24, width, 76), "");
        GUI.Label(new Rect(x + 16, 30, width - 32, 30), "AL MMD: Restoring characters", title);
        GUI.Label(new Rect(x + 16, 62, width - 32, 26), $"Please wait... {remaining} remaining", hint);
    }

    public void Dispose()
    {
        Reset(); title = row = hint = option = null;
        roster = null;
        if (font) UnityEngine.Object.Destroy(font);
        font = null;
    }
}
