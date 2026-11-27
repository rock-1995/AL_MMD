namespace ALMMD;

internal sealed class LoadedSong
{
    internal sealed class Part
    {
        public string Name;
        public VmdMotion Motion, Face, Lips;
        public PreparedGaze Gaze;
    }
    public Part[] Parts;
    public VmdMotion Camera;
    public WavAudio Audio;

    public static LoadedSong Read(string directory)
    {
        var parts = ReadParts(directory);
        return new LoadedSong { Parts = parts,
            Camera = VmdMotion.Load(Path.Combine(directory, "camera.vmd")),
            Audio = WavAudio.Load(Path.Combine(directory, "music.wav")) };
    }

    internal static Part[] ReadParts(string directory)
    {
        var parts = new List<Part>();
        // Explicit person folders avoid confusing alternate model adaptations
        // (such as TDA/YYB/full/light) with distinct dancers.
        string dancers = Path.Combine(directory, "dancers");
        if (Directory.Exists(dancers))
        {
            foreach (string part in Directory.EnumerateDirectories(dancers).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                AddPart(parts, part, "motion", VmdMotion.Load(Path.Combine(part, "motion.vmd")));
        }
        else
        {
            // motion.vmd / motion_1.vmd / motion_2.vmd and model1/model2.
            // Keep the established motion.vmd at index zero.
            var files = Directory.EnumerateFiles(directory, "*.vmd")
                .Where(p => IsPartName(Path.GetFileNameWithoutExtension(p)))
                .OrderBy(p => Path.GetFileName(p).Equals("motion.vmd", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(p => PartNumber(Path.GetFileNameWithoutExtension(p)))
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase);
            foreach (string path in files)
                AddPart(parts, directory, Path.GetFileNameWithoutExtension(path), VmdMotion.Load(path));
        }
        if (parts.Count == 0) throw new InvalidDataException("The song folder contains no character-motion VMD files: " + directory);
        return parts.ToArray();
    }

    private static bool IsPartName(string name)
    {
        if (name.Equals("motion", StringComparison.OrdinalIgnoreCase)) return true;
        string suffix = name.StartsWith("motion", StringComparison.OrdinalIgnoreCase) ? name.Substring(6) :
            name.StartsWith("model", StringComparison.OrdinalIgnoreCase) ? name.Substring(5) : "";
        return int.TryParse(suffix.TrimStart('_', '-', ' '), out _);
    }
    private static int PartNumber(string name)
    {
        int first = name.Length;
        while (first > 0 && char.IsDigit(name[first - 1])) first--;
        return int.TryParse(name.Substring(first), out int n) ? n : -1;
    }
    private static void AddPart(List<Part> parts, string directory, string name, VmdMotion motion)
    {
        string suffix = name.StartsWith("motion", StringComparison.OrdinalIgnoreCase) ? name.Substring(6) : name.Substring(5);
        VmdMotion Companion(string kind)
        {
            string specific = Path.Combine(directory, kind + suffix + ".vmd");
            string shared = Path.Combine(directory, kind + ".vmd");
            return File.Exists(specific) ? VmdMotion.Load(specific) :
                File.Exists(shared) ? VmdMotion.Load(shared) : motion;
        }
        var face = Companion("face");
        parts.Add(new Part { Name = Path.GetFileName(directory) + "/" + name,
            Motion = motion, Face = face, Lips = Companion("lips"), Gaze = new PreparedGaze(motion, face) });
    }
}
