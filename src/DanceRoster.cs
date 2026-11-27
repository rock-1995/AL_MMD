using AL;
using Character;
using ILLGAMES.Unity.Component;

namespace ALMMD;

internal sealed class DanceRoster
{
    internal sealed class Choice
    {
        public Human Human;
        public Actor Actor;
        public string Name;
        public int OriginalCoordinate;
    }
    public readonly List<Choice> Choices = new();
    private readonly int[] selection = new int[6];
    public int Count { get; set; } = 1;
    public Choice At(int slot) => Choices[selection[slot]];
    public int SelectedIndex(int slot) => selection[slot];
    public void Select(int slot, int index) { if (slot != 0) selection[slot] = index; }
    public IEnumerable<Choice> UniqueSources() => Enumerable.Range(0, Count).Select(At).Distinct();
    public bool Reloading => UniqueSources().Any(c => c.Human.IsReloading);

    public void Open(Heroine lead)
    {
        Clear();
        void Add(Actor actor, Human human, bool player = false)
        {
            if (human == null || human.Disposed || !human.GameObject) return;
            if (Choices.Any(c => c.Human.Pointer == human.Pointer)) return;
            Choices.Add(new Choice { Human = human, Actor = actor,
                Name = human.FileParam.fullname + (player ? "（主人公）" : ""),
                OriginalCoordinate = human.FileStatus.coordinateType });
        }
        Add(lead, lead.Human);
        var game = SingletonInitializer<Manager.Game>._instance;
        if (game)
        {
            foreach (var actor in game._heroines)
                if (actor != null && actor.MapID == lead.MapID) Add(actor, actor.Human);
            var player = game.Player;
            if (player != null) Add(player, player.Human, true);
        }
        for (int i = 1; i < selection.Length; i++) selection[i] = i % Choices.Count;
    }

    public void Clear() { Choices.Clear(); Array.Clear(selection, 0, selection.Length); Count = 1; }
}
