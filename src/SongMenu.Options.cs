using UnityEngine;

namespace ALMMD;

internal sealed partial class SongMenu
{
    private DanceRoster roster;
    private int choosingSlot = -1;
    private Vector2 actorScroll;
    public void SetRoster(DanceRoster value) { roster = value; choosingSlot = -1; }
    private float OptionsHeight => roster == null ? 0 : 126 + ((roster.Count + 2) / 3) * 36;

    private void DrawOptions(float x, float y, float width)
    {
        if (roster == null) return;
        GUI.Label(new Rect(x, y, 70, 30), "視線", hint);
        string[] modes = { "自動", "MMD", "カメラを見る" };
        float bw = (width - 80) / 3;
        for (int i = 0; i < 3; i++)
            if (GUI.Button(new Rect(x + 80 + bw * i, y, bw - 6, 30),
                (DanceSettings.Gaze.Value == (GazeMode)i ? "● " : "○ ") + modes[i], option))
                DanceSettings.Gaze.Value = (GazeMode)i;
        if (GUI.Button(new Rect(x, y + 36, 180, 30),
            "フリーカメラ：" + (DanceSettings.FreeCamera.Value ? "オン" : "オフ"), option))
            DanceSettings.FreeCamera.Value = !DanceSettings.FreeCamera.Value;
        GUI.Label(new Rect(x + 192, y + 36, width - 192, 30),
            "右ドラッグで回転・中ドラッグで移動・ホイールでズーム", hint);
        GUI.Label(new Rect(x, y + 72, 70, 30), "人数", hint);
        for (int i = 1; i <= 6; i++)
            if (GUI.Button(new Rect(x + 80 + (i - 1) * 48, y + 72, 42, 30),
                (roster.Count == i ? "●" : "") + i, option))
            { roster.Count = i; choosingSlot = -1; }
        GUI.Label(new Rect(x + 378, y + 72, width - 378, 30),
            $"{DanceSettings.Label(DanceSettings.GazeKey)}：視線を切替", hint);
        float cell = width / 3;
        for (int slot = 0; slot < roster.Count; slot++)
        {
            var rect = new Rect(x + (slot % 3) * cell, y + 108 + (slot / 3) * 36, cell - 8, 30);
            string label = $"{slot}  {roster.At(slot).Name}" + (slot == 0 ? "（会話相手）" : "  ▾");
            if (slot == 0) GUI.Label(rect, label, hint);
            else if (GUI.Button(rect, label, option)) choosingSlot = slot;
        }
    }

    private void DrawActorChoice()
    {
        if (choosingSlot < 1 || roster == null) return;
        float width = Mathf.Min(520, Screen.width - 32), height = Mathf.Min(Screen.height - 48, 82 + roster.Choices.Count * 38);
        float x = (Screen.width - width) / 2, y = 24;
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Label(new Rect(x + 16, y + 8, width - 100, 30), $"出演者 {choosingSlot} を選択（重複可）", hint);
        if (GUI.Button(new Rect(x + width - 76, y + 8, 60, 30), "閉じる", option)) choosingSlot = -1;
        actorScroll = GUI.BeginScrollView(new Rect(x + 12, y + 44, width - 24, height - 54), actorScroll,
            new Rect(0, 0, width - 48, roster.Choices.Count * 38));
        for (int i = 0; i < roster.Choices.Count; i++)
            if (GUI.Button(new Rect(0, i * 38, width - 52, 32), roster.Choices[i].Name, option))
            { roster.Select(choosingSlot, i); choosingSlot = -1; }
        GUI.EndScrollView();
    }
}
