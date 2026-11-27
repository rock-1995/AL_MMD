using CompositeCanvas;
using UnityEngine;

namespace ALMMD;

internal static class DanceUI
{
    public static void Hide(RestoreScope restore)
    {
        // Composite renderers keep canvas-rebuild callbacks while their Canvas is disabled.
        // Disable them first so OnDisable unregisters those callbacks while the Canvas still exists.
        foreach (var renderer in UnityEngine.Object.FindObjectsOfType<CompositeCanvasRenderer>())
            Disable(renderer, restore);
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            Disable(canvas, restore);
        // RestoreScope unwinds in reverse: all canvases return before their renderers re-register.
    }

    private static void Disable(Behaviour component, RestoreScope restore)
    {
        bool enabled = component.enabled;
        restore.Add(() => { if (component) component.enabled = enabled; });
        component.enabled = false;
    }
}
