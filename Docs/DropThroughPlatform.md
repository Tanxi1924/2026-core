# 双体角色下跳机关

站在机关上，按一次 S 或 ↓ 下穿脚下平台。长按不会连续穿过下一层；人物保持重力、水平运动与旋转。普通地面不支持下跳。

## 安装与使用

1. 在现有角色 Prefab 根节点添加 `TwinBodyDropThrough`，与 `TwinBodyCharacter` 放在一起，再保存 Prefab。新建角色时，原有 `Tools > Character > Build TwinBody Character Prefab` 菜单已自动添加此 Ability；不要为了安装能力而重建已有角色，以免覆盖其配置。
2. 执行 `Tools > Environment > Create Drop Through Platform`，在当前场景创建绿色平台。调整位置后，可以拖入 Project 保存为 Prefab。
3. 机关根节点有 BoxCollider2D Trigger 与 `DropThroughPlatform`；Surface 子节点是实际实体平台。Trigger 应覆盖表面上方的接触区域，Surface 引用不能指向 Trigger。
4. 保持平台水平、静止。宽度和厚度通过 Surface 的 BoxCollider2D 调整，同时调整 Visual 与根 Trigger。不要与普通地面重叠；平台下方留足整个角色的通过空间。
5. 确认 Player 与 Platforms 在 Physics 2D 层碰撞矩阵中允许碰撞。无需修改全局碰撞矩阵，也无需使用 OneWayPlatforms 层。

此版本是按键下穿的实体平台，从下方接近仍会碰撞；没有新增从下往上穿透的行为。适配对象为当前 TwinBody 刚体角色，不是原版 CorgiController 角色。

## 与梯子结构的对应

- `DropThroughPlatform` 对应 `Ladder`：Trigger 将交互对象注册给角色 Ability。两个头和连接件分别进出时，最后一个碰撞体离开才取消注册。
- `TwinBodyDropThrough : CharacterAbility` 对应角色侧能力：使用 Corgi 的 Early/ProcessAbility、AbilityAuthorized、Start/Stop Feedback 与死亡/禁用清理。
- 通过 Collider2D.Cast 向下短距离检查所有身体碰撞体。仅在支撑来自已注册的机关时触发；同时踩着普通地面时拒绝。并排的机关可同时下穿。
- 仅调用角色与当前平台碰撞体之间的 IgnoreCollision；其他人物、下层平台仍正常碰撞。穿过后按整个身体的包围盒恢复，保留原本已被其他逻辑忽略的碰撞对。
- 不复制原版 CharacterLadder 的 CollisionsOff/GravityActive(false)，因为本项目禁用了 CorgiController。

## Inspector

- ReadInput：允许读取 S / ↓。
- GroundProbeDistance：脚下支撑检测容差，默认 0.04。
- InitialDownSpeed：触发时最小向下速度，默认 2，不改变水平速度或角速度。
- Clearance：完整离开平台后恢复碰撞的间隔，默认 0.03。
- AbilityPermitted / Blocking States / Feedback：继承 Corgi 标准字段。

## Unity 中的验收步骤（尚未运行）

1. 双头水平站立，分别按 S、↓，检查整个人体下落且不在中途卡头。
2. 旋转角色，仅一个头支撑或连接件支撑，检查下跳。
3. 两层平台间留足身体高度：长按 S 应停在下一层，松开再按才能再次下穿。
4. 普通地面、平台侧面、平台下方按键不应触发。一个头踩机关、另一个头踩普通地面也不应触发。
5. 两个角色共用同一平台，只有按键者下落。两个头跨两个相邻机关时，应同时忽略这两块支撑。
6. 下穿中禁用角色、死亡并复活，检查碰撞可恢复；禁用再启用能力后，在机关上再次触发。
7. 下穿中沿水平方向离开，重新落回机关仍能站立。

当前环境没有 Unity 编辑器或 C# 编译器，未执行编译、Play Mode 或物理验收。需在 Unity 中完成以上检查后再合并。

API 参考：https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Collider2D.Cast.html
