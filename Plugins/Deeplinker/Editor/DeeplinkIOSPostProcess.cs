#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using Deeplink;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Deeplink.Editor
{
    /// <summary>
    /// iOS build 後自動處理：將 DeeplinkConfig 中的 iosUrlSchemes
    /// 寫入 Xcode 專案的 Info.plist（CFBundleURLTypes）。
    /// </summary>
    public static class DeeplinkIOSPostProcess
    {
        // priority 50：在 Unity 預設處理（100）之前執行
        [PostProcessBuild(50)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var config = AssetDatabase.LoadAssetAtPath<DeeplinkConfig>(DeeplinkConfig.AssetPath);
            if (config == null)
            {
                Debug.LogWarning("[Deeplink] DeeplinkConfig.asset 不存在，跳過 iOS post-process。" +
                                 "請在 Tools → Deeplink → Settings 中建立設定檔。");
                return;
            }

            if (config.iosUrlSchemes == null || config.iosUrlSchemes.Count == 0)
            {
                Debug.Log("[Deeplink] iOS URL Schemes 清單為空，跳過 Info.plist 修改。");
                return;
            }

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var root = plist.root;

            // 取得或建立 CFBundleURLTypes 陣列
            PlistElementArray urlTypes = root.values.ContainsKey("CFBundleURLTypes")
                ? root["CFBundleURLTypes"].AsArray()
                : root.CreateArray("CFBundleURLTypes");

            // 收集已存在的 scheme，避免重複寫入
            var existingSchemes = new HashSet<string>();
            foreach (var elem in urlTypes.values)
            {
                var dict = elem.AsDict();
                if (dict == null || !dict.values.ContainsKey("CFBundleURLSchemes")) continue;
                foreach (var s in dict.values["CFBundleURLSchemes"].AsArray().values)
                    existingSchemes.Add(s.AsString());
            }

            // 寫入新的 scheme
            foreach (var scheme in config.iosUrlSchemes)
            {
                if (string.IsNullOrWhiteSpace(scheme) || existingSchemes.Contains(scheme)) continue;

                var urlDict      = urlTypes.AddDict();
                var schemesArray = urlDict.CreateArray("CFBundleURLSchemes");
                schemesArray.AddString(scheme);

                // CFBundleURLName 建議填 Bundle Identifier，以利識別
                urlDict.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);

                Debug.Log($"[Deeplink] 已新增 iOS URL Scheme：{scheme}");
            }

            plist.WriteToFile(plistPath);
            Debug.Log("[Deeplink] Info.plist 更新完成。");
        }
    }
}
#endif
