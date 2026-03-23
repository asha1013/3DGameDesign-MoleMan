//
// Copyright (c) 2023 Off The Beaten Track UG
// All rights reserved.
//
// Maintainer: Marc Freitag
//

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Sparrow.Utilities
{
    [Serializable]
    public class CustomPropertyField<T>
    {
        public bool toggle = false;
        public T value;

        public void OnInspectorGUI()
        {

        }

        public bool OnCustomInspectorChange(SerializedProperty serializedObjectValue, string label)
        {
            T oldValue = value;
            serializedObjectValue.serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            toggle = EditorGUILayout.Toggle(toggle, GUILayout.Width(20));
            EditorGUILayout.LabelField(label, GUILayout.Width(175));
            EditorGUILayout.PropertyField(serializedObjectValue, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            serializedObjectValue.serializedObject.ApplyModifiedProperties();
            return !oldValue.Equals(value);
        }

        public CustomPropertyField(T _value)
        {
            value = _value;
        }
    }
}
#endif
