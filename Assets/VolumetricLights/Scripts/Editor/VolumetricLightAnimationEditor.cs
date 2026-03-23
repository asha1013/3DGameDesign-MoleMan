// 
// Copyright (c) 2023 Off The Beaten Track UG
// All rights reserved.
// 
// Maintainer: Jens Bahr
// 

using UnityEditor;
using Sparrow.Utilities;

namespace Sparrow.VolumetricLight.Editor
{
    /*
     * formatting volumetric light profiles
     */
    [CustomEditor(typeof(VolumetricLightAnimation))]
    public class VolumetricLightAnimationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Check if target is null to prevent SerializedObjectNotCreatableException
            if (target == null)
            {
                EditorGUILayout.HelpBox("Target object has been destroyed or removed.", MessageType.Warning);
                return;
            }

            EditorUtils.DrawLogoHeader("Volumetric Light Animation", "volumetric_light_logo", "https://www.sparrow.tools/docs/volumetric-light-system/animating-via-component/", true, true, VolumetricLight.hexColor, VolumetricLight.version, VolumetricLight.assetID);
            DrawDefaultInspector();
        }
    }
}
