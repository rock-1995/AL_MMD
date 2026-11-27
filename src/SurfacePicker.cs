using NatureManufacture.RAM;
using UnityEngine;

namespace ALMMD;

// Exists only while choosing a location. Water render meshes often have no physics
// collider. Private, normally-disabled proxies query their real footprint without
// adding a collision floor to the game or flattening the whole map to water level.
internal sealed class SurfacePicker : IDisposable
{
    private sealed class WaterSurface
    {
        public MeshFilter Source;
        public Renderer Renderer;
        public MeshCollider Query;
    }
    private readonly List<WaterSurface> water = new();
    private readonly HashSet<int> meshes = new(), waterColliders = new();
    private Transform markers;

    public void Capture(Transform markerRoot)
    {
        markers = markerRoot;
        foreach (var lake in UnityEngine.Object.FindObjectsOfType<LakePolygon>())
        {
            AddWaterMesh(lake.MainMeshFilter);
            if (lake.meshesPartTransforms != null)
                foreach (var part in lake.meshesPartTransforms)
                    if (part) AddWaterMesh(part.GetComponent<MeshFilter>());
        }
        foreach (var river in UnityEngine.Object.FindObjectsOfType<RamSpline>())
        {
            AddWaterMesh(river.MainMeshFilter);
            if (river.meshesPartTransforms != null)
                foreach (var part in river.meshesPartTransforms)
                    if (part) AddWaterMesh(part.GetComponent<MeshFilter>());
        }
        Physics.SyncTransforms();
        Plugin.Logger.LogInfo($"Surface picker: {water.Count} RAM water meshes captured.");
    }

    internal void AddWaterMesh(MeshFilter source)
    {
        if (!source || !source.sharedMesh || !meshes.Add(source.GetInstanceID())) return;
        var renderer = source.GetComponent<Renderer>();
        if (!renderer) return;
        foreach (var collider in source.GetComponents<Collider>()) waterColliders.Add(collider.GetInstanceID());
        var obj = new GameObject("AL MMD water query");
        obj.layer = 2; // Ignore Raycast: never participates in the normal surface query.
        obj.transform.SetParent(source.transform, false);
        var query = obj.AddComponent<MeshCollider>();
        query.enabled = false;
        query.sharedMesh = source.sharedMesh;
        water.Add(new WaterSurface { Source = source, Renderer = renderer, Query = query });
    }

    public bool TryPick(Ray ray, Vector3 eye, float far, out Vector3 position)
    {
        position = default;
        float distance = far;
        bool found = false;
        // RaycastAll runs only on clicks; unlike a fixed-size buffer it cannot drop
        // the closest surface when a ray crosses a densely decorated map.
        foreach (var hit in Physics.RaycastAll(ray, far, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var collider = hit.collider;
            if (!collider || waterColliders.Contains(collider.GetInstanceID()) ||
                (markers && collider.transform.IsChildOf(markers))) continue;
            if (hit.distance < distance) { distance = hit.distance; position = hit.point; found = true; }
        }
        foreach (var surface in water)
        {
            if (!surface.Source || !surface.Renderer || !surface.Renderer.enabled || !surface.Source.gameObject.activeInHierarchy) continue;
            var query = surface.Query;
            query.enabled = true;
            try
            {
                Physics.SyncTransforms();
                var bounds = surface.Renderer.bounds;
                // Probe from above the mesh at the CAMERA's XZ, not the clicked
                // point: being below a pool's height while outside it isn't underwater.
                float top = Mathf.Max(bounds.max.y, eye.y) + 1;
                var probe = new Ray(new Vector3(eye.x, top, eye.z), Vector3.down);
                bool inside = query.Raycast(probe, out var above, Mathf.Max(1, top - bounds.min.y + 1)) && eye.y < above.point.y;
                if (inside) continue;
                if (query.Raycast(ray, out var hit, distance))
                { distance = hit.distance; position = hit.point; found = true; }
            }
            finally { query.enabled = false; }
        }
        return found;
    }

    public void Dispose()
    {
        foreach (var surface in water)
            if (surface.Query) { surface.Query.enabled = false; UnityEngine.Object.Destroy(surface.Query.gameObject); }
        water.Clear(); meshes.Clear(); waterColliders.Clear(); markers = null;
    }
}
