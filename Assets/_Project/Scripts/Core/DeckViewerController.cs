using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using ECG.Infrastructure;
using ECG.Presentation;
using ECG.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ECG.Core
{
    /// <summary>
    /// DeckViewer:
    /// - Loads decks from a single shared jsonbin (multi-user).
    /// - Renders a vertical list of deck entries (header + small cards row).
    /// - Click a card => creates a "ghost" CardView that animates to center (focus).
    /// - Click anywhere => ghost animates back and focus closes.
    ///
    /// Important: We DO NOT reparent or disable the original card,
    /// because doing so would trigger HorizontalLayoutGroup reflow and look clunky.
    /// Instead, we hide the original card with CanvasGroup alpha = 0.
    /// </summary>
    public class DeckViewerController : MonoBehaviour
    {
        [Header("Remote (jsonbin)")] [SerializeField]
        private string jsonBinId;

        [SerializeField] private string jsonBinMasterKey;

        [Header("Data")] [SerializeField] private CardDatabase cardDatabase;

        [Header("UI")] [SerializeField] private LoadingOverlay loading;
        [SerializeField] private TMP_Text errorText;

        [Header("Scroll")] [SerializeField] private RectTransform contentRoot; // ScrollRect/Viewport/Content
        [SerializeField] private DeckEntryView deckEntryPrefab;
        [SerializeField] private CardView cardViewPrefab;

        [Header("Focus")] [SerializeField] private RectTransform focusAnchor; // centered anchor (NOT inside ScrollView)
        [SerializeField] private ClickCatcherOverlay clickCatcher; // fullscreen transparent catcher (inactive by default)
        [SerializeField] private ScrollRect scrollRect; // optional; disable while focused to avoid drag

        private JsonBinDeckStore _store;

        private bool _busy;

        // Focus state (original card in the row)
        private CardView _focusedOriginal;
        private CanvasGroup _focusedOriginalCanvasGroup;

        // The animated clone that goes to the center and back
        private CardView _ghost;
        private RectTransform _ghostRt;

        private void Awake()
        {
            if (errorText) errorText.text = "";

            if (clickCatcher)
            {
                clickCatcher.gameObject.SetActive(false);
                clickCatcher.Clicked += CloseFocus;
            }
            Debug.Log($"[DeckViewer] Subscribed to clickCatcher: {clickCatcher.name} (instanceID {clickCatcher.GetInstanceID()})");

        }

        private async void Start()
        {
            if (!UserIdService.HasUserId())
            {
                if (errorText) errorText.text = "No user UUID found. Please create a new user first.";
                return;
            }

            if (cardDatabase == null)
            {
                if (errorText) errorText.text = "CardDatabase is required in DeckViewer to render CardViews.";
                return;
            }

            if (focusAnchor == null)
            {
                if (errorText) errorText.text = "Missing FocusAnchor reference.";
                return;
            }

            _store = new JsonBinDeckStore(new JsonBinClient(jsonBinId, jsonBinMasterKey));
            await LoadAndRenderAsync(UserIdService.GetUserId());
        }

        private async Task LoadAndRenderAsync(string userId)
        {
            try
            {
                if (loading) loading.Show("Fetching decks...");
                var decks = await _store.GetDecksForUserAsync(userId); // List<string[]>
                if (loading) loading.Hide();

                ClearContent();

                if (decks.Count == 0)
                {
                    if (errorText) errorText.text = "No decks saved yet.";
                    return;
                }

                for (int i = 0; i < decks.Count; i++)
                {
                    var entry = Instantiate(deckEntryPrefab, contentRoot);
                    entry.DeckName.text = $"Deck #{i + 1}";

                    var cardIds = decks[i];
                    if (cardIds == null) continue;

                    for (int k = 0; k < cardIds.Length; k++)
                    {
                        var cv = Instantiate(cardViewPrefab, entry.CardsRow);

                        // Bind card data
                        BindViewerCard(cv, cardIds[k]);

                        // Viewer cards should be face-up by default
                        cv.SetFaceUp(true, instant: true);

                        // Make them smaller in viewer
                        var rt = cv.GetComponent<RectTransform>();
                        rt.localScale = Vector3.one * 0.70f;

                        cv.Clicked += OnViewerCardClicked;
                    }
                }
            }
            catch (Exception e)
            {
                if (loading) loading.Hide();
                Debug.LogError(e);
                if (errorText) errorText.text = "Failed to load decks. Check internet / jsonbin keys.";
            }
        }

        private void OnViewerCardClicked(CardView card)
        {
            if (_busy) return;

            // If a focus is open, we close on click-anywhere via ClickCatcher.
            // Ignore card clicks while focused.
            if (_focusedOriginal != null) return;

            OpenFocus(card);
        }

        private void OpenFocus(CardView original)
        {
            _busy = true;
            _focusedOriginal = original;

            if (scrollRect) scrollRect.enabled = false;
            if (clickCatcher) clickCatcher.gameObject.SetActive(true);

            // Ensure the original card stays in the layout without reflow:
            // we hide it with CanvasGroup instead of disabling / reparenting.
            _focusedOriginalCanvasGroup = GetOrAddCanvasGroup(original.gameObject);
            _focusedOriginalCanvasGroup.alpha = 0f;
            _focusedOriginalCanvasGroup.interactable = false;
            _focusedOriginalCanvasGroup.blocksRaycasts = false;

            // Create ghost (clone) under the same parent as focusAnchor so anchored space matches.
            var ghostParent = focusAnchor.parent;
            _ghost = Instantiate(cardViewPrefab, ghostParent);
            _ghostRt = _ghost.GetComponent<RectTransform>();

            // Bind the same card data to the ghost using original.CardId (card_id_X)
            BindViewerCard(_ghost, original.CardId);
            _ghost.SetFaceUp(true, instant: true);

            // Place ghost exactly over the original card on screen to avoid snapping.
            var originalRt = original.GetComponent<RectTransform>();
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, originalRt.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)ghostParent, screenPos, null, out var localPoint);

            // Free/centered rect setup for focusing
            _ghostRt.anchorMin = _ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
            _ghostRt.pivot = new Vector2(0.5f, 0.5f);
            _ghostRt.anchoredPosition = localPoint;
            _ghostRt.localScale = originalRt.localScale;
            _ghostRt.localEulerAngles = originalRt.localEulerAngles;
            _ghostRt.SetAsLastSibling();

            // Clicking the ghost itself is not needed; ClickCatcher closes focus.
            // But we avoid accidental re-entrancy:
            _ghost.Clicked += _ =>
            {
                /* no-op */
            };

            // Animate ghost to center
            _ghost.PlayToFocus(focusAnchor, focusScale: 1.5f).OnComplete(() => { _busy = false; });
        }

        private void CloseFocus()
        {
            if (_focusedOriginal == null) return;
            if (_busy) return;

            _busy = true;

            // Compute the target position for the ghost: where the original card is.
            // Original card is invisible but still in layout, so its RectTransform position is valid.
            var originalRt = _focusedOriginal.GetComponent<RectTransform>();
            var ghostParent = (RectTransform)focusAnchor.parent;

            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, originalRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ghostParent, targetScreen, null, out var targetLocal);

            // Animate ghost back to the original spot
            float originalScale = originalRt.localScale.x;

            _ghost.PlayToHand(targetLocal, finalScale: originalScale).OnComplete(() =>
            {
                // Restore original visibility
                if (_focusedOriginalCanvasGroup != null)
                {
                    _focusedOriginalCanvasGroup.alpha = 1f;
                    _focusedOriginalCanvasGroup.interactable = true;
                    _focusedOriginalCanvasGroup.blocksRaycasts = true;
                }

                // Cleanup ghost
                if (_ghost != null) Destroy(_ghost.gameObject);

                _ghost = null;
                _ghostRt = null;

                _focusedOriginal = null;
                _focusedOriginalCanvasGroup = null;

                if (clickCatcher) clickCatcher.gameObject.SetActive(false);
                if (scrollRect) scrollRect.enabled = true;

                _busy = false;
            });
        }

        private void BindViewerCard(CardView cv, string cardId)
        {
            int numericId = TryParseCardNumericId(cardId);
            if (numericId <= 0)
                throw new Exception($"Invalid card id format: {cardId}");

            var data = cardDatabase.GetById(numericId);
            cv.Bind(data);
        }

        private static int TryParseCardNumericId(string cardId)
        {
            // Expected: "card_id_7"
            if (string.IsNullOrEmpty(cardId)) return -1;
            int idx = cardId.LastIndexOf('_');
            if (idx < 0 || idx >= cardId.Length - 1) return -1;
            return int.TryParse(cardId.Substring(idx + 1), out int n) ? n : -1;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }
    }
}