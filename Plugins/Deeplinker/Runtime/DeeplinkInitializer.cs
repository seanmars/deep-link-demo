using System;
using UnityEngine;

namespace Deeplink
{
    public static class DeeplinkInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Application.deepLinkActivated += url =>
            {
                try
                {
                    DeeplinkHandler.Handle(url);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DeeplinkInitializer] Failed to handle deeplink: {ex}");
                }
            };

            // Cold start: app was opened directly by a deeplink
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                try
                {
                    DeeplinkHandler.Handle(Application.absoluteURL);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DeeplinkInitializer] Failed to handle cold-start deeplink: {ex}");
                }
            }
        }
    }
}
