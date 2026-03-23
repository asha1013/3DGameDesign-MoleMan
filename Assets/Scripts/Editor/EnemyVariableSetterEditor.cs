using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyVariableSetter))]
public class EnemyVariableSetterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyVariableSetter setter = (EnemyVariableSetter)target;

        if (GUILayout.Button("Create Missing Components"))
        {
            setter.CreateMissingComponents();
            // Save changes to prefab
            PrefabUtility.RecordPrefabInstancePropertyModifications(setter.gameObject);
            EditorUtility.SetDirty(setter.gameObject);
        }

        if (GUILayout.Button("Set Variables"))
        {
            setter.SetVariables();
            // Save changes to prefab
            PrefabUtility.RecordPrefabInstancePropertyModifications(setter.GetComponent<Enemy>());
            EditorUtility.SetDirty(setter.gameObject);
        }
    }
}
