using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Drop Through (Custom)")]
    public class TwinBodyDropThrough : CharacterAbility
    {
        public override string HelpBoxText() =>
            "站在下跳机关上按 S 或下方向键，整个双体角色穿过当前平台。松开后才能再次触发。";

        public bool ReadInput = true;
        [Min(0.001f)] public float GroundProbeDistance = 0.04f;
        [Min(0.1f)] public float InitialDownSpeed = 2f;
        [Min(0.001f)] public float Clearance = 0.03f;
        public bool IsDropping => _ignored.Count > 0;

        private Rigidbody2D _body;
        private Collider2D[] _bodyColliders;
        private bool _requested;
        private readonly HashSet<DropThroughPlatform> _platforms = new HashSet<DropThroughPlatform>();
        private readonly HashSet<Collider2D> _surfaces = new HashSet<Collider2D>();
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();
        private readonly List<Pair> _ignored = new List<Pair>();
        private struct Pair { public Collider2D Body; public Collider2D Surface; }

        protected override void Initialization()
        {
            base.Initialization();
            _body = GetComponent<Rigidbody2D>();
            _bodyColliders = GetComponentsInChildren<Collider2D>();
        }

        public void RegisterPlatform(DropThroughPlatform platform) { _platforms.Add(platform); }
        public void UnregisterPlatform(DropThroughPlatform platform) { _platforms.Remove(platform); }

        public override void EarlyProcessAbility()
        {
            // Matches the other TwinBody abilities' direct keyboard input.
            _requested = ReadInput && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow));
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (_requested && isActiveAndEnabled && AbilityAuthorized &&
                _body != null && !IsDropping && Time.timeScale > 0f &&
                (_condition == null || _condition.CurrentState == CharacterStates.CharacterConditions.Normal))
                TryDrop();
            _requested = false;
        }

        private bool IsBodyCollider(Collider2D collider) => collider != null && collider.enabled &&
            collider.gameObject.activeInHierarchy && !collider.isTrigger && collider.attachedRigidbody == _body;

        private void TryDrop()
        {
            if (!_body.simulated || _body.bodyType != RigidbodyType2D.Dynamic || _body.linearVelocity.y > 0.1f) return;
            _surfaces.Clear();
            // Probe every compound collider, including the connector and both rotating heads.
            foreach (var bodyCollider in _bodyColliders)
            {
                if (!IsBodyCollider(bodyCollider)) continue;
                var filter = new ContactFilter2D();
                filter.SetLayerMask(Physics2D.GetLayerCollisionMask(bodyCollider.gameObject.layer));
                filter.useTriggers = false;
                bodyCollider.Cast(Vector2.down, filter, _hits, GroundProbeDistance, true);
                foreach (var hit in _hits)
                {
                    var surface = hit.collider;
                    if (surface == null || surface.attachedRigidbody == _body || hit.normal.y < 0.5f ||
                        Physics2D.GetIgnoreCollision(bodyCollider, surface)) continue;
                    // Casts starting in an overlap have a synthetic upward normal. Reject side walls.
                    if (surface.bounds.max.y > bodyCollider.bounds.min.y + GroundProbeDistance) continue;
                    bool allowed = false;
                    foreach (var platform in _platforms)
                        if (platform != null && platform.Available && platform.Surface == surface)
                        { allowed = true; break; }
                    // If either head is supported by ordinary ground, do not drop part of the body.
                    if (!allowed) return;
                    _surfaces.Add(surface);
                }
            }
            if (_surfaces.Count == 0) return;
            foreach (var surface in _surfaces)
                foreach (var bodyCollider in _bodyColliders)
                {
                    if (!IsBodyCollider(bodyCollider) || Physics2D.GetIgnoreCollision(bodyCollider, surface)) continue;
                    Physics2D.IgnoreCollision(bodyCollider, surface, true);
                    _ignored.Add(new Pair { Body = bodyCollider, Surface = surface });
                }
            if (!IsDropping) return;
            _body.linearVelocity = new Vector2(_body.linearVelocity.x,
                Mathf.Min(_body.linearVelocity.y, -InitialDownSpeed));
            PlayAbilityStartFeedbacks();
        }

        private void FixedUpdate()
        {
            if (!IsDropping) return;
            // Independent of AbilityAuthorized, so locking input cannot strand ignored pairs.
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (var collider in _bodyColliders)
            {
                if (!IsBodyCollider(collider)) continue;
                if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
                else bounds.Encapsulate(collider.bounds);
            }
            for (int i = _ignored.Count - 1; i >= 0; i--)
            {
                var pair = _ignored[i];
                bool restore = !hasBounds || pair.Body == null || pair.Surface == null;
                if (!restore)
                {
                    var s = pair.Surface.bounds;
                    restore = !pair.Surface.enabled || !pair.Surface.gameObject.activeInHierarchy ||
                        bounds.max.y < s.min.y - Clearance || bounds.min.y > s.max.y + Clearance ||
                        bounds.max.x < s.min.x - Clearance || bounds.min.x > s.max.x + Clearance;
                }
                if (!restore) continue;
                Restore(pair);
                _ignored.RemoveAt(i);
            }
            if (!IsDropping) { StopStartFeedbacks(); PlayAbilityStopFeedbacks(); }
        }

        private static void Restore(Pair pair)
        {
            if (pair.Body != null && pair.Surface != null)
                Physics2D.IgnoreCollision(pair.Body, pair.Surface, false);
        }

        private void ClearDrop()
        {
            foreach (var pair in _ignored) Restore(pair);
            _ignored.Clear();
            _surfaces.Clear();
            _requested = false;
            StopStartFeedbacks();
        }

        public override void ResetAbility() { base.ResetAbility(); ClearDrop(); }
        protected override void OnDeath() { ClearDrop(); base.OnDeath(); }
        protected override void OnDisable()
        {
            ClearDrop();
            _platforms.Clear();
            base.OnDisable();
        }
    }
}
