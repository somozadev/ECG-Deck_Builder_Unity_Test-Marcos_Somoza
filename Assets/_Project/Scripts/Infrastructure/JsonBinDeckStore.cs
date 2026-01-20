using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ECG.Infrastructure
{
    /// <summary>
    /// Persistence layer on top of JsonBinClient.
    ///
    /// IMPORTANT:
    /// Unity JsonUtility does NOT serialize nested List<List<string>> reliably.
    /// We use a DTO structure that JsonUtility supports:
    /// - Root.users: List<UserRecord>
    /// - UserRecord.decks: List<DeckDto>
    /// - DeckDto.cards: string[] (8 items)
    ///
    /// Data format (single global bin for all users):
    /// {
    ///   "users": [
    ///     {
    ///       "user_id": "...",
    ///       "decks": [
    ///         { "cards": ["card_id_1", ...] }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </summary>
    public sealed class JsonBinDeckStore
    {
        [Serializable]
        public class Root
        {
            public List<UserRecord> users = new List<UserRecord>();
        }

        [Serializable]
        public class UserRecord
        {
            public string user_id;
            public List<DeckDto> decks = new List<DeckDto>();
        }

        [Serializable]
        public class DeckDto
        {
            public string[] cards; // exactly 8 ids for this test
        }

        // jsonbin v3 wraps the record payload like { "record": { ... }, ... }
        [Serializable]
        private class Wrapper<T>
        {
            public T record;
        }

        private readonly JsonBinClient _client;

        public JsonBinDeckStore(JsonBinClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Loads the root object from jsonbin.
        /// Handles both wrapped (v3) and direct formats (if you paste plain JSON).
        /// </summary>
        public async Task<Root> LoadRootAsync()
        {
            var raw = await _client.GetLatestAsync();

            // 1) Try wrapper: { "record": { ... } }
            try
            {
                var wrapped = JsonUtility.FromJson<Wrapper<Root>>(raw);
                if (wrapped != null && wrapped.record != null)
                    return Normalize(wrapped.record);
            }
            catch
            {
                // ignore, try other formats
            }

            // 2) Try direct: { "users": [...] }
            try
            {
                var direct = JsonUtility.FromJson<Root>(raw);
                if (direct != null)
                    return Normalize(direct);
            }
            catch
            {
                // ignore
            }

            // 3) Fallback: empty
            return new Root { users = new List<UserRecord>() };
        }

        /// <summary>
        /// Saves the entire root object back to jsonbin (PUT overwrite).
        /// </summary>
        public async Task SaveRootAsync(Root root)
        {
            root = Normalize(root);
            var json = JsonUtility.ToJson(root, prettyPrint: true);
            await _client.PutAsync(json);
        }

        /// <summary>
        /// Append a new deck to the user's list (merge strategy).
        /// Creates the user record if it doesn't exist.
        /// </summary>
        public async Task AppendDeckAsync(string userId, List<string> deck8)
        {
            Debug.Log($"[Store] AppendDeck userId={userId} deckCount={deck8?.Count}");

            var root = await LoadRootAsync();

            var user = FindOrCreate(root, userId);
            user.decks ??= new List<DeckDto>();
            user.decks.Add(new DeckDto { cards = deck8.ToArray() });

            await SaveRootAsync(root);
        }

        /// <summary>
        /// Returns all decks for the given user as a list of string arrays.
        /// </summary>
        public async Task<List<string[]>> GetDecksForUserAsync(string userId)
        {
            var root = await LoadRootAsync();
            var user = FindOrNull(root, userId);

            var result = new List<string[]>();
            if (user == null || user.decks == null) return result;

            for (int i = 0; i < user.decks.Count; i++)
            {
                var d = user.decks[i];
                if (d?.cards != null) result.Add(d.cards);
            }

            return result;
        }

        private static UserRecord FindOrNull(Root root, string userId)
        {
            if (root?.users == null) return null;

            for (int i = 0; i < root.users.Count; i++)
            {
                var u = root.users[i];
                if (u != null && u.user_id == userId) return u;
            }
            return null;
        }

        private static UserRecord FindOrCreate(Root root, string userId)
        {
            var existing = FindOrNull(root, userId);
            if (existing != null) return existing;

            var created = new UserRecord
            {
                user_id = userId,
                decks = new List<DeckDto>()
            };
            root.users.Add(created);
            return created;
        }

        /// <summary>
        /// Ensures null-safe collections for JsonUtility + runtime usage.
        /// </summary>
        private static Root Normalize(Root r)
        {
            r ??= new Root();
            r.users ??= new List<UserRecord>();

            for (int i = 0; i < r.users.Count; i++)
            {
                if (r.users[i] == null) continue;
                r.users[i].decks ??= new List<DeckDto>();
            }

            return r;
        }
    }
}