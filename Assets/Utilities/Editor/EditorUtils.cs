//
// Copyright (c) 2024 Off The Beaten Track UG
// All rights reserved.
//
// Maintainer: Jens Bahr
//

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace Sparrow.Utilities
{
    /// <summary>
    /// Various functionality for building our Editors, ensuring everything looks the same
    /// </summary>
    public static class EditorUtils
    {
        private const int SpaceHeight = 6;
        private const int LineHeight = 2;
#pragma warning disable UDR0001 // Domain Reload Analyzer

        private static Texture2D _staticRectTexture;
        private static Color _staticColor = Color.white;

        private static Dictionary<string, Texture> s_ImageCache = new Dictionary<string, Texture>();
        private static Dictionary<string, Version> s_VersionCache = new Dictionary<string, Version>();
        private static Sprite[] sprites_light;
        private static Sprite[] sprites_dark;


#pragma warning restore UDR0001 // Domain Reload Analyzer


        public static void Separator(int height = LineHeight)
        {
            Space();
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            Space();
        }

        public static Texture LoadSpritesheet(string filename)
        {
            if (sprites_dark == null)
                sprites_dark = Resources.LoadAll<Sprite>("sparrow-iconset-dark");
            if (sprites_light == null)
                sprites_light = Resources.LoadAll<Sprite>("sparrow-iconset");

            string cacheName = (EditorGUIUtility.isProSkin ? "sprites_light" : "sprites_dark") + "_" + filename;

            if(s_ImageCache.ContainsKey(cacheName))
                if (s_ImageCache[cacheName] == null) 
                    s_ImageCache.Remove(cacheName);
            if (!s_ImageCache.ContainsKey(cacheName))
            {
                var sprite = Array.Find(EditorGUIUtility.isProSkin ? sprites_light : sprites_dark, spriteNew => spriteNew.name == filename);
                if (sprite == null) return null;
                s_ImageCache.Add(cacheName, GetSlicedSpriteTexture(sprite));
            }
            return s_ImageCache[cacheName];
        }

        public static Texture2D GetSlicedSpriteTexture(Sprite sprite)
        {
            Rect rect = sprite.rect;
            // Create a 20x20 texture for slightly bigger icons
            Texture2D slicedTex = new Texture2D(20, 20);
            slicedTex.filterMode = FilterMode.Bilinear;

            // Get the original pixels
            Color[] pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            
            // Resize pixels to 20x20
            Color[] resizedPixels = new Color[400]; // 20x20
            float xRatio = rect.width / 20f;
            float yRatio = rect.height / 20f;
            
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    int originalX = Mathf.FloorToInt(x * xRatio);
                    int originalY = Mathf.FloorToInt(y * yRatio);
                    resizedPixels[y * 20 + x] = pixels[originalY * (int)rect.width + originalX];
                }
            }
            
            slicedTex.SetPixels(resizedPixels);
            slicedTex.Apply();
            
            return slicedTex;
        }

        private static Texture LoadResource(string filename)
        {
            if (s_ImageCache.ContainsKey(filename)) return s_ImageCache[filename];
            var logo = Resources.Load<Texture2D>(filename);
            if (logo != null)
                s_ImageCache.Add(filename, logo);
            return logo;
        }

        public static void Space(int height = SpaceHeight)
        {
            EditorGUILayout.Space(height);
        }
        public static void DrawLogoHeader(string title, string logofile, string wikiURL = "", bool small = false, bool feedback = true, string colorHex = "#4B6584", string feedbackVersion = "", string updateCheckId = "") { 
            // draw the actual logo
            var logo = LoadResource(logofile);
            if (logo != null)
            {
                GUI.DrawTexture(new Rect(15, 5, 423f * (30f / 516f), 30), logo);
            }

            // change color
            Color color;
            var defaultColorBackground = GUI.backgroundColor;
            if (ColorUtility.TryParseHtmlString(colorHex, out color))  // replace "#0000FF" with your color hex
            {
                // draw text
                color.a = 1f;
                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.fontSize = (int)(style.fontSize * (1.5f));
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = color;
                style.hover.textColor = color;
                GUI.Label(new Rect(logo == null ? 10 : 45, 7, EditorGUILayout.GetControlRect().width - 70, 25), title, style);

                // draw horizontal line
                if (_staticRectTexture == null || !_staticColor.Equals(color))
                {
                    _staticColor = color;
                    _staticRectTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _staticRectTexture.SetPixel(0, 0, color);
                    _staticRectTexture.Apply();
                }
                GUI.backgroundColor = color;
                float startPos = logo == null ? 10 : (423f * (30f / 516f) + 15) + 10;
                var rect = new Rect(startPos, 33, EditorGUILayout.GetControlRect().width - startPos, 2);
                GUI.DrawTexture(rect, _staticRectTexture);
            }

            // create a smaller font size for the buttons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 10;
            float buttonWidth = 90; 
            float buttonHeight = 20; 
            if (feedback)
            {
                if (GUI.Button(new Rect(EditorGUILayout.GetControlRect().width - buttonWidth - (!string.IsNullOrEmpty(wikiURL) ? buttonWidth + 10 : 0), 40, buttonWidth, buttonHeight), "Give feedback", buttonStyle))
                {
                    FeedbackWindow.ShowWindow(title, feedbackVersion);
                }
            }
            if (!string.IsNullOrEmpty(wikiURL))
            {
                if (GUI.Button(new Rect(EditorGUILayout.GetControlRect().width - buttonWidth, 40, buttonWidth, buttonHeight), "Documentation", buttonStyle))
                {
                    Application.OpenURL(wikiURL+"?utm_content=unity-editor");
                }
            }

            if (!string.IsNullOrEmpty(updateCheckId) && (EditorUtils.IsUpdateAvailable(updateCheckId, feedbackVersion)))
            {
                if (GUI.Button(new Rect(EditorGUILayout.GetControlRect().width - buttonWidth - (!string.IsNullOrEmpty(wikiURL) ? buttonWidth + 10 : 0) - (feedback ? buttonWidth + 10 : 0)-15, 40, buttonWidth + 15, buttonHeight), "Update available", buttonStyle))
                {
                    Application.OpenURL("https://assetstore.unity.com/packages/slug/" + updateCheckId);
                }
            }
            GUI.backgroundColor = defaultColorBackground;
        }

        public static string FindAssetPath<T>() where T : ScriptableObject
        {
            // This method searches for an asset of type T and returns its path
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return string.Empty;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static bool IsUpdateAvailable(string assetID, string version)
        {
            if(s_VersionCache.ContainsKey(assetID))
                return s_VersionCache[assetID] > new Version(version);

            s_VersionCache.Add(assetID, new Version("0.0.0"));
            _ = GetLatestAssetStoreToolsVersion(assetID);
            return false;
        }

        [Serializable]
        private class AssetStoreVersionReply
        {
            public string version;
        }

        public static async System.Threading.Tasks.Task GetLatestAssetStoreToolsVersion(string assetID)
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                var url = $"https://api.assetstore.unity3d.com/package/latest-version/{assetID}";
                var result = await httpClient.GetAsync(url);

                result.EnsureSuccessStatusCode();
                var resultStr = await result.Content.ReadAsStringAsync();
                var json = JsonUtility.FromJson<AssetStoreVersionReply>(resultStr);
                httpClient.Dispose();
                s_VersionCache[assetID] = new Version(json.version);
            }
            catch (Exception e)
            {
                httpClient.Dispose();
                Debug.LogException(e);
                s_VersionCache[assetID] = new Version("0.0.0");
            }
        }

        public static void DrawWikiLinkButtonHere(string url)
        {
            EditorGUILayout.Space();
            Rect rect = GUILayoutUtility.GetLastRect();

            if (GUI.Button(new Rect(rect.x + rect.width - 30, rect.y - 15, 25, 25), LabelWithIcon("", "question")))
                Application.OpenURL(url);
        }
        public static void DrawWikiLinkButton(string url)
        {
            if (GUILayout.Button(LabelWithIcon("", "question"), GUILayout.Width(25), GUILayout.Height(25)))
                Application.OpenURL(url);
        }


        public static GUIContent LabelWithIcon(string txt, string iconFilename, string tooltip = "", bool skipIconSize = false)
        {
            var texture = LoadSpritesheet(iconFilename);
            return new GUIContent(txt, texture, tooltip);
        }

        public static void Label(string txt, MessageType messageType = MessageType.None)
        {
            GUIStyle textStyle = new GUIStyle(EditorStyles.label);
            textStyle.wordWrap = true;
            textStyle.padding = new RectOffset(4, 4, 2, 2); // Add some padding around the text
            textStyle.margin = new RectOffset(0, 0, 0, 0); // Remove any margin
            EditorGUILayout.LabelField(LabelMessageType(txt, messageType), textStyle);
        }

        public static void BoldLabel(string v, MessageType messageType = MessageType.None)
        {
            GUIStyle boldLabel = EditorStyles.boldLabel;
            boldLabel.wordWrap = true;
            GUILayout.Label(LabelMessageType(v, messageType), boldLabel);
        }
        public static void SmallLabel(string v, bool box = false, MessageType messageType = MessageType.None)
        {
            if(box) EditorGUILayout.BeginVertical("box");
            GUIStyle miniLabel = EditorStyles.miniLabel;
            miniLabel.wordWrap = true;
            GUILayout.Label(LabelMessageType(v, messageType), miniLabel);
            if (box) EditorGUILayout.EndVertical();
        }

        public static GUIContent LabelMessageType(string v, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    return LabelWithIcon(v, "info");
                case MessageType.Warning:
                    return LabelWithIcon(v, "warning");
                case MessageType.Error:
                    return LabelWithIcon(v, "x");
                default:
                    return new GUIContent(v);
            }
        }

        public static bool ColoredButtonToggle(bool on, Color onColor, string caption, out bool changed)
        {
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = on ? onColor : originalBgColor;
            bool ret = GUILayout.Toggle(on, new GUIContent(caption), "Button");
            GUI.backgroundColor = originalBgColor;
            changed = on != ret;
            return ret;
        }
        public static void DrawSectionHeader(string title, bool small = false)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.fontSize = (int)(style.fontSize * (small ? 1.5f : 2f));
            style.fontStyle = FontStyle.Bold;
            GUILayout.Label(title, style);
        }

        public static void DrawTwoPropertyEditors(SerializedObject obj, string property1, string property2)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(obj.FindProperty(property1));
            EditorGUILayout.PropertyField(obj.FindProperty(property2));
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawPropertyEditor(SerializedObject obj, string property)
        {
            var pro = obj.FindProperty(property);
            if(pro == null)
            {
                Debug.LogError("Trying to draw undefined property editor: " + property);
            } else
            {
                EditorGUILayout.PropertyField(pro);
            }
        }

        public static GameObject FindInChildren(Transform parent, string name, string parentName, bool strict = false)
        {
            foreach (Transform child in parent)
            {
                if (strict
                    ? (child.name == (name) && parent.name == (parentName))
                    : (child.name.ToLower().Contains(name.ToLower()) &&
                       parent.name.ToLower().Contains(parentName.ToLower())))
                {
                    return child.gameObject;
                }

                GameObject childFound = FindInChildren(child, name, parentName, strict);
                if (childFound != null)
                    return childFound;
            }

            return null;
        }

        public static bool ToggleFoldout(this SerializedObject serializedObject, string propertyName, string caption)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property.propertyType != SerializedPropertyType.Boolean)
                throw new ArgumentException("Serialized Property of Type Boolean expected.");

            GUIStyle pushButton = new GUIStyle(EditorStyles.miniButtonRight);
            if (property.boolValue)
            {
                pushButton.fontStyle = FontStyle.Bold;
            }

            DrawPropertyField(serializedObject, propertyName, caption);

            return property.boolValue;
        }

        public static void DrawPropertyField(this SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property);
        }

        public static void DrawPropertyField(this SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }
}
#endif
