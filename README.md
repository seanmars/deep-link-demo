# DeeplinkHandler

Unity 深度連結（Deeplink）路由系統，支援路徑參數與 Query String 解析，並自動橋接 Android / iOS 原生事件。

---

## 架構總覽

```
Assets/Scripts/Deeplink/
  ├── DeeplinkHandler.cs       ← 對外 API（Register / Unregister / Handle / ClearAll）
  ├── DeeplinkRoute.cs         ← 單一路由的模式比對與參數萃取
  └── DeeplinkInitializer.cs   ← 自動啟動，橋接 Application.deepLinkActivated

Assets/Plugins/Android/
  └── AndroidManifest.xml      ← Android Intent Filter 設定
```

### 元件職責

#### `DeeplinkRoute`

儲存單一路由的 URL 模式（pattern）與對應的 handler，並負責執行比對邏輯：

- 將 pattern 與傳入 URL 分別解析為 scheme、host、路徑段（path segments）、query params
- 路徑段逐一比對：`{param}` 形式的段視為萬用，並將實際值記錄進參數字典
- Query string 的 key=value 也合併進同一個字典，統一交給 handler

#### `DeeplinkHandler`

靜態核心類別，管理所有已註冊路由：

- 維護一份 `List<DeeplinkRoute>`
- `Handle(url)` 依序嘗試所有路由，第一個符合的執行並回傳 `true`

#### `DeeplinkInitializer`

使用 `[RuntimeInitializeOnLoadMethod]` 在遊戲啟動後自動執行：

- 訂閱 `Application.deepLinkActivated`（App 在前台時收到 deeplink）
- 檢查 `Application.absoluteURL`（cold start，App 被 deeplink 直接喚醒）

---

## 路由比對規則

| Pattern | URL | 結果參數 |
|---|---|---|
| `myapp://item/{id}` | `myapp://item/42` | `{ "id": "42" }` |
| `myapp://shop/{cat}` | `myapp://shop/shoes?page=2` | `{ "cat": "shoes", "page": "2" }` |
| `myapp://scene/{name}` | `myapp://scene/MainMenu` | `{ "name": "MainMenu" }` |

比對條件：

1. scheme 與 host 必須完全一致（大小寫不敏感）
2. 路徑段數必須相同
3. 非 `{param}` 的路徑段必須完全一致
4. query string 參數附加進結果字典（可與路徑參數同名，query 會覆蓋路徑參數）

---

## 安裝與平台設定

### Android

`Assets/Plugins/Android/AndroidManifest.xml` 已包含 Intent Filter：

```xml
<intent-filter>
  <action android:name="android.intent.action.VIEW" />
  <category android:name="android.intent.category.DEFAULT" />
  <category android:name="android.intent.category.BROWSABLE" />
  <data android:scheme="myapp" />   <!-- 改成你的 scheme -->
</intent-filter>
```

如需修改 scheme，將 `android:scheme="myapp"` 換成實際值即可。

### iOS

在 Unity Editor 中手動新增：

```
Edit → Project Settings → Player → iOS → Other Settings
  → Supported URL schemes → 點 + → 輸入 myapp（不含 ://）
```

---

## API 參考

### `DeeplinkHandler.Register`

```csharp
DeeplinkHandler.Register(string pattern, Action<Dictionary<string, string>> handler);
```

註冊一個 URL 模式與對應的處理函式。可在任意 `MonoBehaviour.Awake()` 或 `Start()` 中呼叫。

### `DeeplinkHandler.Unregister`

```csharp
DeeplinkHandler.Unregister(string pattern);
```

移除指定 pattern 的路由（以 pattern 字串完全匹配為準）。

### `DeeplinkHandler.Handle`

```csharp
bool DeeplinkHandler.Handle(string url);
```

手動觸發 deeplink 處理。通常由 `DeeplinkInitializer` 自動呼叫，也可用於測試。

- 回傳 `true`：找到匹配路由並執行
- 回傳 `false`：無匹配路由（會輸出 `LogWarning`）

### `DeeplinkHandler.ClearAll`

```csharp
DeeplinkHandler.ClearAll();
```

清除所有已註冊路由，適用於場景切換後需要重新設定路由的情境。

---

## 使用範例

### 基本路由

```csharp
using Deeplink;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppRouter : MonoBehaviour
{
    void Awake()
    {
        // 路徑參數
        DeeplinkHandler.Register("myapp://item/{id}", p =>
        {
            Debug.Log($"Open item: {p["id"]}");
            // 載入道具頁面...
        });

        // 場景跳轉
        DeeplinkHandler.Register("myapp://scene/{name}", p =>
        {
            SceneManager.LoadScene(p["name"]);
        });

        // 路徑參數 + Query String
        DeeplinkHandler.Register("myapp://shop/{category}", p =>
        {
            var category = p["category"];
            var page = p.ContainsKey("page") ? p["page"] : "1";
            Debug.Log($"Shop: {category}, page: {page}");
        });
    }
}
```

### 安全地讀取參數

```csharp
DeeplinkHandler.Register("myapp://profile/{userId}", p =>
{
    if (!p.TryGetValue("userId", out var userId)) return;
    // 使用 userId...
});
```

---

## 測試方式

### Editor（最快）

直接在 Play Mode 中呼叫 `Handle`，不需要實體裝置：

```csharp
// 在任意腳本或 Inspector 按鈕中：
DeeplinkHandler.Handle("myapp://item/99");
DeeplinkHandler.Handle("myapp://shop/shoes?page=3");
```

### Android

Build APK 後用 `adb` 發送 deeplink：

```bash
# 基本連結
adb shell am start -a android.intent.action.VIEW -d "myapp://item/42"

# 含 query string
adb shell am start -a android.intent.action.VIEW -d "myapp://shop/shoes?page=2"

# Cold start 測試（先殺掉 App 再觸發）
adb shell am force-stop com.yourcompany.yourgame
adb shell am start -a android.intent.action.VIEW -d "myapp://item/42"
```

觀察 Logcat：`adb logcat -s Unity`

### iOS

Build Xcode 專案後：

- 在 Safari 輸入 `myapp://item/42` → 點前往
- 或在備忘錄中輸入連結後長按開啟

---

## 注意事項

- **`DeeplinkInitializer` 不需手動掛載**，`[RuntimeInitializeOnLoadMethod]` 會自動在 AfterSceneLoad 執行
- **路由以先進先出（FIFO）順序比對**，第一個符合的路由勝出；若有衝突的 pattern 請注意註冊順序
- **`ClearAll` 後需重新呼叫 `Register`**，否則後續 deeplink 皆無法匹配
- Android `AndroidManifest.xml` 中的 `android:name` 預設為 `UnityPlayerGameActivity`；若你的專案使用舊版 Unity（2022 以前）可能需改為 `UnityPlayerActivity`
