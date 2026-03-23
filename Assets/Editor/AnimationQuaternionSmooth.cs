using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationQuaternionSmooth : EditorWindow
{
    private AnimationClip animationClip;
    private float searchIncrement = 45f;

    [MenuItem("Tools/Animation Quaternion Smooth")]
    public static void ShowWindow()
    {
        GetWindow<AnimationQuaternionSmooth>("Quaternion Smooth");
    }

    void OnGUI()
    {
        GUILayout.Label("Smooth Euler Paths via Quaternions", EditorStyles.boldLabel);
        GUILayout.Label("Finds equivalent euler representations for smoother paths", EditorStyles.helpBox);

        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        searchIncrement = EditorGUILayout.FloatField("Search Increment (degrees)", searchIncrement);

        if (GUILayout.Button("Smooth Rotations"))
        {
            if (animationClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an animation clip.", "OK");
                return;
            }

            SmoothRotations();
        }
    }

    void SmoothRotations()
    {
        int curvesFixed = 0;

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animationClip);

        Dictionary<string, RotationCurves> rotationGroups = new Dictionary<string, RotationCurves>();

        foreach (EditorCurveBinding binding in bindings)
        {
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

        foreach (var group in rotationGroups.Values)
        {
            if (group.x != null && group.y != null && group.z != null &&
                group.x.keys.Length > 1)
            {
                SmoothRotationGroup(group);

                AnimationUtility.SetEditorCurve(animationClip, group.xBinding, group.x);
                AnimationUtility.SetEditorCurve(animationClip, group.yBinding, group.y);
                AnimationUtility.SetEditorCurve(animationClip, group.zBinding, group.z);
                curvesFixed += 3;
            }
        }

        if (curvesFixed > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Smoothed {curvesFixed} rotation curves.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No rotation curves found to smooth.", "OK");
        }
    }

    class RotationCurves
    {
        public AnimationCurve x, y, z;
        public EditorCurveBinding xBinding, yBinding, zBinding;
    }

    void SmoothRotationGroup(RotationCurves curves)
    {
        Keyframe[] xKeys = curves.x.keys;
        Keyframe[] yKeys = curves.y.keys;
        Keyframe[] zKeys = curves.z.keys;

        int minCount = Mathf.Min(xKeys.Length, yKeys.Length, zKeys.Length);
        if (minCount == 0) return;

        Keyframe[] smoothX = new Keyframe[xKeys.Length];
        Keyframe[] smoothY = new Keyframe[yKeys.Length];
        Keyframe[] smoothZ = new Keyframe[zKeys.Length];

        // Start with first keyframe as-is
        smoothX[0] = xKeys[0];
        smoothY[0] = yKeys[0];
        smoothZ[0] = zKeys[0];

        Vector3 previousEuler = new Vector3(xKeys[0].value, yKeys[0].value, zKeys[0].value);

        for (int i = 1; i < minCount; i++)
        {
            Vector3 currentEuler = new Vector3(xKeys[i].value, yKeys[i].value, zKeys[i].value);
            Quaternion targetQuat = Quaternion.Euler(currentEuler);

            // Find best euler representation for this quaternion
            Vector3 bestEuler = FindBestEulerForQuaternion(targetQuat, previousEuler);

            smoothX[i] = new Keyframe(xKeys[i].time, bestEuler.x, xKeys[i].inTangent, xKeys[i].outTangent, xKeys[i].inWeight, xKeys[i].outWeight);
            smoothX[i].weightedMode = xKeys[i].weightedMode;

            smoothY[i] = new Keyframe(yKeys[i].time, bestEuler.y, yKeys[i].inTangent, yKeys[i].outTangent, yKeys[i].inWeight, yKeys[i].outWeight);
            smoothY[i].weightedMode = yKeys[i].weightedMode;

            smoothZ[i] = new Keyframe(zKeys[i].time, bestEuler.z, zKeys[i].inTangent, zKeys[i].outTangent, zKeys[i].inWeight, zKeys[i].outWeight);
            smoothZ[i].weightedMode = zKeys[i].weightedMode;

            previousEuler = bestEuler;
        }

        // Copy remaining keyframes
        for (int i = minCount; i < xKeys.Length; i++)
        {
            smoothX[i] = xKeys[i];
        }
        for (int i = minCount; i < yKeys.Length; i++)
        {
            smoothY[i] = yKeys[i];
        }
        for (int i = minCount; i < zKeys.Length; i++)
        {
            smoothZ[i] = zKeys[i];
        }

        curves.x = new AnimationCurve(smoothX);
        curves.y = new AnimationCurve(smoothY);
        curves.z = new AnimationCurve(smoothZ);
    }

    Vector3 FindBestEulerForQuaternion(Quaternion targetQuat, Vector3 previousEuler)
    {
        Vector3 bestEuler = targetQuat.eulerAngles;
        float bestDistance = Vector3.Distance(bestEuler, previousEuler);

        // Generate candidate euler angles by searching around each axis
        int steps = Mathf.Max(1, Mathf.RoundToInt(360f / searchIncrement));

        for (int xStep = 0; xStep < steps; xStep++)
        {
            for (int yStep = 0; yStep < steps; yStep++)
            {
                for (int zStep = 0; zStep < steps; zStep++)
                {
                    Vector3 candidateEuler = new Vector3(
                        xStep * searchIncrement,
                        yStep * searchIncrement,
                        zStep * searchIncrement
                    );

                    Quaternion candidateQuat = Quaternion.Euler(candidateEuler);

                    // Check if this euler produces the same quaternion (or very close)
                    float quatDistance = Quaternion.Angle(targetQuat, candidateQuat);

                    if (quatDistance < 0.1f) // Essentially the same rotation
                    {
                        // Adjust candidate to be within ±180 of previous on each axis
                        Vector3 adjustedCandidate = AdjustToNearestEquivalent(candidateEuler, previousEuler);

                        float eulerDistance = Vector3.Distance(adjustedCandidate, previousEuler);

                        if (eulerDistance < bestDistance)
                        {
                            bestDistance = eulerDistance;
                            bestEuler = adjustedCandidate;
                        }
                    }
                }
            }
        }

        return bestEuler;
    }

    Vector3 AdjustToNearestEquivalent(Vector3 candidate, Vector3 reference)
    {
        // For each axis, add/subtract 360 to get closest to reference
        Vector3 result = candidate;

        for (int axis = 0; axis < 3; axis++)
        {
            float value = result[axis];
            float refValue = reference[axis];

            // Try offsets
            float bestOffset = value;
            float bestDist = Mathf.Abs(value - refValue);

            for (int offset = -2; offset <= 2; offset++)
            {
                float testValue = value + offset * 360f;
                float dist = Mathf.Abs(testValue - refValue);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestOffset = testValue;
                }
            }

            result[axis] = bestOffset;
        }

        return result;
    }
}
