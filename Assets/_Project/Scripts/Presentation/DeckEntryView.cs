using TMPro;
using UnityEngine;

namespace ECG.Presentation
{
    /// <summary>
    /// DeckEntryView:
    /// - Small view/prefab helper representing one "deck row" in the deck viewer.
    /// - Contains:
    ///   - A header/title (deck name)
    ///   - A row container where CardView instances are instantiated
    ///
    /// This class is intentionally minimal: it only exposes references needed by controllers.
    /// </summary>
    public class DeckEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text deckName;
        [SerializeField] private RectTransform cardsRow;

        /// <summary>
        /// Header label that displays the deck name (e.g., "Deck #1").
        /// </summary>
        public TMP_Text DeckName => deckName;

        /// <summary>
        /// Parent transform where CardView instances should be spawned for this deck entry.
        /// </summary>
        public RectTransform CardsRow => cardsRow;
    }
}