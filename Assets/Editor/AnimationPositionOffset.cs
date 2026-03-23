using UnityEngine;
using UnityEditor;

public class AnimationPositionOffset : EditorWindow
{
    private AnimationClip animationClip;
    private Vector3 targetWorldPosition;
    private bool useRectTransform = false;

    [MenuItem("Tools/Animation Position Offset")]
    public static void ShowWindow()
    {
        GetWindow<AnimationPositionOffset>("Animation Position Offset");
    }

    void OnGUI()
    {
        GUILayout.Label("Offset Animation Position", EditorStyles.boldLabel);

        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        targetWorldPosition = EditorGUILayout.Vector3Field("Target World Position", targetWorldPosition);
        useRectTransform = EditorGUILayout.Toggle("Use RectTransform", useRectTransform);

        if (GUILayout.Button("Apply Offset"))
        {
            if (animationClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an animation clip.", "OK");
                return;
            }

            ApplyPositionOffset();
        }
    }

    void ApplyPositionOffset()
    {
        // Determine which property to modify
        string propertyX, propertyY, propertyZ;
        if (useRectTransform)
        {
            propertyX = "m_AnchoredPosition.x";
            propertyY = "m_AnchoredPosition.y";
            propertyZ = "m_AnchoredPosition.z";
        }
        else
        {
            propertyX = "m_LocalPosition.x";
            propertyY = "m_LocalPosition.y";
            propertyZ = "m_LocalPosition.z";
        }

        // Get the animation curves
        AnimationCurve curveX = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(RectTransform), propertyX));
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(RectTransform), propertyY));
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(RectTransform), propertyZ));

        // If RectTransform curves not found, try Transform
        if (curveX == null && !useRectTransform)
        {
            curveX = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(Transform), propertyX));
            curveY = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(Transform), propertyY));
            curveZ = AnimationUtility.GetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", typeof(Transform), propertyZ));
        }

        if (curveX == null || curveX.keys.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No position curves found for {(useRectTransform ? "RectTransform" : "Transform")}.", "OK");
            return;
        }

        // Get first keyframe values
        float originalX = curveX.keys.Length > 0 ? curveX.keys[0].value : 0;
        float originalY = curveY != null && curveY.keys.Length > 0 ? curveY.keys[0].value : 0;
        float originalZ = curveZ != null && curveZ.keys.Length > 0 ? curveZ.keys[0].value : 0;

        // Calculate offset
        Vector3 offset = targetWorldPosition - new Vector3(originalX, originalY, originalZ);

        // Apply offset to all keyframes
        curveX = OffsetCurve(curveX, offset.x);
        if (curveY != null) curveY = OffsetCurve(curveY, offset.y);
        if (curveZ != null) curveZ = OffsetCurve(curveZ, offset.z);

        // Set the modified curves back
        System.Type componentType = useRectTransform ? typeof(RectTransform) : typeof(Transform);
        AnimationUtility.SetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", componentType, propertyX), curveX);
        if (curveY != null)
            AnimationUtility.SetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", componentType, propertyY), curveY);
        if (curveZ != null)
            AnimationUtility.SetEditorCurve(animationClip, EditorCurveBinding.FloatCurve("", componentType, propertyZ), curveZ);

        EditorUtility.DisplayDialog("Success", $"Applied offset {offset} to animation clip.", "OK");
    }

    AnimationCurve OffsetCurve(AnimationCurve curve, float offset)
    {
        if (curve == null) return null;

        AnimationCurve newCurve = new AnimationCurve();
        foreach (Keyframe key in curve.keys)
        {
            Keyframe newKey = new Keyframe(key.time, key.value + offset, key.inTangent, key.outTangent, key.inWeight, key.outWeight);
            newKey.weightedMode = key.weightedMode;
            newCurve.AddKey(newKey);
        }

        return newCurve;
    }
}
