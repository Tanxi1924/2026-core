using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 约翰的枪械 Ability（文档 4. 约翰-枪系统）。开火本身已交给 Corgi 官方的
    /// CharacterHandleWeapon + Weapon/ProjectileWeapon（挂在同一节点上，见
    /// TwinBodyCharacterPrefabBuilder，武器实例为 Assets/Prefabs/GunTemp.prefab）处理：
    /// 瞄准鼠标、扣弹匣、造成伤害、播放特效都走官方流程，左键开火靠场景里的
    /// InputManager（PlayerID = Player1）识别。
    ///
    /// 这个类只负责官方系统无法覆盖、且这个双体角色特有的两件事：
    /// 1) 后坐力——Corgi 官方后坐力是通过 CorgiController 施加的，而 TwinBodyCharacter
    ///    为了保持自由旋转的刚体物理，永久禁用了 CorgiController，官方后坐力对它是静默
    ///    无效果的，所以这里改成直接对 JohnNode 施力（TwinBodyCharacter.ApplyForceAtNode）。
    /// 2) 弹匣回复——官方弹匣只有"打光后 Reload"，没有"闲置时随时间自动回复 1 发"，
    ///    也没有"舌头吃到东西回复弹匣"的接口，这里通过直接读写 Weapon.CurrentAmmoLoaded
    ///    实现，TwinBodyTongue 捕获吃到东西后仍调用本类的 RestoreAmmo。
    /// 做法：每帧比较 CurrentWeapon.CurrentAmmoLoaded 相对上一帧的变化——变少了就说明刚开
    /// 了一枪，触发后坐力；不在开火状态且未满时才计时回复。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [RequireComponent(typeof(CharacterHandleWeapon))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Gun (Custom)")]
    public class TwinBodyGun : CharacterAbility
    {
        public override string HelpBoxText() =>
            "约翰的枪械：开火/瞄准/弹道/伤害都由官方 CharacterHandleWeapon + GunTemp 处理。" +
            "本组件只补两件事：把后坐力施加到 JohnNode 的刚体上（官方后坐力对这个无控制器的" +
            "角色无效），以及弹匣的闲置自动回复 / 舌头吃东西回复。";

        [Header("弹匣回复（叠加在官方 Weapon 弹匣之上，由策划调节）")]
        [Min(0f), Tooltip("不开枪时，每隔多少秒自动回复 1 发弹匣")]
        public float AmmoRegenInterval = 1.5f;

        [Header("后坐力（施加到 JohnNode，由策划调节）")]
        [Min(0f)] public float RecoilForce = 4f;

        protected TwinBodyCharacter _body;
        protected CharacterHandleWeapon _handleWeapon;
        protected int _lastAmmoLoaded = -1;
        protected float _regenTimer;

        protected override void Initialization()
        {
            base.Initialization();
            _body = GetComponent<TwinBodyCharacter>();
            _handleWeapon = GetComponent<CharacterHandleWeapon>();
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!AbilityAuthorized || _body == null || _handleWeapon == null) return;

            Weapon weapon = _handleWeapon.CurrentWeapon;
            if (weapon == null)
            {
                _lastAmmoLoaded = -1;
                return;
            }

            if (_lastAmmoLoaded < 0)
            {
                _lastAmmoLoaded = weapon.CurrentAmmoLoaded;
            }

            if (weapon.CurrentAmmoLoaded < _lastAmmoLoaded)
            {
                ApplyRecoil(weapon);
                _regenTimer = 0f;
            }
            else if (weapon.CurrentAmmoLoaded < weapon.MagazineSize
                     && weapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponIdle)
            {
                _regenTimer += Time.deltaTime;
                if (_regenTimer >= AmmoRegenInterval)
                {
                    _regenTimer = 0f;
                    weapon.CurrentAmmoLoaded = Mathf.Min(weapon.CurrentAmmoLoaded + 1, weapon.MagazineSize);
                }
            }
            else
            {
                _regenTimer = 0f;
            }

            _lastAmmoLoaded = weapon.CurrentAmmoLoaded;
        }

        /// <summary>用武器当前朝向（WeaponAim 已经转好的方向）的反方向，对 JohnNode 打一次冲量。</summary>
        protected virtual void ApplyRecoil(Weapon weapon)
        {
            Vector2 fireDirection = weapon.transform.right;
            _body.ApplyForceAtNode(_body.JohnNode, -fireDirection * RecoilForce, ForceMode2D.Impulse);
        }

        /// <summary>供 TwinBodyTongue 捕获吃到东西后调用，回复弹匣（对应文档"弹匣可以通过舌头吃掉东西回复"）。</summary>
        public virtual void RestoreAmmo(int amount)
        {
            Weapon weapon = _handleWeapon != null ? _handleWeapon.CurrentWeapon : null;
            if (weapon == null) return;

            weapon.CurrentAmmoLoaded = Mathf.Min(weapon.CurrentAmmoLoaded + amount, weapon.MagazineSize);
            _lastAmmoLoaded = weapon.CurrentAmmoLoaded;
        }
    }
}
