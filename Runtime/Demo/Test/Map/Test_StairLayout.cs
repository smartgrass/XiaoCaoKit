using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_StairLayout : MonoBehaviour
{
#if UNITY_EDITOR
    public enum StairForwardAxis
    {
        Auto,
        X,
        Z,
    }

    [MiniBtn(nameof(CaculateStepInfo))] [OnValueChanged(nameof(CaculateStepInfo))]
    public GameObject prefab;

    [Min(1)] public int stairCount = 1;

    public StairForwardAxis forwardAxis = StairForwardAxis.Auto;

    public bool reverseDirection;

    [Label("追加间距")] public Vector3 addValue;

    [Label("忽略前缀")] public string ignoreBoundsNamePrefix = "_";

    public string prefabName;

    [ReadOnly] public Vector3 boxSize;
    [ReadOnly] public Vector3 center;
    [ReadOnly] public float stairHeightDelta;
    [ReadOnly] public float stairLength;
    [ReadOnly] public Vector3 stairOffset;
    [ReadOnly] public float stairTiltAngle;

    void CaculateStepInfo()
    {
        if (prefab == null)
        {
            boxSize = Vector3.zero;
            center = Vector3.zero;
            stairHeightDelta = 0f;
            stairLength = 0f;
            stairOffset = addValue;
            stairTiltAngle = 0f;
            return;
        }

        Bounds bounds = GetPrefabLocalBounds(prefab);
        boxSize = bounds.size;
        center = bounds.center;

        StairForwardAxis axis = ResolveForwardAxis(bounds);
        stairHeightDelta = boxSize.y;
        stairLength = axis == StairForwardAxis.X ? boxSize.x : boxSize.z;

        float directionSign = GetDirectionSign(axis, bounds);
        if (reverseDirection)
        {
            directionSign *= -1f;
        }

        stairOffset = axis == StairForwardAxis.X
            ? new Vector3(stairLength * directionSign, stairHeightDelta, 0f)
            : new Vector3(0f, stairHeightDelta, stairLength * directionSign);

        stairOffset += addValue;
        stairTiltAngle = GetTiltAngle(stairOffset);
    }

    [Button("生成/排列")]
    void TestGent()
    {
        if (prefab == null)
        {
            return;
        }

        stairCount = Mathf.Max(1, stairCount);
        CaculateStepInfo();

        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = prefab.name;
        }

        for (int i = 0; i < stairCount; i++)
        {
            GameObject obj = GetObject(i);
            obj.name = $"{prefabName}{i}";
            obj.transform.localPosition = stairOffset * i;
        }

        int delta = transform.childCount - stairCount;
        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                GameObject go = transform.GetChild(stairCount).gameObject;
                GameObject.DestroyImmediate(go);
            }
        }
    }

    private StairForwardAxis ResolveForwardAxis(Bounds bounds)
    {
        if (forwardAxis != StairForwardAxis.Auto)
        {
            return forwardAxis;
        }

        float absCenterX = Mathf.Abs(bounds.center.x);
        float absCenterZ = Mathf.Abs(bounds.center.z);
        const float tolerance = 0.001f;

        if (absCenterX > absCenterZ + tolerance)
        {
            return StairForwardAxis.X;
        }

        if (absCenterZ > absCenterX + tolerance)
        {
            return StairForwardAxis.Z;
        }

        // Fall back to the shorter horizontal side when center offset is inconclusive.
        return bounds.size.x < bounds.size.z ? StairForwardAxis.X : StairForwardAxis.Z;
    }

    private float GetDirectionSign(StairForwardAxis axis, Bounds bounds)
    {
        float axisCenter = axis == StairForwardAxis.X ? bounds.center.x : bounds.center.z;
        if (Mathf.Abs(axisCenter) <= 0.001f)
        {
            return 1f;
        }

        return Mathf.Sign(axisCenter);
    }

    private float GetTiltAngle(Vector3 offset)
    {
        float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
        if (horizontalDistance <= 0.001f)
        {
            if (Mathf.Abs(offset.y) <= 0.001f)
            {
                return 0f;
            }

            return Mathf.Sign(offset.y) * 90f;
        }

        return Mathf.Atan2(offset.y, horizontalDistance) * Mathf.Rad2Deg;
    }

    private Bounds GetPrefabLocalBounds(GameObject targetPrefab)
    {
        Renderer[] renderers = targetPrefab.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        Matrix4x4 rootWorldToLocal = targetPrefab.transform.worldToLocalMatrix;

        foreach (Renderer renderer in renderers)
        {
            if (ShouldIgnoreRenderer(renderer, targetPrefab.transform))
            {
                continue;
            }

            if (!TryGetRendererLocalBounds(renderer, out Bounds rendererLocalBounds))
            {
                continue;
            }

            Matrix4x4 rendererToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;
            EncapsulateBounds(ref result, ref hasBounds, rendererLocalBounds, rendererToRoot);
        }

        return hasBounds ? result : new Bounds(Vector3.zero, Vector3.zero);
    }

    private bool ShouldIgnoreRenderer(Renderer renderer, Transform root)
    {
        if (renderer == null || string.IsNullOrEmpty(ignoreBoundsNamePrefix))
        {
            return false;
        }

        Transform current = renderer.transform;
        while (current != null && current != root)
        {
            if (current.name.StartsWith(ignoreBoundsNamePrefix))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool TryGetRendererLocalBounds(Renderer renderer, out Bounds rendererLocalBounds)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            rendererLocalBounds = skinnedMeshRenderer.localBounds;
            return true;
        }

        if (renderer.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
        {
            rendererLocalBounds = meshFilter.sharedMesh.bounds;
            return true;
        }

        rendererLocalBounds = default;
        return false;
    }

    private void EncapsulateBounds(
        ref Bounds targetBounds,
        ref bool hasBounds,
        Bounds sourceBounds,
        Matrix4x4 localToRoot)
    {
        Vector3 min = sourceBounds.min;
        Vector3 max = sourceBounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 point = localToRoot.MultiplyPoint3x4(corner);
            if (!hasBounds)
            {
                targetBounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                targetBounds.Encapsulate(point);
            }
        }
    }

    public GameObject GetObject(int index)
    {
        if (transform.childCount > index)
        {
            return transform.GetChild(index).gameObject;
        }

        return BuildingTool.EditorInstancePrefab(prefab, transform);
    }
#endif
}
