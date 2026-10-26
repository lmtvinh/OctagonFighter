using System;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Owns per-fighter attack cooldowns, the buffered attack input, and pending hit requests.
    /// </summary>
    public sealed class AttackTracker
    {
        private const int SlotCount = 3;

        private readonly AttackBook _book;
        private readonly float[] _cooldownRemaining = new float[SlotCount];
        private AttackSlot _bufferedSlot;
        private float _bufferRemainingSeconds;
        private AttackHitRequest _pendingHit;
        private bool _hasPendingHit;
        private int _hitSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackTracker"/> class.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the attack book is not configured.</exception>
        public AttackTracker(AttackBook book)
        {
            if (!book.Light.IsDefined || !book.Heavy.IsDefined)
            {
                throw new ArgumentException("Attack book must define both attacks.", nameof(book));
            }

            _book = book;
        }

        /// <summary>
        /// Gets the attack book used by this tracker.
        /// </summary>
        public AttackBook Book => _book;

        /// <summary>
        /// Gets a value indicating whether an input is currently buffered.
        /// </summary>
        public bool HasBufferedAttack => _bufferRemainingSeconds > 0f;

        /// <summary>
        /// Gets the remaining cooldown for a slot in seconds.
        /// </summary>
        public float GetCooldownRemaining(AttackSlot slot)
        {
            int index = (int)slot;
            return index > 0 && index < SlotCount ? _cooldownRemaining[index] : 0f;
        }

        /// <summary>
        /// Determines whether a slot may start now.
        /// </summary>
        public bool CanStart(AttackSlot slot)
        {
            return GetCooldownRemaining(slot) <= 0f;
        }

        /// <summary>
        /// Starts the cooldown for a slot.
        /// </summary>
        public void StartAttack(AttackSlot slot, in AttackDefinition definition)
        {
            int index = (int)slot;
            if (index <= 0 || index >= SlotCount)
            {
                return;
            }

            _cooldownRemaining[index] = definition.CooldownSeconds;
        }

        /// <summary>
        /// Buffers a pressed attack until the fighter can start it.
        /// </summary>
        public void BufferAttack(AttackSlot slot)
        {
            if (slot == AttackSlot.None || _book.InputBufferSeconds <= 0f)
            {
                return;
            }

            _bufferedSlot = slot;
            _bufferRemainingSeconds = _book.InputBufferSeconds;
        }

        /// <summary>
        /// Returns the buffered attack slot without consuming it.
        /// </summary>
        public bool TryGetBufferedAttack(out AttackSlot slot)
        {
            if (_bufferRemainingSeconds <= 0f || _bufferedSlot == AttackSlot.None)
            {
                slot = AttackSlot.None;
                return false;
            }

            slot = _bufferedSlot;
            return true;
        }

        /// <summary>
        /// Clears the buffered attack input.
        /// </summary>
        public void ClearBufferedAttack()
        {
            _bufferedSlot = AttackSlot.None;
            _bufferRemainingSeconds = 0f;
        }

        /// <summary>
        /// Allocates the next hit window identifier for this fighter.
        /// </summary>
        public HitId NextHitId()
        {
            _hitSequence++;
            return new HitId(_hitSequence);
        }

        /// <summary>
        /// Queues a hit query request for the presentation layer.
        /// </summary>
        public void RequestHit(in AttackHitRequest request)
        {
            _pendingHit = request;
            _hasPendingHit = true;
        }

        /// <summary>
        /// Removes and returns the pending hit request, if any.
        /// </summary>
        public bool TryTakeHitRequest(out AttackHitRequest request)
        {
            if (!_hasPendingHit)
            {
                request = default;
                return false;
            }

            request = _pendingHit;
            _hasPendingHit = false;
            _pendingHit = default;
            return true;
        }

        /// <summary>
        /// Advances cooldowns and the input buffer by one simulation step.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the delta is invalid.</exception>
        public void Tick(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");
            }

            if (deltaSeconds <= 0f)
            {
                return;
            }

            for (int index = 1; index < SlotCount; index++)
            {
                if (_cooldownRemaining[index] > 0f)
                {
                    _cooldownRemaining[index] = MathF.Max(0f, _cooldownRemaining[index] - deltaSeconds);
                }
            }

            if (_bufferRemainingSeconds > 0f)
            {
                _bufferRemainingSeconds = MathF.Max(0f, _bufferRemainingSeconds - deltaSeconds);
                if (_bufferRemainingSeconds <= 0f)
                {
                    _bufferedSlot = AttackSlot.None;
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether a slot is currently running a cooldown.
        /// </summary>
        public bool IsOnCooldown(AttackSlot slot)
        {
            return GetCooldownRemaining(slot) > 0f;
        }
    }
}
