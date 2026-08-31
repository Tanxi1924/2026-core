using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>
    /// 双脑角色的物理主体，沿用 Corgi 的 CharacterAbility 结构（AbilityStartFeedbacks / AbilityPermitted /
    /// BlockingXxxStates 等字段与写法都与其他 Ability 一致），但不挂 CorgiController。
    ///
    /// 原因：CorgiController 是射线贴地的运动学控制器，会强行让角色保持直立、贴地，
    /// 和文档里"整体保持刚性结构、旋转时端点间距离不变、不主动锁定水平姿态"的真实刚体物理要求冲突。
    /// 所以这里改用真正的 Rigidbody2D：珍妮佛 / 约翰两个节点 + 中间连接件共用同一个 Rigidbody2D
    /// （复合 Collider），天然保持刚性、允许自由旋转。
    ///
    /// 为了能塞进 LevelManager.PlayerPrefabs（类型是 Character[]），根节点还是挂了真正的 Character
    /// 组件，并配了一个永久 enabled = false 的 CorgiController（只用来满足 Character.Initialization()
    /// 里 GetComponent&lt;CorgiController&gt;() 的硬依赖，它自己的 Update/FixedUpdate 永远不会跑，
    /// 不会跟 Rigidbody2D 抢控制权）。这样 Ability 生命周期就改由 Character.Update() 驱动，
    /// 不用再自己手动调用 Early/Process/LateProcessAbility。
    /// 代价：Character 的 Ability 循环挂在 Update()（跟渲染帧同步），不是 FixedUpdate()，
    /// 所以下面用 Time.deltaTime 而不是 Time.fixedDeltaTime。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Corgi Engine/Character/Abilities/Twin Body Character (Custom)")]
    public class TwinBodyCharacter : CharacterAbility
    {
        public override string HelpBoxText() =>
            "双脑角色物理骨架：两个球形节点（珍妮佛/约翰）+ 一个刚体连接件，共用同一个 Rigidbody2D。" +
            "不依赖 CorgiController，允许自由旋转，用于承载后续的开枪后坐力 / 舌头牵引等局部力效果。";

        [Header("节点引用")]
        public Transform JenniferNode;
        public Transform JohnNode;
        public Transform ConnectorBody;

        [Header("刚体物理（自动同步到 Rigidbody2D，改动即时生效）")]
        [Tooltip("重力规模 Gravity Scale")]
        public float GravityScale = 1f;
        [Min(0.01f)] public float Mass = 1f;
        [Min(0f)] public float LinearDamping = 0f;
        [Range(0f, 2f)] public float AngularDamping = 0.5f;

        [Header("节点位置 / 大小（改动会自动应用到子物体，无需重跑生成菜单）")]
        [Min(0.05f), Tooltip("珍妮佛 / 约翰节点半径")]
        public float NodeRadius = 0.5f;
        [Min(0f), Tooltip("两节点中心间距，可为 0")]
        public float NodeDistance = 2f;
        [Min(0.01f), Tooltip("连接件（刚体四边形）厚度")]
        public float ConnectorThickness = 0.3f;

        [Header("基础横向移动 A/D（由策划调节）")]
        [Tooltip("为 true 时读取键盘 A/D；没有 Character.LinkedInputManager 可用，所以这里直接读键盘，不走基类的 InputManager 分支")]
        public bool ReadInput = true;
        [Min(0f)] public float MoveSpeed = 6f;
        [Min(0f)] public float MoveAcceleration = 20f;

        public Rigidbody2D Body { get; private set; }

        /// <summary>Start() 由基类调用（Unity 生命周期），这里补上 Rigidbody2D 引用并做一次初始同步/摆位。</summary>
        protected override void Initialization()
        {
            base.Initialization();
            Body = GetComponent<Rigidbody2D>();
            SyncPhysics();
            ApplyLayout();
        }

        /// <summary>Inspector 中任意字段被修改后自动调用：把物理参数写回 Rigidbody2D，并重新摆放节点/连接件。</summary>
        private void OnValidate()
        {
            if (Body == null) Body = GetComponent<Rigidbody2D>();
            SyncPhysics();
            ApplyLayout();
        }

        /// <summary>把 GravityScale / Mass / LinearDamping / AngularDamping 写入 Rigidbody2D。</summary>
        public void SyncPhysics()
        {
            if (Body == null) return;

            Body.gravityScale = GravityScale;
            Body.mass = Mass;
            Body.linearDamping = LinearDamping;
            Body.angularDamping = AngularDamping;
        }

        /// <summary>按当前 NodeRadius / NodeDistance / ConnectorThickness 重新摆放两个节点与连接件。</summary>
        public void ApplyLayout()
        {
            if (JenniferNode != null)
            {
                JenniferNode.localPosition = new Vector3(-NodeDistance / 2f, 0f, 0f);
                JenniferNode.localScale = Vector3.one * (NodeRadius * 2f);
            }
            if (JohnNode != null)
            {
                JohnNode.localPosition = new Vector3(NodeDistance / 2f, 0f, 0f);
                JohnNode.localScale = Vector3.one * (NodeRadius * 2f);
            }
            if (ConnectorBody != null)
            {
                ConnectorBody.localPosition = Vector3.zero;
                ConnectorBody.localScale = new Vector3(NodeDistance, ConnectorThickness, 1f);
            }
        }

        /// <summary>
        /// 覆写而不调用 base：基类的 InternalHandleInput 只有在 Character.LinkedInputManager 匹配到
        /// 场景里的 InputManager 时才会走到 HandleInput()。这个原型角色直接读键盘，绕开那一层。
        /// </summary>
        public override void EarlyProcessAbility()
        {
            _horizontalInput = 0f;
            if (!ReadInput) return;

            if (Input.GetKey(KeyCode.A)) _horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D)) _horizontalInput += 1f;
        }

        /// <summary>由 Character.Update() 每帧调用，因此用 Time.deltaTime 而不是 Time.fixedDeltaTime。</summary>
        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!AbilityAuthorized || Body == null) return;

            float targetX = _horizontalInput * MoveSpeed;
            float newX = Mathf.MoveTowards(Body.linearVelocity.x, targetX, MoveAcceleration * Time.deltaTime);
            Body.linearVelocity = new Vector2(newX, Body.linearVelocity.y);
        }

        /// <summary>
        /// 在指定节点的世界坐标位置，对整体刚体施加一次力/冲量。
        /// 用于枪械后坐力、舌头牵引等"局部力"效果；同一帧内多次调用会按向量叠加（对应"后座力允许叠加"）。
        /// </summary>
        public void ApplyForceAtNode(Transform node, Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
        {
            Body.AddForceAtPosition(force, node.position, mode);
        }
    }
}
