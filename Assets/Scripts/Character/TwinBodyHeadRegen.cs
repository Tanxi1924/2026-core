using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 两个头的血量联动规则（对应文档 3.4 里"已讨论但未实现"的设计，这里补上第一条）：
    /// 一个头没血了、另一个头还有血 → 没血的那个头自动进入回复状态，经过 Revive Duration 秒后
    /// 血量涨回 1 点就停止（不会回满，也不会一直回）。
    ///
    /// 两个头都是 0 血的情况（game over）暂不处理，见文档，留到下一步。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Twin Body/Head Regen (Custom)")]
    public class TwinBodyHeadRegen : MonoBehaviour
    {
        [Header("自动回复（由策划调节）")]
        [Tooltip("头没血之后，只要另一个头还有血，就会自动进入回复状态；经过这么多秒后血量涨回 1 点就停止")]
        [Min(0.01f)] public float ReviveDuration = 3f;

        protected TwinBodyCharacter _body;
        protected Health _jenniferHealth;
        protected Health _johnHealth;

        // < 0 表示不在回复状态；>= 0 时是"已经回复了多久"的计时
        protected float _jenniferReviveTimer = -1f;
        protected float _johnReviveTimer = -1f;

        private void Awake()
        {
            _body = GetComponent<TwinBodyCharacter>();
            _jenniferHealth = _body.JenniferNode != null ? _body.JenniferNode.GetComponent<Health>() : null;
            _johnHealth = _body.JohnNode != null ? _body.JohnNode.GetComponent<Health>() : null;

            // Health.Kill() 会把 TemporarilyInvulnerable 永久设为 true，
            // 用 OnDeath 精确捕捉"刚变成 0 血"的那一刻，开始计时。
            if (_jenniferHealth != null) _jenniferHealth.OnDeath += () => _jenniferReviveTimer = 0f;
            if (_johnHealth != null) _johnHealth.OnDeath += () => _johnReviveTimer = 0f;
        }

        private void Update()
        {
            UpdateRevive(_jenniferHealth, _johnHealth, ref _jenniferReviveTimer);
            UpdateRevive(_johnHealth, _jenniferHealth, ref _johnReviveTimer);
        }

        /// <summary>推进 dead 这个头的回复进度；other 是另一个头，用来判断"另一个头是否还有血"。</summary>
        private void UpdateRevive(Health dead, Health other, ref float timer)
        {
            if (dead == null || other == null || timer < 0f) return;

            // 另一个头也没血了：两个头都是 0，属于以后 game over 的情况，这里先冻结进度，不继续回复。
            if (other.CurrentHealth <= 0f) return;

            timer += Time.deltaTime;
            dead.CurrentHealth = Mathf.Clamp01(timer / ReviveDuration);

            if (timer >= ReviveDuration)
            {
                dead.CurrentHealth = 1f;
                dead.DamageEnabled(); // 解除 Kill() 留下的永久无敌，让这个头重新可以被打
                timer = -1f;
            }
        }
    }
}
