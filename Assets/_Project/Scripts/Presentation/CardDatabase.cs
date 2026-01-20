#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace ECG.Presentation
{
    /// <summary>
    /// CardDatabase (ScriptableObject):
    /// - Holds a list of CardData assets.
    /// - Builds a lookup dictionary (Id -> CardData) for fast runtime queries.
    ///
    /// Intended usage:
    /// - Assign this database to systems that need to resolve a card id into CardData,
    ///   such as deck rendering, hand building, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/Card Database", fileName = "CardDatabase")]
    public class CardDatabase : ScriptableObject
    {
        [Header("Cards")]
        [SerializeField] private List<CardData> cards = new List<CardData>();

        /// <summary>
        /// Exposes the raw list (useful for editor/debug).
        /// </summary>
        public List<CardData> AllCards => cards;

        /// <summary>
        /// Runtime lookup cache: Card Id -> CardData.
        /// Rebuilt on enable.
        /// </summary>
        private Dictionary<int, CardData>? byId;

        /// <summary>
        /// Unity calls OnEnable when the ScriptableObject is loaded/reloaded.
        /// We build the dictionary here so GetById() is O(1).
        /// </summary>
        private void OnEnable()
        {
            byId = new Dictionary<int, CardData>(cards.Count);

            foreach (var c in cards)
            {
                if (c == null) continue;
                byId[c.Id] = c;
            }
        }

        /// <summary>
        /// Returns the CardData for a given numeric id.
        /// Throws if the id doesn't exist in the database.
        /// </summary>
        public CardData GetById(int id)
        {
            // Defensive: ensure lookup exists even if OnEnable didn't run (rare, but safe).
            if (byId == null) OnEnable();

            if (byId!.TryGetValue(id, out var c)) return c;

            // Throwing is useful here because missing card data is usually a setup error.
            throw new KeyNotFoundException($"Card id not found: {id}");
        }
    }
}
