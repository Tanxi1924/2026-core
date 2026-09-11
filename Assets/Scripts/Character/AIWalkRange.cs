using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// World-X patrol turn points around the initial position.
    /// This issues movement commands; it does not clamp knockback or teleport.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/AI/Range Patrol (Custom)")]
    public class AIWalkRange : AIWalk
    {
        [Header("Patrol Range")]
        [Min(0f)]
        [Tooltip("Distance from the initial position to each turn point in world units. Zero stops patrol. Only applies to Patrol mode.")]
        public float PatrolRadius = 5f;

        private Vector3 _patrolOrigin;
        private bool _originInitialized;

        protected override void Initialization()
        {
            base.Initialization();
            if (!_originInitialized)
            {
                _patrolOrigin = transform.position;
                _originInitialized = true;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!_originInitialized || _character == null
                || _characterHorizontalMovement == null
                || WalkBehaviour != WalkBehaviours.Patrol)
            {
                return;
            }

            if (_character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead
                || _character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Frozen)
            {
                return;
            }

            float radius = Mathf.Max(0f, PatrolRadius);
            if (radius == 0f)
            {
                _direction = Vector2.zero;
            }
            else if (transform.position.x <= _patrolOrigin.x - radius)
            {
                _direction = Vector2.right;
            }
            else if (transform.position.x >= _patrolOrigin.x + radius)
            {
                _direction = Vector2.left;
            }
            else if (_direction.x == 0f)
            {
                // Resume after changing a zero radius back to a positive value.
                _direction = _character.IsFacingRight ? Vector2.right : Vector2.left;
            }

            // A push can leave the robot outside its range. Do not force it
            // back through a wall or over a hole just to reach its turn point.
            if (radius > 0f && Mathf.Abs(transform.position.x - _patrolOrigin.x) >= radius)
            {
                if (ChangeDirectionOnWall
                    && ((_direction.x < 0f && _controller.State.IsCollidingLeft)
                        || (_direction.x > 0f && _controller.State.IsCollidingRight)))
                {
                    _direction = Vector2.zero;
                }
                if (AvoidFalling && _controller.State.IsGrounded && _direction.x != 0f)
                {
                    Vector3 rayOrigin = transform.position
                        + transform.right * (_direction.x * (_controller.Bounds.x / 2f + HoleDetectionOffset.x))
                        + transform.up * HoleDetectionOffset.y;
                    int groundMask = _controller.PlatformMask | _controller.MovingPlatformMask
                        | _controller.OneWayPlatformMask | _controller.MovingOneWayPlatformMask;
                    if (!Physics2D.Raycast(rayOrigin, -transform.up, HoleDetectionRaycastLength, groundMask))
                    {
                        _direction = Vector2.zero;
                    }
                }
            }

            _characterHorizontalMovement.SetHorizontalMove(_direction.x);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying && _originInitialized
                ? _patrolOrigin : transform.position;
            float radius = Mathf.Max(0f, PatrolRadius);
            Vector3 left = origin + Vector3.left * radius;
            Vector3 right = origin + Vector3.right * radius;
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left + Vector3.up * 0.5f, left + Vector3.down * 0.5f);
            Gizmos.DrawLine(right + Vector3.up * 0.5f, right + Vector3.down * 0.5f);
            Gizmos.DrawWireSphere(origin, 0.1f);
            Gizmos.color = previousColor;
        }
    }
}
