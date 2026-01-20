using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using ECG.Infrastructure;
using ECG.Presentation;
using ECG.Services;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ECG.Core
{
    /// <summary>
    /// DeckBuilder core loop:
    /// - Instantiate 15 cards into a face-down deck stack.
    /// - Clicking the top card animates it to "focus".
    /// - While focused, a click anywhere finishes and moves the card into the player's hand.
    /// - Repeat until the hand has 8 cards.
    /// - Then show Save/Discard buttons. Save persists deck to jsonbin (single bin shared by users).
    ///
    /// UI-only (Screen Space Overlay): everything is done with RectTransforms + DOTween.
    /// </summary>
    public class DeckBuilderController : MonoBehaviour
    {
        private enum State
        {
            ReadyToDraw,
            FocusingCard,
            Built
        }

        [Header("Cards")]
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private CardView cardPrefab;

        [Header("UI Anchors (Screen Space Overlay)")]
        [SerializeField] private RectTransform deckAnchor;
        [SerializeField] private RectTransform focusAnchor;
        [SerializeField] private RectTransform handAnchor;
        [SerializeField] private RectTransform cardsRoot;

        [Header("Deck Stack Visuals")]
        [SerializeField] private float pileOffset = 2f; // slight Y offset so the deck looks like a stack

        [Header("Hand Fan Layout (8 cards)")]
        [SerializeField] private float fanRadius = 1250f;     // larger radius => more spacing between cards
        [SerializeField] private float fanAngleTotal = 60f;   // total spread in degrees
        [SerializeField] private float tiltMax = 16f;         // max z-rotation on sides
        [SerializeField] private float pivotBelowYOffset = 0f; // pivot offset relative to handAnchor
        [SerializeField] private float relayoutTime = 0.25f;
        [SerializeField] private Ease relayoutEase = Ease.OutQuad;

        [Header("UI")]
        [SerializeField] private LoadingOverlay loading;
        [SerializeField] private Button saveDeckButton;
        [SerializeField] private Button discardDeckButton;
        [SerializeField] private TMP_Text errorText;

        [Header("Remote (jsonbin)")]
        [SerializeField] private string jsonBinId;
        [SerializeField] private string jsonBinMasterKey;

        // Runtime state
        private readonly Stack<CardView> _deck = new Stack<CardView>();
        private readonly List<CardView> _hand = new List<CardView>();
        private readonly List<CardView> _instantiatedRuntimeCards = new List<CardView>();

        private State _state = State.ReadyToDraw;
        private CardView _focusedCard;
        private JsonBinDeckStore _store;

        // Guards against race conditions when clicking fast during tweens.
        private bool _busy;
        private bool _canFinishFocus;

        private void Awake()
        {
            // Buttons are only visible once the deck is built.
            if (saveDeckButton) saveDeckButton.gameObject.SetActive(false);
            if (discardDeckButton) discardDeckButton.gameObject.SetActive(false);

            if (saveDeckButton) saveDeckButton.onClick.AddListener(async () => await SaveDeckAsync());
            if (discardDeckButton) discardDeckButton.onClick.AddListener(DiscardDeck);

            if (errorText) errorText.text = "";

            _store = new JsonBinDeckStore(new JsonBinClient(jsonBinId, jsonBinMasterKey));
        }

        private void Start()
        {
            EnsureUserId();
            BuildDeck15();
        }

        private void Update()
        {
            // While a card is in focus, a click anywhere finishes the focus and sends it to hand.
            if (_state != State.FocusingCard) return;
            if (_busy) return;
            if (!_canFinishFocus) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                FinishFocusToHand();
        }

        private void EnsureUserId()
        {
            // The test constraint: only store UUID locally (PlayerPrefs).
            if (!UserIdService.HasUserId())
                UserIdService.CreateUserId();
        }

        private void BuildDeck15()
        {
            // Clear all previous runtime objects/state.
            ClearSpawned();
            if (errorText) errorText.text = "";

            // Load and shuffle.
            var all = (cardDatabase != null && cardDatabase.AllCards != null)
                ? cardDatabase.AllCards.Where(c => c != null).ToList()
                : new List<CardData>();

            Shuffle(all);

            // Take up to 15 cards.
            var cards = all.Take(15).ToList();
            if (cards.Count == 0)
            {
                if (errorText) errorText.text = "CardDatabase is empty. Create CardData assets and assign them.";
                return;
            }

            // Instantiate in a stack (slight offset) under a shared root so anchored space matches.
            float y = 0f;
            for (int i = 0; i < cards.Count; i++)
            {
                var cardData = cards[i];
                var card = Instantiate(cardPrefab, cardsRoot);

                var rt = card.GetComponent<RectTransform>();
                rt.anchoredPosition = deckAnchor.anchoredPosition + new Vector2(0, y);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                // Bind data and force face-down.
                card.Bind(cardData);
                card.SetFaceUp(false, true);

                // All cards are clickable, but we only accept the top one in OnCardClicked.
                card.Clicked += OnCardClicked;

                // Ensure top-of-stack renders above.
                rt.SetAsLastSibling();

                _deck.Push(card);
                _instantiatedRuntimeCards.Add(card);

                y += pileOffset;
            }

            _hand.Clear();
            _focusedCard = null;
            _state = State.ReadyToDraw;

            _busy = false;
            _canFinishFocus = false;

            if (saveDeckButton) saveDeckButton.gameObject.SetActive(false);
            if (discardDeckButton) discardDeckButton.gameObject.SetActive(false);
        }

        private void OnCardClicked(CardView card)
        {
            if (_busy) return;
            if (_state != State.ReadyToDraw) return;
            if (_hand.Count >= 8) return;
            if (_deck.Count == 0) return;

            // Constraint: only the top card can be drawn.
            var top = _deck.Peek();
            if (card != top) return;

            DrawTopCardToFocus();
        }

        private void DrawTopCardToFocus()
        {
            if (_deck.Count == 0) return;

            _busy = true;
            _canFinishFocus = false;

            _focusedCard = _deck.Pop();
            _state = State.FocusingCard;

            // Prevent accidental re-entry clicks while tweening to focus.
            _focusedCard.Clicked -= OnCardClicked;

            // Animate to focus. Allow finishing only on the next frame
            // so the same click doesn't instantly close focus.
            _focusedCard.PlayToFocus(focusAnchor, focusScale: 1.5f).OnComplete(() =>
            {
                _busy = false;
                StartCoroutine(EnableFinishFocusNextFrame());
            });
        }

        private System.Collections.IEnumerator EnableFinishFocusNextFrame()
        {
            yield return null;
            _canFinishFocus = true;
        }

        private void FinishFocusToHand()
        {
            if (_focusedCard == null) return;
            if (_busy) return;

            _busy = true;

            var card = _focusedCard;
            _focusedCard = null;

            _state = State.ReadyToDraw;
            _canFinishFocus = false;

            // Move to hand pivot first (feels nicer), then relayout the full fan.
            Vector2 pivot = GetHandPivot();
            card.PlayToHand(pivot, finalScale: 1.0f).OnComplete(() =>
            {
                _busy = false;

                _hand.Add(card);

                // Re-enable click handler (safe; we still filter top deck card only).
                card.Clicked += OnCardClicked;

                // Layout all hand cards in a deterministic fan.
                LayoutHandFan();

                if (_hand.Count >= 8)
                    OnBuilt();
            });
        }

        /// <summary>
        /// Returns the fan pivot position for the hand.
        /// You can offset it to change how "curved" the fan looks.
        /// </summary>
        private Vector2 GetHandPivot()
        {
            return handAnchor.anchoredPosition + new Vector2(0f, pivotBelowYOffset);
        }

        /// <summary>
        /// Stable fan layout:
        /// - deterministic index order left->right (no sibling juggling).
        /// - uses circular arc around a pivot.
        /// </summary>
        private void LayoutHandFan()
        {
            int n = _hand.Count;
            if (n == 0) return;

            Vector2 pivot = GetHandPivot();

            float half = fanAngleTotal * 0.5f;
            float denom = Mathf.Max(1, n - 1);

            for (int i = 0; i < n; i++)
            {
                var card = _hand[i];
                if (card == null) continue;

                var rt = card.GetComponent<RectTransform>();

                float t = (denom <= 0.0001f) ? 0.5f : (i / denom);
                float angle = Mathf.Lerp(-half, half, t);
                float rad = angle * Mathf.Deg2Rad;

                // Arc around pivot:
                // - x spreads with sin
                // - y uses cos. We subtract radius so the center card sits near the handAnchor.
                float x = Mathf.Sin(rad) * fanRadius;
                float y = Mathf.Cos(rad) * fanRadius;
                Vector2 targetPos = pivot + new Vector2(x, y - fanRadius);

                // Rotate so the cards follow the arc.
                float z = -(angle / half) * tiltMax;
                Vector3 targetRot = new Vector3(0f, 0f, z);

                rt.DOKill();
                rt.DOAnchorPos(targetPos, relayoutTime).SetEase(relayoutEase);
                rt.DOLocalRotate(targetRot, relayoutTime).SetEase(relayoutEase);
                rt.DOScale(Vector3.one, relayoutTime).SetEase(relayoutEase);

                // Deterministic render order.
                rt.SetSiblingIndex(i);
            }
        }

        private void OnBuilt()
        {
            _state = State.Built;
            if (saveDeckButton) saveDeckButton.gameObject.SetActive(true);
            if (discardDeckButton) discardDeckButton.gameObject.SetActive(true);
        }

        private async Task SaveDeckAsync()
        {
            if (_hand.Count < 8) return;
            if (errorText) errorText.text = "";

            var userId = UserIdService.GetUserId();
            var deck8 = _hand.Take(8).Select(c => c.CardId).ToList();

            try
            {
                if (loading) loading.Show("Saving deck...");
                await _store.AppendDeckAsync(userId, deck8);
                if (loading) loading.Hide();

                // MVP: after saving, reset builder. (You can SceneLoader.Load DeckViewer if desired.)
                BuildDeck15();
            }
            catch (Exception e)
            {
                if (loading) loading.Hide();
                Debug.LogError(e);
                if (errorText) errorText.text = "Failed to save deck. Check internet / jsonbin keys.";
            }
        }

        private void DiscardDeck()
        {
            // MVP: local reset.
            BuildDeck15();
        }

        private void ClearSpawned()
        {
            foreach (var c in _instantiatedRuntimeCards)
            {
                if (c == null) continue;
                c.Clicked -= OnCardClicked;
                Destroy(c.gameObject);
            }

            _deck.Clear();
            _hand.Clear();
            _instantiatedRuntimeCards.Clear();
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var j = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
