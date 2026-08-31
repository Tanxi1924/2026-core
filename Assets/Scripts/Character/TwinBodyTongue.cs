using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 珍妮佛的舌头 Ability（文档 5. 珍妮佛-舌头系统）。右键朝鼠标方向伸出舌头：
    /// 打中 TongueCapturable → 把目标拉到嘴边吃掉（回复约翰弹匣，见 TwinBodyGun.RestoreAmmo）；
    /// 打中 TonguePullAnchor → 对珍妮佛节点持续施加拉力（TwinBodyCharacter.ApplyForceAtNode），
    /// 配合重力和已有速度可以做出摆荡的效果。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Tongue (Custom)")]
    public class TwinBodyTongue : CharacterAbility
    {
        public override string HelpBoxText() =>
            "珍妮佛的舌头：右键朝鼠标方向伸出。打中 TongueCapturable 会把目标拉过来吃掉（回复约翰弹匣）；" +
            "打中 TonguePullAnchor 会对珍妮佛节点持续施加拉力，松开右键或够到目标后停止。";

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
        [Min(0f)] public float PullForce = 25f;

        protected TwinBodyCharacter _body;
        protected TwinBodyGun _gun;

        protected bool _tongueFirePressed;
        protected bool _tongueReleased;

        protected Transform _capturedTarget;
        protected Collider2D _capturedCollider;
        protected Rigidbody2D _capturedRigidbody;

        protected Transform _pullAnchor;

        public bool IsReeling => _capturedTarget != null;
        public bool IsPulling => _pullAnchor != null;

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
            _tongueReleased = Input.GetMouseButtonUp(1);
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!AbilityAuthorized || _body == null) return;

            if (_tongueReleased)
            {
                _pullAnchor = null;
            }

            if (_tongueFirePressed && !IsReeling)
            {
                FireTongue();
            }

            if (IsReeling)
            {
                UpdateReel();
            }

            if (IsPulling)
            {
                UpdatePull();
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

            TonguePullAnchor anchor = hit.collider.GetComponentInParent<TonguePullAnchor>();
            if (anchor != null)
            {
                _pullAnchor = anchor.transform;
            }
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

        protected virtual void UpdatePull()
        {
            Vector2 origin = _body.JenniferNode.position;
            Vector2 direction = ((Vector2)_pullAnchor.position - origin).normalized;
            _body.ApplyForceAtNode(_body.JenniferNode, direction * PullForce, ForceMode2D.Force);

            Debug.DrawLine(origin, _pullAnchor.position, Color.cyan);
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
