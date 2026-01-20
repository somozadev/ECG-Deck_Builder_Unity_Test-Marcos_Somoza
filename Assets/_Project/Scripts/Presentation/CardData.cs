using UnityEngine;
using UnityEngine.Serialization;

namespace ECG.Presentation
{
    /// <summary>
    /// CardData (ScriptableObject):
    /// - Authoring-time container for a single card's static data.
    /// - Instances are created as assets and referenced by runtime systems (e.g., CardDatabase).
    ///
    /// Fields are serialized so designers can edit them in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/Card Data", fileName = "CardData")]
    public class CardData : ScriptableObject
    {
        /// <summary>
        /// Numeric identifier used to look up cards quickly (e.g., in CardDatabase).
        /// </summary>
        [SerializeField] public int Id;

        /// <summary>
        /// Card stats displayed in UI (format based on the game, e.g., "ATK 2 / HP 3").
        /// </summary>
        [SerializeField] public string Stats;

        /// <summary>
        /// Display name of the card.
        /// </summary>
        [SerializeField] public string Name;

        /// <summary>
        /// Card cost value (energy/mana/etc.).
        /// </summary>
        [SerializeField] public int Cost;

        /// <summary>
        /// Card description / rules text. TextArea makes editing nicer in the Inspector.
        /// </summary>
        [SerializeField, TextArea(2, 6)] public string Description;

        /// <summary>
        /// Card art shown on the front of the card.
        /// </summary>
        [SerializeField] public Sprite Artwork;
    }
}
