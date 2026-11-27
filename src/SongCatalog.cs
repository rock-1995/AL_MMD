using BepInEx.Configuration;

namespace ALMMD;

internal sealed class SongCatalog
{
    internal sealed class Song
    {
        public readonly string Name, Directory, Number;
        public Song(string directory)
        {
            Directory = directory;
            string folder = Path.GetFileName(directory);
            Number = folder.Substring(0, 2);
            Name = folder.Substring(3);
        }
    }

    private readonly ConfigEntry<string> root;
    private Song[] songs = Array.Empty<Song>();
    private int page, pageSize = 30;
    public int MaximumPageSize { get; private set; } = 30;
    public IReadOnlyList<Song> All => songs;
    private string[] labels = Array.Empty<string>();
    public int Count => songs.Length;
    public int VisibleCount => Math.Min(pageSize, Math.Max(0, Count - page * pageSize));
    public int Pages => Math.Max(1, (Count + pageSize - 1) / pageSize);
    public int Page => page;
    public IReadOnlyList<string> Labels => labels;
    public string ChoiceSummary => string.Join(", ", songs.Select(s => s.Number + "=" + s.Name));

    public SongCatalog(ConfigFile config, string pluginDirectory)
    {
        root = config.Bind("Library", "Directory", Path.Combine(pluginDirectory, "Songs"),
            "Song library. Use NN_SongName folders, e.g. 01_Girls. Refreshed whenever the song menu opens.");
    }

    internal static bool IsSongFolder(string name) => name.Length > 3 && name[2] == '_' &&
        name[0] >= '0' && name[0] <= '9' && name[1] >= '0' && name[1] <= '9' &&
        !string.IsNullOrWhiteSpace(name.Substring(3));

    public void Refresh()
    {
        songs = System.IO.Directory.Exists(root.Value)
            ? System.IO.Directory.EnumerateDirectories(root.Value)
                .Where(p => IsSongFolder(Path.GetFileName(p)))
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(p => new Song(p)).ToArray()
            : Array.Empty<Song>();
        page = 0;
        pageSize = MaximumPageSize = DanceSettings.SongsPerPage.Value;
        UpdateLabels();
    }

    public Song At(int visibleIndex) => visibleIndex >= 0 && visibleIndex < VisibleCount
        ? songs[page * pageSize + visibleIndex] : null;

    public void Clear()
    {
        songs = Array.Empty<Song>(); labels = Array.Empty<string>(); page = 0;
    }

    public void FitPage(int capacity)
    {
        int size = Math.Max(1, Math.Min(MaximumPageSize, capacity));
        if (size == pageSize) return;
        int first = page * pageSize;
        pageSize = size;
        page = Math.Min(first / pageSize, Pages - 1);
        UpdateLabels();
    }

    public void ChangePage(int delta)
    {
        page = (page + delta + Pages) % Pages;
        UpdateLabels();
    }
    private void UpdateLabels() => labels = songs.Skip(page * pageSize).Take(pageSize)
        .Select(s => s.Name).ToArray();
}
