using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deeplink
{
    public readonly struct DeeplinkData
    {
        public string RawUrl { get; }
        public string Scheme { get; }
        public string Host { get; }
        public string Path { get; }
        public IReadOnlyDictionary<string, string> Query { get; }
        public string Fragment { get; }

        public DeeplinkData(string rawUrl, string scheme, string host, string path,
            IReadOnlyDictionary<string, string> query, string fragment)
        {
            RawUrl = rawUrl;
            Scheme = scheme;
            Host = host;
            Path = path;
            Query = query;
            Fragment = fragment;
        }
    }

    public static class DeeplinkHandler
    {
        private static Action<DeeplinkData> _handler;

        public static void SetHandler(Action<DeeplinkData> handler)
        {
            _handler = handler;
        }

        public static void Handle(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            // UriFormatException is thrown naturally if url is invalid
            var uri = new Uri(url);

            var path = uri.AbsolutePath.Trim('/');

            Debug.LogWarning($"[DeeplinkHandler] url: {url}");
            Debug.LogWarning($"[DeeplinkHandler] Host: {uri.Host}");
            Debug.LogWarning($"[DeeplinkHandler] AbsolutePath: {uri.AbsolutePath}");
            Debug.LogWarning($"[DeeplinkHandler] Path: {path}");

            // Parse query string
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var q = uri.Query.TrimStart('?');
            if (!string.IsNullOrEmpty(q))
            {
                foreach (var pair in q.Split('&'))
                {
                    var idx = pair.IndexOf('=');
                    if (idx > 0)
                    {
                        var key = Uri.UnescapeDataString(pair.Substring(0, idx));
                        var value = Uri.UnescapeDataString(pair.Substring(idx + 1));
                        query[key] = value;
                    }
                    else if (pair.Length > 0)
                    {
                        query[Uri.UnescapeDataString(pair)] = string.Empty;
                    }
                }
            }

            var fragment = uri.Fragment.TrimStart('#');

            var data = new DeeplinkData(url, uri.Scheme, uri.Host, path, query, fragment);

            if (_handler != null)
            {
                _handler(data);
            }
            else
            {
                Debug.LogWarning($"[DeeplinkHandler] No handler set. Ignoring deeplink: {url}");
            }
        }
    }
}
