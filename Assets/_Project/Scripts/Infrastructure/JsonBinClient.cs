using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ECG.Infrastructure
{
    /// <summary>
    /// Minimal JSONBin.io HTTP client (API v3).
    /// - GET latest record
    /// - PUT overwrite record
    ///
    /// Notes:
    /// - For private bins you must send X-Master-Key.
    /// - UnityWebRequest runs async via polling the async operation.
    /// </summary>
    public sealed class JsonBinClient
    {
        private readonly string _binId;
        private readonly string _masterKey;

        // API v3:
        // GET  https://api.jsonbin.io/v3/b/{BIN_ID}/latest
        // PUT  https://api.jsonbin.io/v3/b/{BIN_ID}
        private const string BaseUrl = "https://api.jsonbin.io/v3/b/";

        public JsonBinClient(string binId, string masterKey)
        {
            _binId = (binId ?? string.Empty).Trim();
            _masterKey = (masterKey ?? string.Empty).Trim();

            // Keep logs lightweight. Avoid printing secrets.
            Debug.Log($"[JsonBin] BinId set (len={_binId.Length})");
            Debug.Log($"[JsonBin] MasterKey set (len={_masterKey.Length})");
        }

        /// <summary>
        /// Fetch the latest record snapshot.
        /// Returns raw JSON as returned by jsonbin v3 (includes wrapper fields).
        /// </summary>
        public async Task<string> GetLatestAsync()
        {
            var url = $"{BaseUrl}{_binId}/latest";
            using var req = UnityWebRequest.Get(url);
            ApplyHeaders(req);

            await Send(req);
            return req.downloadHandler.text;
        }

        /// <summary>
        /// Overwrite the bin record with the given JSON body.
        /// The body should be the "record" object you want to persist.
        /// </summary>
        public async Task<string> PutAsync(string jsonBody)
        {
            var url = $"{BaseUrl}{_binId}";
            var bytes = Encoding.UTF8.GetBytes(jsonBody);

            using var req = new UnityWebRequest(url, "PUT");
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyHeaders(req);

            await Send(req);
            return req.downloadHandler.text;
        }

        /// <summary>
        /// Apply authentication headers.
        /// For public bins, the master key can be empty.
        /// </summary>
        private void ApplyHeaders(UnityWebRequest req)
        {
            if (!string.IsNullOrWhiteSpace(_masterKey))
                req.SetRequestHeader("X-Master-Key", _masterKey);
        }

        /// <summary>
        /// Sends a UnityWebRequest and throws on non-success.
        /// Includes response body in the exception for debugging.
        /// </summary>
        private static async Task Send(UnityWebRequest req)
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                throw new Exception($"HTTP {(int)req.responseCode} {req.error}\n{body}");
            }
        }
    }
}