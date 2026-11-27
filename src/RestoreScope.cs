namespace ALMMD;

// Register each undo before mutating state; a later failure cannot strand earlier changes.
internal sealed class RestoreScope
{
    private readonly List<Action> undo = new();
    public void Add(Action action) => undo.Add(action);
    public void Restore()
    {
        for (int i = undo.Count - 1; i >= 0; --i)
            try { undo[i](); }
            catch (Exception ex) { Plugin.LogError("Restore", ex); }
        undo.Clear();
    }
}
