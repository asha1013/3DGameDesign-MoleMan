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
    [CustomEditor(typeof(VolumetricLightProfile))]
    public class VolumetricLightProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Check if target is null to prevent SerializedObjectNotCreatableException
            if (target == null)
            {
                EditorGUILayout.HelpBox("Target object has been destroyed or removed.", MessageType.Warning);
                return;
            }

            EditorUtils.DrawLogoHeader("Volumetric Light Profile", "volumetric_light_logo", "https://www.sparrow.tools/docs/light-profiles/", true, true, VolumetricLight.hexColor, VolumetricLight.version, VolumetricLight.assetID);
            VolumetricLightProfile profile = target as VolumetricLightProfile;
            if (profile == null) return;
            
            var changes = LightSettingsEditor.DrawEditor(profile.settings);
            if (changes) EditorUtility.SetDirty(profile);
        }
    }
}
