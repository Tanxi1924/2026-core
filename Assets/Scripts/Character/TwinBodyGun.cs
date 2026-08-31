using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 约翰的枪械 Ability（文档 4. 约翰-枪系统）。左键朝鼠标方向开枪：命中目标用 Corgi 自带的
    /// Health.Damage 造成伤害，并在约翰节点位置对整体刚体施加后坐力（TwinBodyCharacter.ApplyForceAtNode）。
    /// 弹匣有上限、单发 CD，闲置时随时间自动回复；TwinBodyTongue 捕获吃到东西后也会调用 RestoreAmmo。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Gun (Custom)")]
    public class TwinBodyGun : CharacterAbility
    {
        public override string HelpBoxText() =>
            "约翰的枪械：左键朝鼠标方向开枪，命中造成伤害并产生后坐力。弹匣有上限，射击消耗弹匣，" +
            "有单发 CD，闲置时随时间自动回复，TwinBodyTongue 吃到东西也会回复弹匣。";

        [Header("输入")]
        public bool ReadInput = true;

        [Header("弹道 / 伤害（由策划调节）")]
        [Min(0f)] public float Range = 20f;
        [Min(0f)] public float Damage = 10f;
        public LayerMask HitMask = ~0;

        [Header("弹匣（由策划调节）")]
        [Min(1)] public int MagazineCapacity = 6;
        [MMReadOnly] public int CurrentAmmo;
        [Min(0f), Tooltip("单发之间的最小间隔（秒，支持小数）")]
        public float FireCooldown = 0.25f;
        [Min(0f), Tooltip("不开枪时，每隔多少秒自动回复 1 发弹匣")]
        public float AmmoRegenInterval = 1.5f;

        [Header("后坐力（由策划调节）")]
        [Min(0f)] public float RecoilForce = 4f;

        protected TwinBodyCharacter _body;
        protected bool _fireInput;
        protected float _cooldownTimer;
        protected float _regenTimer;

        protected override void Initialization()
        {
            base.Initialization();
            _body = GetComponent<TwinBodyCharacter>();
            CurrentAmmo = MagazineCapacity;

            // 默认排除自身所在层，避免子弹一出枪口就打到自己身上的 Collider
            if (HitMask.value == ~0)
            {
                HitMask &= ~(1 << gameObject.layer);
            }
        }

        public override void EarlyProcessAbility()
        {
            _fireInput = ReadInput && Input.GetMouseButton(0);
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!AbilityAuthorized || _body == null) return;

            _cooldownTimer -= Time.deltaTime;

            if (_fireInput)
            {
                TryFire();
            }
            else if (CurrentAmmo < MagazineCapacity)
            {
                _regenTimer += Time.deltaTime;
                if (_regenTimer >= AmmoRegenInterval)
                {
                    _regenTimer = 0f;
                    CurrentAmmo = Mathf.Min(CurrentAmmo + 1, MagazineCapacity);
                }
            }
        }

        protected virtual void TryFire()
        {
            if (_cooldownTimer > 0f) return;
            if (CurrentAmmo <= 0) return;

            Vector2 muzzle = _body.JohnNode.position;
            Vector2 direction = ((Vector2)GetMouseWorldPosition() - muzzle).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

            CurrentAmmo--;
            _cooldownTimer = FireCooldown;
            _regenTimer = 0f;

            RaycastHit2D hit = Physics2D.Raycast(muzzle, direction, Range, HitMask);
            if (hit.collider != null)
            {
                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.Damage(Damage, gameObject, 0.2f, 0.2f, direction);
                }
                Debug.DrawLine(muzzle, hit.point, Color.red, 0.15f);
            }
            else
            {
                Debug.DrawLine(muzzle, muzzle + direction * Range, Color.red, 0.15f);
            }

            // 局部后坐力：方向与射击相反，作用在约翰节点上，会同时产生位移和旋转
            _body.ApplyForceAtNode(_body.JohnNode, -direction * RecoilForce, ForceMode2D.Impulse);

            PlayAbilityStartFeedbacks();
        }

        /// <summary>供 TwinBodyTongue 捕获吃到东西后调用，回复弹匣（对应文档"弹匣可以通过舌头吃掉东西回复"）。</summary>
        public virtual void RestoreAmmo(int amount)
        {
            CurrentAmmo = Mathf.Min(CurrentAmmo + amount, MagazineCapacity);
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
