using UnityEngine;
using UnityEditor;

public class AnimationRotationRangeCorrector : EditorWindow
{
    private AnimationClip animationClip;
    private float maxRotationRange = 180f;

    [MenuItem("Tools/Animation Rotation Range Corrector")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRotationRangeCorrector>("Rotation Range Corrector");
    }

    void OnGUI()
    {
        GUILayout.Label("Correct Excessive Rotation Ranges", EditorStyles.boldLabel);
        GUILayout.Label("Fixes rotations with total range exceeding threshold", EditorStyles.helpBox);

        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        maxRotationRange = EditorGUILayout.FloatField("Max Range (degrees)", maxRotationRange);

        if (GUILayout.Button("Correct Rotations"))
        {
            if (animationClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an animation clip.", "OK");
                return;
            }

            CorrectRotations();
        }
    }

    void CorrectRotations()
    {
        int curvesFixed = 0;

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animationClip);

        System.Collections.Generic.Dictionary<string, RotationCurves> rotationGroups =
            new System.Collections.Generic.Dictionary<string, RotationCurves>();

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
                if (CorrectRotationGroup(group))
                {
                    AnimationUtility.SetEditorCurve(animationClip, group.xBinding, group.x);
                    AnimationUtility.SetEditorCurve(animationClip, group.yBinding, group.y);
                    AnimationUtility.SetEditorCurve(animationClip, group.zBinding, group.z);
                    curvesFixed += 3;
                }
            }
        }

        if (curvesFixed > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Corrected {curvesFixed} rotation curves.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No rotation curves needed correction.", "OK");
        }
    }

    class RotationCurves
    {
        public AnimationCurve x, y, z;
        public EditorCurveBinding xBinding, yBinding, zBinding;
    }

    bool CorrectRotationGroup(RotationCurves curves)
    {
        Keyframe[] xKeys = curves.x.keys;
        Keyframe[] yKeys = curves.y.keys;
        Keyframe[] zKeys = curves.z.keys;

        int minCount = Mathf.Min(xKeys.Length, yKeys.Length, zKeys.Length);
        if (minCount == 0) return false;

        // Calculate initial range to see if correction is needed
        float xRange = GetRange(xKeys);
        float yRange = GetRange(yKeys);
        float zRange = GetRange(zKeys);

        if (xRange <= maxRotationRange && yRange <= maxRotationRange && zRange <= maxRotationRange)
        {
            return false; // No correction needed
        }

        // Try to find better euler representations
        Keyframe[] correctedX = new Keyframe[xKeys.Length];
        Keyframe[] correctedY = new Keyframe[yKeys.Length];
        Keyframe[] correctedZ = new Keyframe[zKeys.Length];

        // Start with first keyframe
        correctedX[0] = xKeys[0];
        correctedY[0] = yKeys[0];
        correctedZ[0] = zKeys[0];

        // Process each subsequent keyframe
        for (int i = 1; i < minCount; i++)
        {
            Vector3 prev = new Vector3(correctedX[i - 1].value, correctedY[i - 1].value, correctedZ[i - 1].value);
            Vector3 current = new Vector3(xKeys[i].value, yKeys[i].value, zKeys[i].value);

            // Find representation that minimizes total range so far
            current = FindBestRepresentation(prev, current, correctedX, correctedY, correctedZ, i);

            correctedX[i] = new Keyframe(xKeys[i].time, current.x, xKeys[i].inTangent, xKeys[i].outTangent, xKeys[i].inWeight, xKeys[i].outWeight);
            correctedX[i].weightedMode = xKeys[i].weightedMode;

            correctedY[i] = new Keyframe(yKeys[i].time, current.y, yKeys[i].inTangent, yKeys[i].outTangent, yKeys[i].inWeight, yKeys[i].outWeight);
            correctedY[i].weightedMode = yKeys[i].weightedMode;

            correctedZ[i] = new Keyframe(zKeys[i].time, current.z, zKeys[i].inTangent, zKeys[i].outTangent, zKeys[i].inWeight, zKeys[i].outWeight);
            correctedZ[i].weightedMode = zKeys[i].weightedMode;
        }

        // Copy remaining keyframes
        for (int i = minCount; i < xKeys.Length; i++)
        {
            correctedX[i] = xKeys[i];
        }
        for (int i = minCount; i < yKeys.Length; i++)
        {
            correctedY[i] = yKeys[i];
        }
        for (int i = minCount; i < zKeys.Length; i++)
        {
            correctedZ[i] = zKeys[i];
        }

        curves.x = new AnimationCurve(correctedX);
        curves.y = new AnimationCurve(correctedY);
        curves.z = new AnimationCurve(correctedZ);

        return true;
    }

    float GetRange(Keyframe[] keys)
    {
        if (keys.Length == 0) return 0;

        float min = keys[0].value;
        float max = keys[0].value;

        foreach (Keyframe key in keys)
        {
            if (key.value < min) min = key.value;
            if (key.value > max) max = key.value;
        }

        return max - min;
    }

    Vector3 FindBestRepresentation(Vector3 previous, Vector3 current, Keyframe[] xKeysSoFar, Keyframe[] yKeysSoFar, Keyframe[] zKeysSoFar, int currentIndex)
    {
        Vector3 bestMatch = current;
        float bestScore = float.MaxValue;

        // Try different offset combinations (±360 and ±180)
        for (int xOffset = -2; xOffset <= 2; xOffset++)
        {
            for (int yOffset = -2; yOffset <= 2; yOffset++)
            {
                for (int zOffset = -2; zOffset <= 2; zOffset++)
                {
                    Vector3 candidate = new Vector3(
                        current.x + xOffset * 180f,
                        current.y + yOffset * 180f,
                        current.z + zOffset * 180f
                    );

                    // Calculate score based on:
                    // 1. Distance from previous (continuity)
                    // 2. Impact on total range
                    float continuityScore = Mathf.Abs(candidate.x - previous.x) +
                                          Mathf.Abs(candidate.y - previous.y) +
                                          Mathf.Abs(candidate.z - previous.z);

                    // Calculate what the new range would be with this candidate
                    float xMin = xKeysSoFar[0].value;
                    float xMax = xKeysSoFar[0].value;
                    float yMin = yKeysSoFar[0].value;
                    float yMax = yKeysSoFar[0].value;
                    float zMin = zKeysSoFar[0].value;
                    float zMax = zKeysSoFar[0].value;

                    for (int j = 1; j < currentIndex; j++)
                    {
                        if (xKeysSoFar[j].value < xMin) xMin = xKeysSoFar[j].value;
                        if (xKeysSoFar[j].value > xMax) xMax = xKeysSoFar[j].value;
                        if (yKeysSoFar[j].value < yMin) yMin = yKeysSoFar[j].value;
                        if (yKeysSoFar[j].value > yMax) yMax = yKeysSoFar[j].value;
                        if (zKeysSoFar[j].value < zMin) zMin = zKeysSoFar[j].value;
                        if (zKeysSoFar[j].value > zMax) zMax = zKeysSoFar[j].value;
                    }

                    xMin = Mathf.Min(xMin, candidate.x);
                    xMax = Mathf.Max(xMax, candidate.x);
                    yMin = Mathf.Min(yMin, candidate.y);
                    yMax = Mathf.Max(yMax, candidate.y);
                    zMin = Mathf.Min(zMin, candidate.z);
                    zMax = Mathf.Max(zMax, candidate.z);

                    float xRange = xMax - xMin;
                    float yRange = yMax - yMin;
                    float zRange = zMax - zMin;

                    // Penalize excessive ranges
                    float rangeScore = 0f;
                    if (xRange > maxRotationRange) rangeScore += (xRange - maxRotationRange) * 10f;
                    if (yRange > maxRotationRange) rangeScore += (yRange - maxRotationRange) * 10f;
                    if (zRange > maxRotationRange) rangeScore += (zRange - maxRotationRange) * 10f;

                    float totalScore = continuityScore + rangeScore;

                    if (totalScore < bestScore)
                    {
                        bestScore = totalScore;
                        bestMatch = candidate;
                    }
                }
            }
        }

        return bestMatch;
    }
}
