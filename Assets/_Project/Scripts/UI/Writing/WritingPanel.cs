using TMPro;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Hanzi.Input;

namespace HanziZombieDefense.UI.Writing
{
    /// <summary>
    /// On-screen overlay for the writing UI in the mobile build. Always visible
    /// during gameplay — it shows the active target's faded glyph preview and
    /// the "current/total" stroke progress. The actual ink-drawing surface
    /// (<see cref="HanziZombieDefense.Hanzi.Input.WritingCanvas"/>) is hosted
    /// as a sibling/child and is also always active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WritingPanel : MonoBehaviour
    {
        [SerializeField, Tooltip("Faded preview text of the target character.")]
        private TextMeshProUGUI targetCharacterText;

        [SerializeField, Tooltip("Stroke progress label, e.g. '3/7'.")]
        private TextMeshProUGUI strokeProgressText;

        [SerializeField, Range(0f, 1f), Tooltip("Alpha applied to the preview character.")]
        private float previewAlpha = 0.25f;

        private HanziCharacter _activeCharacter;
        private int _currentStroke;

        public HanziCharacter ActiveCharacter => _activeCharacter;

        private void Awake()
        {
            ClearDisplay();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.TargetChanged>(OnTargetChanged);
            EventBus.Subscribe<WritingEvents.StrokeAccepted>(OnStrokeAccepted);
            EventBus.Subscribe<WritingEvents.CharacterCompleted>(OnCharacterCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.TargetChanged>(OnTargetChanged);
            EventBus.Unsubscribe<WritingEvents.StrokeAccepted>(OnStrokeAccepted);
            EventBus.Unsubscribe<WritingEvents.CharacterCompleted>(OnCharacterCompleted);
        }

        /// <summary>Bind <paramref name="character"/> as the active character displayed in the panel.</summary>
        public void SetCharacter(HanziCharacter character)
        {
            _activeCharacter = character;
            _currentStroke = 0;

            if (targetCharacterText != null)
            {
                targetCharacterText.text = character != null ? character.Character : string.Empty;
                Color c = targetCharacterText.color;
                c.a = previewAlpha;
                targetCharacterText.color = c;
            }

            UpdateProgress(0, character != null ? character.StrokeCount : 0);
        }

        /// <summary>Clear the displayed character and progress without disabling the panel.</summary>
        public void ClearDisplay()
        {
            _activeCharacter = null;
            _currentStroke = 0;

            if (targetCharacterText != null) targetCharacterText.text = string.Empty;
            if (strokeProgressText != null) strokeProgressText.text = string.Empty;
        }

        /// <summary>Update the on-screen stroke progress label.</summary>
        public void UpdateProgress(int current, int total)
        {
            _currentStroke = current;
            if (strokeProgressText == null) return;

            if (total <= 0)
            {
                strokeProgressText.text = string.Empty;
                return;
            }

            strokeProgressText.text = $"{Mathf.Clamp(current, 0, total)}/{total}";
        }

        private void OnTargetChanged(GameEvents.TargetChanged evt)
        {
            var zombie = evt.Current ?? evt.newTarget;
            if (zombie == null)
            {
                ClearDisplay();
                return;
            }

            var character = zombie.GetAssignedCharacter();
            if (character == null)
            {
                ClearDisplay();
                return;
            }

            SetCharacter(character);
        }

        private void OnStrokeAccepted(WritingEvents.StrokeAccepted evt)
        {
            int total = _activeCharacter != null ? _activeCharacter.StrokeCount : 0;
            UpdateProgress(evt.StrokeIndex + 1, total);
        }

        private void OnCharacterCompleted(WritingEvents.CharacterCompleted _)
        {
            ClearDisplay();
        }
    }
}
