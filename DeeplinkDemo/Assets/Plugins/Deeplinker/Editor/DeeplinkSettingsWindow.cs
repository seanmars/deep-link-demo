using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Deeplink;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Deeplink.Editor
{
    /// <summary>
    /// Tools → Deeplink → Settings
    /// 統一管理 Android / iOS Deep link 設定，並支援 Play Mode 測試。
    /// </summary>
    public class DeeplinkSettingsWindow : EditorWindow
    {
        // ── 序列化狀態 ────────────────────────────────────────────────────

        private DeeplinkConfig _config;
        private SerializedObject _so;
        private SerializedProperty _propAndroidScheme;
        private SerializedProperty _propAndroidHost;
        private SerializedProperty _propUseGameActivity;
        private SerializedProperty _propIosSchemes;

        // ── UI 狀態 ───────────────────────────────────────────────────────

        private ReorderableList _iosList;
        private Vector2 _scroll;
        private bool _foldAndroid = true;
        private bool _foldIos = true;
        private bool _foldTest = true;

        private string _testUrl = "myapp://item/42";
        private string _statusMsg = "";
        private bool _statusIsError;

        // ── 路徑常數 ──────────────────────────────────────────────────────

        private const string ManifestAssetPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";

        // ══════════════════════════════════════════════════════════════════
        //  選單 & 開啟
        // ══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Deeplink/Settings", false, 100)]
        public static void Open()
        {
            var w = GetWindow<DeeplinkSettingsWindow>("Deeplink Settings");
            w.minSize = new Vector2(440, 560);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Lifecycle
        // ══════════════════════════════════════════════════════════════════

        private void OnEnable() => TryLoadConfig();

        private void TryLoadConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<DeeplinkConfig>(DeeplinkConfig.AssetPath);
            if (_config != null) BindSerializedObject();
        }

        private void BindSerializedObject()
        {
            _so = new SerializedObject(_config);
            _propAndroidScheme  = _so.FindProperty("androidScheme");
            _propAndroidHost    = _so.FindProperty("androidHost");
            _propUseGameActivity = _so.FindProperty("useGameActivity");
            _propIosSchemes     = _so.FindProperty("iosUrlSchemes");
            BuildIOSList();
        }

        private void BuildIOSList()
        {
            _iosList = new ReorderableList(_so, _propIosSchemes, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "URL Schemes（不含 ://）"),

                drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, _propIosSchemes.GetArrayElementAtIndex(index), GUIContent.none);
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════
        //  OnGUI 主體
        // ══════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            DrawConfigHeader();
            if (_config == null) return;

            _so.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawAndroidSection();
            Space();
            DrawIOSSection();
            Space();
            EditorGUILayout.EndScrollView();

            _so.ApplyModifiedProperties();

            DrawStatusBar();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Config Asset Header
        // ══════════════════════════════════════════════════════════════════

        private void DrawConfigHeader()
        {
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Config Asset", EditorStyles.boldLabel, GUILayout.Width(88));

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(_config, typeof(DeeplinkConfig), false);

                if (_config == null && GUILayout.Button("建立", GUILayout.Width(48)))
                    CreateConfig();
            }

            EditorGUILayout.Space(4);
            Separator();
        }

        private void CreateConfig()
        {
            var dir = Path.GetDirectoryName(DeeplinkConfig.AssetPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var asset = CreateInstance<DeeplinkConfig>();
            AssetDatabase.CreateAsset(asset, DeeplinkConfig.AssetPath);
            AssetDatabase.SaveAssets();

            _config = asset;
            BindSerializedObject();
            SetStatus("已建立 DeeplinkConfig.asset", false);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Android Section
        // ══════════════════════════════════════════════════════════════════

        private void DrawAndroidSection()
        {
            _foldAndroid = EditorGUILayout.BeginFoldoutHeaderGroup(_foldAndroid, "Android");
            if (_foldAndroid)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(
                    _propAndroidScheme,
                    new GUIContent("Scheme", "URL Scheme，不含 ://，例如 myapp"));

                EditorGUILayout.PropertyField(
                    _propAndroidHost,
                    new GUIContent("Host（選填）", "限定 host，留空表示不限制，例如 open"));

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Activity 類型", EditorStyles.boldLabel);

                bool useGame = _propUseGameActivity.boolValue;

                EditorGUI.indentLevel++;
                bool newUseGame = EditorGUILayout.ToggleLeft(
                    "UnityPlayerGameActivity", useGame);
                bool newUseLegacy = EditorGUILayout.ToggleLeft(
                    "UnityPlayerActivity", !useGame);
                EditorGUI.indentLevel--;

                // 互斥切換
                if (newUseGame != useGame)      _propUseGameActivity.boolValue = true;
                if (newUseLegacy == useGame)    _propUseGameActivity.boolValue = false;

                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("從 AndroidManifest 讀取", GUILayout.Width(190)))
                        ReadFromManifest();
                    GUILayout.Space(4);
                    if (GUILayout.Button("套用至 AndroidManifest", GUILayout.Width(190)))
                    {
                        _so.ApplyModifiedProperties();
                        ApplyToManifest();
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 從 AndroidManifest 讀入 ────────────────────────────────────────

        private void ReadFromManifest()
        {
            var path = GetManifestFullPath();
            if (!File.Exists(path))
            {
                SetStatus("找不到 AndroidManifest.xml，請先建立或套用一次", true);
                return;
            }

            try
            {
                XNamespace ns = AndroidNs;
                var doc = XDocument.Load(path);

                // Scheme / Host
                var data = doc.Descendants("data")
                    .FirstOrDefault(e => e.Attribute(ns + "scheme") != null);
                if (data != null)
                {
                    _propAndroidScheme.stringValue = data.Attribute(ns + "scheme")?.Value ?? "myapp";
                    _propAndroidHost.stringValue   = data.Attribute(ns + "host")?.Value ?? "";
                }

                // Activity 類型
                var activity = doc.Descendants("activity").FirstOrDefault();
                if (activity != null)
                {
                    var name = activity.Attribute(ns + "name")?.Value ?? "";
                    _propUseGameActivity.boolValue = !name.Equals(
                        "com.unity3d.player.UnityPlayerActivity",
                        StringComparison.OrdinalIgnoreCase);
                }

                SetStatus("已從 AndroidManifest.xml 讀取設定", false);
            }
            catch (Exception ex)
            {
                SetStatus($"讀取失敗：{ex.Message}", true);
            }
        }

        // ── 套用至 AndroidManifest ─────────────────────────────────────────

        private void ApplyToManifest()
        {
            var path = GetManifestFullPath();
            var dir  = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            try
            {
                var doc = File.Exists(path)
                    ? XDocument.Load(path)
                    : BuildDefaultManifest();

                XNamespace ns = AndroidNs;

                // Activity 名稱
                var activity = doc.Descendants("activity").FirstOrDefault();
                if (activity != null)
                {
                    activity.SetAttributeValue(ns + "name",
                        _config.useGameActivity
                            ? "com.unity3d.player.UnityPlayerGameActivity"
                            : "com.unity3d.player.UnityPlayerActivity");
                }

                // VIEW intent-filter
                var viewFilter = doc.Descendants("intent-filter").FirstOrDefault(f =>
                    f.Descendants("action")
                     .Any(a => a.Attribute(ns + "name")?.Value == "android.intent.action.VIEW"));

                if (viewFilter == null)
                {
                    viewFilter = new XElement("intent-filter",
                        new XElement("action",   new XAttribute(ns + "name", "android.intent.action.VIEW")),
                        new XElement("category", new XAttribute(ns + "name", "android.intent.category.DEFAULT")),
                        new XElement("category", new XAttribute(ns + "name", "android.intent.category.BROWSABLE")));
                    activity?.Add(viewFilter);
                }

                // <data> 元素
                var data = viewFilter.Element("data") ?? new XElement("data");
                if (viewFilter.Element("data") == null) viewFilter.Add(data);

                data.SetAttributeValue(ns + "scheme", _config.androidScheme);

                if (!string.IsNullOrWhiteSpace(_config.androidHost))
                    data.SetAttributeValue(ns + "host", _config.androidHost);
                else
                    data.Attribute(ns + "host")?.Remove();

                var settings = new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = new System.Text.UTF8Encoding(false)
                };
                using (var writer = System.Xml.XmlWriter.Create(path, settings))
                    doc.Save(writer);

                AssetDatabase.Refresh();
                SetStatus($"已套用至 AndroidManifest.xml（scheme: {_config.androidScheme}）", false);
            }
            catch (Exception ex)
            {
                SetStatus($"套用失敗：{ex.Message}", true);
            }
        }

        private XDocument BuildDefaultManifest()
        {
            XNamespace ns = AndroidNs;
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("manifest",
                    new XAttribute(XNamespace.Xmlns + "android", AndroidNs),
                    new XAttribute("package", PlayerSettings.applicationIdentifier),
                    new XElement("application",
                        new XElement("activity",
                            new XAttribute(ns + "name", "com.unity3d.player.UnityPlayerGameActivity"),
                            new XAttribute(ns + "exported", "true"),
                            new XElement("intent-filter",
                                new XElement("action",   new XAttribute(ns + "name", "android.intent.action.MAIN")),
                                new XElement("category", new XAttribute(ns + "name", "android.intent.category.LAUNCHER")))))));
        }

        // ══════════════════════════════════════════════════════════════════
        //  iOS Section
        // ══════════════════════════════════════════════════════════════════

        private void DrawIOSSection()
        {
            _foldIos = EditorGUILayout.BeginFoldoutHeaderGroup(_foldIos, "iOS");
            if (_foldIos)
            {
                EditorGUI.indentLevel++;

                _iosList.DoLayoutList();

                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(
                    "Build iOS 時，DeeplinkIOSPostProcess 會自動將上方 Schemes 寫入 Xcode Info.plist 的 CFBundleURLTypes。",
                    MessageType.Info);

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("同步 Android Scheme → iOS 清單", GUILayout.Width(230)))
                        SyncAndroidSchemeToIOS();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void SyncAndroidSchemeToIOS()
        {
            _so.ApplyModifiedProperties();
            var scheme = _config.androidScheme;

            if (!_config.iosUrlSchemes.Contains(scheme))
            {
                _propIosSchemes.InsertArrayElementAtIndex(_propIosSchemes.arraySize);
                _propIosSchemes.GetArrayElementAtIndex(_propIosSchemes.arraySize - 1).stringValue = scheme;
                SetStatus($"已將 \"{scheme}\" 加入 iOS URL Schemes 清單", false);
            }
            else
            {
                SetStatus($"\"{scheme}\" 已存在於 iOS 清單中", false);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  狀態列
        // ══════════════════════════════════════════════════════════════════

        private void DrawStatusBar()
        {
            if (string.IsNullOrEmpty(_statusMsg)) return;
            Separator();
            EditorGUILayout.HelpBox(_statusMsg, _statusIsError ? MessageType.Error : MessageType.Info);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════════════

        private void SetStatus(string msg, bool isError)
        {
            _statusMsg     = msg;
            _statusIsError = isError;
            Repaint();
        }

        private static void Separator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private static void Space() => EditorGUILayout.Space(4);

        private static string GetManifestFullPath() =>
            Path.GetFullPath(ManifestAssetPath);
    }
}
