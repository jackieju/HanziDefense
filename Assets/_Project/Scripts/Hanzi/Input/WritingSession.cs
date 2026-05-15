using System;
using System.Collections.Generic;
using UnityEngine;

using HanziZombieDefense.Core;
using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Hanzi.Recognition;
using HanziZombieDefense.Zombies;

namespace HanziZombieDefense.Hanzi.Input
{
    /// <summary>
    /// Contract for any object that <see cref="WritingSession"/> can kill upon
    /// character completion (typically a zombie).
    /// </summary>
    public interface IWritingTarget
    {
        /// <summary>
        /// Apply lethal damage / removal. Must be safe to call multiple times.
        /// </summary>
        void Kill();
    }

    /// <summary>EventBus payloads emitted by <see cref="WritingSession"/>.</summary>
    public static class WritingEvents
    {
        public struct StrokeAccepted
        {
            public string Character;
            public int StrokeIndex;
            public RecognitionResult Result;
        }

        public struct StrokeRejected
        {
            public string Character;
            public int StrokeIndex;
            public RecognitionResult Result;
        }

        public struct CharacterCompleted
        {
            public string Character;
            public int StrokeCount;
        }

        public struct WritingSessionStarted
        {
            public string Character;
        }

        public struct WritingSessionCancelled
        {
            public string Character;
        }
    }

    /// <summary>
    /// Orchestrates the active zombie's writing challenge. In the mobile build
    /// the writing canvas is always visible — this component no longer toggles
    /// its visibility. Sessions begin/cancel automatically in response to
    /// <see cref="GameEvents.TargetChanged"/>: when a new target is acquired
    /// its character is loaded and stroke recognition restarts; when the target
    /// is lost (despawn / pass / death without completion) the session cancels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WritingSession : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WritingCanvas writingCanvas;
        [SerializeField] private StrokeRecorder strokeRecorder;
        [SerializeField] private StrokeRenderer strokeRenderer;

        [Header("Recognition Tuning")]
        [Tooltip("Allow the matcher's defaults if true; otherwise use the inspector overrides below.")]
        [SerializeField] private bool useMatcherDefaults = true;

        [SerializeField] private float shapeThreshold = ResampledPointMatcher.DefaultShapeThreshold;
        [SerializeField] private float endpointThreshold = ResampledPointMatcher.DefaultEndpointThreshold;
        [SerializeField] private float directionThreshold = ResampledPointMatcher.DefaultDirectionThreshold;

        [Header("Auto-Begin")]
        [Tooltip("If true, sessions begin/cancel automatically based on GameEvents.TargetChanged.")]
        [SerializeField] private bool autoFollowTarget = true;

        private IStrokeMatcher _matcher;
        private HanziCharacter _character;
        private IWritingTarget _target;
        private Zombie _pendingTarget;
        private int _currentStrokeIndex;
        private bool _active;

        /// <summary>Character being practiced, or null when no session is active.</summary>
        public HanziCharacter ActiveCharacter => _character;

        /// <summary>Index of the next stroke the player must draw.</summary>
        public int CurrentStrokeIndex => _currentStrokeIndex;

        /// <summary>True between <see cref="Begin"/> and termination.</summary>
        public bool IsActive => _active;

        private void Awake()
        {
            _matcher = useMatcherDefaults
                ? new ResampledPointMatcher()
                : new ResampledPointMatcher(shapeThreshold, endpointThreshold, directionThreshold);
        }

        private void OnEnable()
        {
            if (writingCanvas != null)
                writingCanvas.StrokeEnded += HandleStrokeEnded;

            if (autoFollowTarget)
                EventBus.Subscribe<GameEvents.TargetChanged>(OnTargetChanged);
        }

        private void OnDisable()
        {
            if (writingCanvas != null)
                writingCanvas.StrokeEnded -= HandleStrokeEnded;

            if (autoFollowTarget)
                EventBus.Unsubscribe<GameEvents.TargetChanged>(OnTargetChanged);
        }

        /// <summary>
        /// Start a writing challenge. Resets stroke index and clears any previous
        /// drawing. Does NOT toggle canvas visibility — the canvas is always shown.
        /// </summary>
        public void Begin(HanziCharacter character, IWritingTarget target = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));

            _character = character;
            _target = target;
            _currentStrokeIndex = 0;
            _active = true;

            if (strokeRenderer != null) strokeRenderer.ClearAll();
            if (strokeRecorder != null) strokeRecorder.ClearAll();

            EventBus.Publish(new WritingEvents.WritingSessionStarted
            {
                Character = character.Character
            });
        }

        /// <summary>
        /// Cancel an in-progress session — for example when the targeted zombie
        /// reaches the player or dies before the character is completed. Idempotent.
        /// </summary>
        public void Cancel()
        {
            if (!_active) return;
            var charStr = _character?.Character ?? string.Empty;

            _active = false;
            _character = null;
            _target = null;
            _currentStrokeIndex = 0;

            if (strokeRenderer != null) strokeRenderer.ClearAll();
            if (strokeRecorder != null) strokeRecorder.ClearAll();

            EventBus.Publish(new WritingEvents.WritingSessionCancelled
            {
                Character = charStr
            });
        }

        /// <summary>
        /// Public entry point matching the spec. Equivalent to the internal
        /// stroke-ended handler; exposed for callers that drive the session
        /// without using <see cref="WritingCanvas"/> events.
        /// </summary>
        public void OnStrokeCompleted(List<Vector2> stroke) => HandleStrokeEnded(stroke);

        private void OnTargetChanged(GameEvents.TargetChanged evt)
        {
            Zombie next = evt.Current ?? evt.newTarget;

            if (next == null)
            {
                _pendingTarget = null;
                if (_active) Cancel();
                return;
            }

            HanziCharacter character = next.GetAssignedCharacter();
            if (character == null)
            {
                _pendingTarget = next;
                if (_active) Cancel();
                return;
            }

            _pendingTarget = null;
            if (_active && ReferenceEquals(_character, character)) return;
            if (_active) Cancel();
            Begin(character, new ZombieWritingTarget(next));
        }

        private void Update()
        {
            if (!autoFollowTarget) return;
            if (_active || _pendingTarget == null) return;

            if (_pendingTarget == null || !_pendingTarget.isActiveAndEnabled || !_pendingTarget.IsTargetable)
            {
                _pendingTarget = null;
                return;
            }

            var character = _pendingTarget.GetAssignedCharacter();
            if (character == null) return;

            var target = _pendingTarget;
            _pendingTarget = null;
            Begin(character, new ZombieWritingTarget(target));
        }

        private void HandleStrokeEnded(List<Vector2> stroke)
        {
            if (!_active || _character == null || stroke == null || stroke.Count < 2) return;
            if (_currentStrokeIndex >= _character.StrokeCount) return;

            var expected = _character.Strokes[_currentStrokeIndex];
            var result = _matcher.Match(stroke, expected);

            int renderedIndex = strokeRenderer != null
                ? strokeRenderer.DrawStrokePending(stroke)
                : -1;

            if (result.IsMatch)
            {
                if (renderedIndex >= 0 && strokeRenderer != null)
                    strokeRenderer.ConfirmStroke(renderedIndex);

                EventBus.Publish(new WritingEvents.StrokeAccepted
                {
                    Character = _character.Character,
                    StrokeIndex = _currentStrokeIndex,
                    Result = result
                });

                _currentStrokeIndex++;

                if (_currentStrokeIndex >= _character.StrokeCount)
                    HandleCharacterCompleted();
            }
            else
            {
                EventBus.Publish(new WritingEvents.StrokeRejected
                {
                    Character = _character.Character,
                    StrokeIndex = _currentStrokeIndex,
                    Result = result
                });

                if (strokeRecorder != null) strokeRecorder.RemoveLastCompleted();
                if (strokeRenderer != null) strokeRenderer.RemoveLastStroke();
            }
        }

        private void HandleCharacterCompleted()
        {
            var completedChar = _character.Character;
            var strokeCount = _character.StrokeCount;
            var target = _target;

            _active = false;
            _character = null;
            _target = null;
            _currentStrokeIndex = 0;

            if (strokeRecorder != null) strokeRecorder.ClearAll();
            if (strokeRenderer != null) strokeRenderer.ClearAll();

            EventBus.Publish(new WritingEvents.CharacterCompleted
            {
                Character = completedChar,
                StrokeCount = strokeCount
            });

            target?.Kill();
        }

        /// <summary>Adapter that lets a <see cref="Zombie"/> satisfy <see cref="IWritingTarget"/> without modifying the Zombie class.</summary>
        private sealed class ZombieWritingTarget : IWritingTarget
        {
            private readonly Zombie _zombie;
            public ZombieWritingTarget(Zombie zombie) { _zombie = zombie; }
            public void Kill() { if (_zombie != null) _zombie.Kill(); }
        }
    }
}
