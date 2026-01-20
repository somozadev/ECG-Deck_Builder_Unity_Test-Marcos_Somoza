using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ECG.Presentation
{
    /// <summary>
    /// CardView (pure UI):
    /// - Clickable card UI (IPointerClickHandler).
    /// - Can show front/back states (simple "flip" by toggling roots).
    /// - Provides DOTween sequences to animate the card into focus or back into a hand slot.
    ///
    /// This class does NOT manage gameplay logic. It's strictly presentation/animation.
    /// </summary>
    public sealed class CardView : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private RectTransform rect;
        [SerializeField] private GameObject frontRoot;
        [SerializeField] private GameObject backRoot;
        [SerializeField] private Image frontImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text costText;

        [Header("Tween Settings")]
        [SerializeField] private float liftPx = 40f;
        [SerializeField] private float liftTime = 0.15f;
        [SerializeField] private float moveTime = 0.25f;
        [SerializeField] private float flipTime = 0.20f;

        /// <summary>
        /// Persisted id format used by the project (e.g., "card_id_7").
        /// Set during Bind().
        /// </summary>
        public string CardId { get; private set; }

        /// <summary>
        /// Raised when the card is clicked.
        /// Consumers decide what "click" means (select, focus, play, etc.).
        /// </summary>
        public event Action<CardView> Clicked;

        private void Reset()
        {
            // Auto-wire rect on add/Reset in editor.
            rect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Bind CardData to UI and start face-down.
        /// </summary>
        public void Bind(CardData card)
        {
            // Persisted id format requested by the test: "card_id_X"
            CardId = $"card_id_{card.Id}";

            if (titleText) titleText.text = card.Name;
            if (descriptionText) descriptionText.text = card.Description;
            if (statsText) statsText.text = card.Stats;
            if (costText) costText.text = card.Cost.ToString();
            if (frontImage) frontImage.sprite = card.Artwork;

            if (!rect) rect = GetComponent<RectTransform>();

            // Default state: face-down.
            SetFaceUp(false, instant: true);
        }

        /// <summary>
        /// Unity UI click callback.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }

        /// <summary>
        /// Toggle face/back by enabling/disabling the roots.
        /// (The "instant" parameter is kept for future expansion; currently it's immediate.)
        /// </summary>
        public void SetFaceUp(bool faceUp, bool instant)
        {
            if (frontRoot) frontRoot.SetActive(faceUp);
            if (backRoot) backRoot.SetActive(!faceUp);
        }

        /// <summary>
        /// Stops any in-progress tweens on this view to avoid overlapping animations.
        /// </summary>
        public void KillTweens()
        {
            if (rect) rect.DOKill();
            transform.DOKill();
        }

        /// <summary>
        /// Sequence: lift -> move+scale to focus -> flip during travel.
        /// All positions are UI anchored positions (Screen Space Overlay).
        /// </summary>
        public Sequence PlayToFocus(RectTransform focusAnchor, float focusScale = 1.5f)
        {
            KillTweens();

            var start = rect.anchoredPosition;
            var lift = start + new Vector2(0, liftPx);

            var seq = DOTween.Sequence();

            // 1) Lift slightly upwards to give feedback.
            seq.Append(rect.DOAnchorPos(lift, liftTime).SetEase(Ease.OutQuad));

            // 2) Move+Scale to focus.
            seq.Append(rect.DOAnchorPos(focusAnchor.anchoredPosition, moveTime).SetEase(Ease.OutQuad));
            seq.Join(rect.DOScale(focusScale, moveTime));

            // 3) Flip while moving (scaleX to 0, swap face, scaleX back).
            seq.Insert(liftTime, FlipTween(focusScale));

            return seq;
        }

        /// <summary>
        /// Move/scale/rotate the card into its final hand slot.
        /// </summary>
        public Sequence PlayToHand(Vector2 handAnchoredPos, float finalScale = 1f)
        {
            KillTweens();

            var seq = DOTween.Sequence();
            seq.Append(rect.DOAnchorPos(handAnchoredPos, moveTime).SetEase(Ease.InOutQuad));
            seq.Join(rect.DOScale(finalScale, moveTime));
            seq.Join(rect.DOLocalRotate(Vector3.zero, moveTime));
            return seq;
        }

        /// <summary>
        /// "Fake flip" using scaleX:
        /// - Scale X to 0 (card edge), swap face, then scale X back.
        /// </summary>
        private Sequence FlipTween(float targetScaleX)
        {
            var seq = DOTween.Sequence();
            seq.Append(rect.DOScaleX(0f, flipTime * 0.5f).SetEase(Ease.InQuad));
            seq.AppendCallback(() => SetFaceUp(true, instant: true));
            seq.Append(rect.DOScaleX(targetScaleX, flipTime * 0.5f).SetEase(Ease.OutQuad));
            return seq;
        }
    }
}
