using System.Collections.Generic;
using UnityEngine;

namespace Deeplink
{
    /// <summary>
    /// Deep link 平台設定資源。
    /// 由 DeeplinkSettingsWindow (Editor) 管理；
    /// iOS build 時由 DeeplinkIOSPostProcess 讀取並寫入 Xcode Info.plist。
    /// </summary>
    [CreateAssetMenu(fileName = "DeeplinkConfig", menuName = "Deeplink/Config")]
    public class DeeplinkConfig : ScriptableObject
    {
        [Header("Android")]
        [Tooltip("URL Scheme，不含 ://，例如 myapp")]
        public string androidScheme = "myapp";

        [Tooltip("限定 Host（留空表示不限制），例如 open")]
        public string androidHost = "";

        [Tooltip("true = UnityPlayerGameActivity (Unity 2023+)；false = UnityPlayerActivity (舊版)")]
        public bool useGameActivity = true;

        [Header("iOS")]
        [Tooltip("Supported URL Schemes（不含 ://）。Build 時 DeeplinkIOSPostProcess 自動寫入 Info.plist")]
        public List<string> iosUrlSchemes = new List<string> { "myapp" };

        /// <summary>此資源在專案中的固定路徑，供 Editor 腳本存取。</summary>
        public const string AssetPath = "Assets/Settings/DeeplinkConfig.asset";
    }
}
