using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 珍妮佛的舌头 Ability（文档 5. 珍妮佛-舌头系统）。右键单点（不需要按住）朝鼠标方向发一条射线：
    /// 打中 TongueCapturable → 把目标拉到嘴边吃掉（回复约翰弹匣，见 TwinBodyGun.RestoreAmmo）；
    /// 打中其他任何实体碰撞体（地形、墙壁……）→ 对整个刚体（TwinBodyCharacter.Body）在珍妮佛节点上
    /// 施加一次朝命中点方向的冲量，牵引一下，没有后续持续效果，松不松右键都无所谓。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Tongue (Custom)")]
    public class TwinBodyTongue : CharacterAbility
    {
        public override string HelpBoxText() =>
            "珍妮佛的舌头：右键点一下（不用按住）朝鼠标方向发射线。打中 TongueCapturable 会把目标拉过来吃掉" +
            "（回复约翰弹匣）；打中其他任何东西（地形、墙壁等）会朝命中点牵引一次性的冲量，没有持续效果。";

        [Header("输入")]
        public bool ReadInput = true;

        [Header("舌头（由策划调节）")]
        [Min(0f)] public float TongueLength = 8f;
        public LayerMask HitMask = ~0;

        [Header("捕获进食（由策划调节）")]
        [Min(0.01f)] public float ReelSpeed = 12f;
        [Min(0.01f)] public float EatDistance = 0.3f;
        [Min(1)] public int AmmoRestoreOnEat = 2;

        [Header("牵引（由策划调节）")]
        [Tooltip("打中非可食用物体时，朝命中点方向给整体刚体的一次性冲量大小")]
        [Min(0f)] public float PullImpulse = 6f;

        protected TwinBodyCharacter _body;
        protected TwinBodyGun _gun;

        protected bool _tongueFirePressed;

        protected Transform _capturedTarget;
        protected Collider2D _capturedCollider;
        protected Rigidbody2D _capturedRigidbody;

        public bool IsReeling => _capturedTarget != null;

        protected override void Initialization()
        {
            base.Initialization();
            _body = GetComponent<TwinBodyCharacter>();
            _gun = (_character != null) ? _character.FindAbility<TwinBodyGun>() : GetComponent<TwinBodyGun>();

            // 默认排除自身所在层，避免舌头刚伸出来就打到自己身上的 Collider
            if (HitMask.value == ~0)
            {
                HitMask &= ~(1 << gameObject.layer);
            }
        }

        public override void EarlyProcessAbility()
        {
            _tongueFirePressed = ReadInput && Input.GetMouseButtonDown(1);
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!AbilityAuthorized || _body == null) return;

            if (_tongueFirePressed && !IsReeling)
            {
                FireTongue();
            }

            if (IsReeling)
            {
                UpdateReel();
            }
        }

        protected virtual void FireTongue()
        {
            Vector2 origin = _body.JenniferNode.position;
            Vector2 target = GetMouseWorldPosition();
            Vector2 direction = (target - origin).normalized;
            if (direction.sqrMagnitude < 0.0001f) return;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, TongueLength, HitMask);
            Debug.DrawLine(origin, hit.collider != null ? hit.point : origin + direction * TongueLength, Color.magenta, 0.2f);

            if (hit.collider == null) return;

            TongueCapturable capturable = hit.collider.GetComponentInParent<TongueCapturable>();
            if (capturable != null)
            {
                StartReel(capturable);
                return;
            }

            // 打中除了"可吃的东西"之外的任何实体碰撞体（地形、墙壁……）都给一次朝命中点的冲量，
            // 不需要按住右键、也没有后续持续效果。
            _body.ApplyForceAtNode(_body.JenniferNode, direction * PullImpulse, ForceMode2D.Impulse);
            PlayAbilityStartFeedbacks();
        }

        protected virtual void StartReel(TongueCapturable capturable)
        {
            _capturedTarget = capturable.transform;

            _capturedCollider = capturable.GetComponent<Collider2D>();
            if (_capturedCollider != null) _capturedCollider.enabled = false;

            _capturedRigidbody = capturable.GetComponent<Rigidbody2D>();
            if (_capturedRigidbody != null)
            {
                _capturedRigidbody.bodyType = RigidbodyType2D.Kinematic;
                _capturedRigidbody.linearVelocity = Vector2.zero;
            }

            PlayAbilityStartFeedbacks();
        }

        protected virtual void UpdateReel()
        {
            Vector3 mouth = _body.JenniferNode.position;
            _capturedTarget.position = Vector3.MoveTowards(_capturedTarget.position, mouth, ReelSpeed * Time.deltaTime);

            if (Vector3.Distance(_capturedTarget.position, mouth) <= EatDistance)
            {
                EatCapturedTarget();
            }
        }

        protected virtual void EatCapturedTarget()
        {
            if (_gun != null)
            {
                _gun.RestoreAmmo(AmmoRestoreOnEat);
            }

            Destroy(_capturedTarget.gameObject);
            _capturedTarget = null;
            _capturedCollider = null;
            _capturedRigidbody = null;

            PlayAbilityStopFeedbacks();
        }

        protected virtual Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return transform.position;
            Vector3 mouse = Input.mousePosition;
            mouse.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mouse);
        }
    }
}
