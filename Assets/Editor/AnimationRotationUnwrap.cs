using UnityEngine;
using UnityEditor;

public class AnimationRotationUnwrap : EditorWindow
{
    private AnimationClip animationClip;
    private bool check180Flips = false;

    [MenuItem("Tools/Animation Rotation Unwrap")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRotationUnwrap>("Animation Rotation Unwrap");
    }

    void OnGUI()
    {
        GUILayout.Label("Unwrap Animation Rotations", EditorStyles.boldLabel);
        GUILayout.Label("Fixes rotations that wrap around 360°", EditorStyles.helpBox);

        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        check180Flips = EditorGUILayout.Toggle("Also Check ±180° Flips", check180Flips);

        if (GUILayout.Button("Unwrap Rotations"))
        {
            if (animationClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an animation clip.", "OK");
                return;
            }

            UnwrapRotations();
        }
    }

    void UnwrapRotations()
    {
        int curvesFixed = 0;

        // Get all curve bindings
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animationClip);

        // Group rotation curves by path and find XYZ triplets
        System.Collections.Generic.Dictionary<string, RotationCurves> rotationGroups =
            new System.Collections.Generic.Dictionary<string, RotationCurves>();

        foreach (EditorCurveBinding binding in bindings)
        {
            // Check if this is a rotation property
            if (binding.propertyName.Contains("localEulerAngles") ||
                binding.propertyName.Contains("m_LocalEulerAngles"))
            {
                string path = binding.path;
                if (!rotationGroups.ContainsKey(path))
                {
                    rotationGroups[path] = new RotationCurves();
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, binding);

                if (binding.propertyName.EndsWith(".x"))
                {
                    rotationGroups[path].x = curve;
                    rotationGroups[path].xBinding = binding;
                }
                else if (binding.propertyName.EndsWith(".y"))
                {
                    rotationGroups[path].y = curve;
                    rotationGroups[path].yBinding = binding;
                }
                else if (binding.propertyName.EndsWith(".z"))
                {
                    rotationGroups[path].z = curve;
                    rotationGroups[path].zBinding = binding;
                }
            }
        }

        // Process each rotation group
        foreach (var group in rotationGroups.Values)
        {
            if (group.x != null && group.y != null && group.z != null &&
                group.x.keys.Length > 1)
            {
                UnwrapRotationGroup(group);

                AnimationUtility.SetEditorCurve(animationClip, group.xBinding, group.x);
                AnimationUtility.SetEditorCurve(animationClip, group.yBinding, group.y);
                AnimationUtility.SetEditorCurve(animationClip, group.zBinding, group.z);
                curvesFixed += 3;
            }
        }

        if (curvesFixed > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Unwrapped {curvesFixed} rotation curves.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No rotation curves found to unwrap.", "OK");
        }
    }

    class RotationCurves
    {
        public AnimationCurve x, y, z;
        public EditorCurveBinding xBinding, yBinding, zBinding;
    }

    void UnwrapRotationGroup(RotationCurves curves)
    {
        Keyframe[] xKeys = curves.x.keys;
        Keyframe[] yKeys = curves.y.keys;
        Keyframe[] zKeys = curves.z.keys;

        int xCount = xKeys.Length;
        int yCount = yKeys.Length;
        int zCount = zKeys.Length;

        if (xCount == 0 || yCount == 0 || zCount == 0) return;

        // Use minimum keyframe count to avoid index out of bounds
        int minCount = Mathf.Min(xCount, yCount, zCount);

        Keyframe[] unwrappedX = new Keyframe[xCount];
        Keyframe[] unwrappedY = new Keyframe[yCount];
        Keyframe[] unwrappedZ = new Keyframe[zCount];

        // Start with first keyframe as-is
        unwrappedX[0] = xKeys[0];
        unwrappedY[0] = yKeys[0];
        unwrappedZ[0] = zKeys[0];

        // Process shared keyframes (up to minimum count)
        for (int i = 1; i < minCount; i++)
        {
            Vector3 current = new Vector3(xKeys[i].value, yKeys[i].value, zKeys[i].value);

            // Find best representation by checking against ALL previous keyframes
            current = FindBestRepresentationFromAll(unwrappedX, unwrappedY, unwrappedZ, i, current);

            // Create new keyframes
            unwrappedX[i] = new Keyframe(xKeys[i].time, current.x, xKeys[i].inTangent, xKeys[i].outTangent, xKeys[i].inWeight, xKeys[i].outWeight);
            unwrappedX[i].weightedMode = xKeys[i].weightedMode;

            unwrappedY[i] = new Keyframe(yKeys[i].time, current.y, yKeys[i].inTangent, yKeys[i].outTangent, yKeys[i].inWeight, yKeys[i].outWeight);
            unwrappedY[i].weightedMode = yKeys[i].weightedMode;

            unwrappedZ[i] = new Keyframe(zKeys[i].time, current.z, zKeys[i].inTangent, zKeys[i].outTangent, zKeys[i].inWeight, zKeys[i].outWeight);
            unwrappedZ[i].weightedMode = zKeys[i].weightedMode;
        }

        // Copy any remaining keyframes that weren't processed
        for (int i = minCount; i < xCount; i++)
        {
            unwrappedX[i] = xKeys[i];
        }
        for (int i = minCount; i < yCount; i++)
        {
            unwrappedY[i] = yKeys[i];
        }
        for (int i = minCount; i < zCount; i++)
        {
            unwrappedZ[i] = zKeys[i];
        }

        // Create new curves
        curves.x = new AnimationCurve(unwrappedX);
        curves.y = new AnimationCurve(unwrappedY);
        curves.z = new AnimationCurve(unwrappedZ);
    }

    Vector3 FindBestRepresentationFromAll(Keyframe[] xKeysSoFar, Keyframe[] yKeysSoFar, Keyframe[] zKeysSoFar, int currentIndex, Vector3 current)
    {
        Vector3 previous = new Vector3(xKeysSoFar[currentIndex - 1].value, yKeysSoFar[currentIndex - 1].value, zKeysSoFar[currentIndex - 1].value);

        // Get the target quaternion (perceived rotation)
        Quaternion targetQuat = Quaternion.Euler(current);

        Vector3 bestMatch = current;
        float bestDistance = float.MaxValue;

        // Determine increment and range based on check180Flips setting
        float increment = check180Flips ? 180f : 360f;
        int range = check180Flips ? 2 : 1;

        // Try different euler representations of the same quaternion
        for (int xOffset = -range; xOffset <= range; xOffset++)
        {
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                for (int zOffset = -range; zOffset <= range; zOffset++)
                {
                    Vector3 candidate = new Vector3(
                        current.x + xOffset * increment,
                        current.y + yOffset * increment,
                        current.z + zOffset * increment
                    );

                    // Check if any axis exceeds 160 degree jump
                    float xDelta = Mathf.Abs(candidate.x - previous.x);
                    float yDelta = Mathf.Abs(candidate.y - previous.y);
                    float zDelta = Mathf.Abs(candidate.z - previous.z);

                    if (xDelta > 160f || yDelta > 160f || zDelta > 160f)
                        continue;

                    // Calculate total angular distance from previous
                    float distance = xDelta + yDelta + zDelta;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = candidate;
                    }
                }
            }
        }

        return bestMatch;
    }

    float UnwrapAngle(float previous, float current)
    {
        float diff = current - previous;

        while (diff > 180f)
        {
            current -= 360f;
            diff = current - previous;
        }

        while (diff < -180f)
        {
            current += 360f;
            diff = current - previous;
        }

        return current;
    }

    Vector3 UnwrapVector3(Vector3 previous, Vector3 current)
    {
        current.x = UnwrapAngle(previous.x, current.x);
        current.y = UnwrapAngle(previous.y, current.y);
        current.z = UnwrapAngle(previous.z, current.z);
        return current;
    }

    Vector3 FindClosestEulerRepresentation(Vector3 previous, Vector3 current)
    {
        Vector3 bestMatch = current;
        float bestDistance = float.MaxValue;

        // Determine increment and range based on check180Flips setting
        float increment = check180Flips ? 180f : 360f;
        int range = check180Flips ? 2 : 1;

        // Try different offset combinations
        for (int xOffset = -range; xOffset <= range; xOffset++)
        {
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                for (int zOffset = -range; zOffset <= range; zOffset++)
                {
                    Vector3 candidate = new Vector3(
                        current.x + xOffset * increment,
                        current.y + yOffset * increment,
                        current.z + zOffset * increment
                    );

                    // Calculate total angular distance from previous
                    float distance = Mathf.Abs(candidate.x - previous.x) +
                                   Mathf.Abs(candidate.y - previous.y) +
                                   Mathf.Abs(candidate.z - previous.z);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = candidate;
                    }
                }
            }
        }

        return bestMatch;
    }

}
