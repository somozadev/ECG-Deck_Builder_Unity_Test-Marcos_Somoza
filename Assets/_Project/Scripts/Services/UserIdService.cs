using System;
using UnityEngine;

namespace ECG.Services
{
    /// <summary>
    /// UserIdService:
    /// - Stores a persistent per-device user UUID using Unity PlayerPrefs.
    /// - Provides helpers to check existence, read, and create a new UUID.
    ///
    /// Notes:
    /// - PlayerPrefs is local to the device/user profile (not cloud-synced by default).
    /// - This UUID is used as a simple identity key for loading/saving user data remotely.
    /// </summary>
    public static class UserIdService
    {
        /// <summary>
        /// PlayerPrefs key where the UUID is stored.
        /// </summary>
        private const string Key = "USER_UUID";

        /// <summary>
        /// Returns true if a non-empty UUID is already stored in PlayerPrefs.
        /// </summary>
        public static bool HasUserId()
        {
            return PlayerPrefs.HasKey(Key) && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(Key));
        }

        /// <summary>
        /// Reads the stored UUID from PlayerPrefs.
        /// Returns empty string if not present.
        /// </summary>
        public static string GetUserId()
        {
            return PlayerPrefs.GetString(Key, string.Empty);
        }

        /// <summary>
        /// Creates a new UUID and stores it in PlayerPrefs.
        /// Overwrites any existing value.
        /// </summary>
        public static void CreateUserId()
        {
            var uid = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(Key, uid);

            // Persist immediately to disk.
            PlayerPrefs.Save();
        }
    }
}