# 蓝衣角色射箭演示

打开 `Assets/ArcheryDemo/ArcheryDemo.unity`，点击 Unity Play。

- 默认自动循环：举弓 → 拉弦蓄力 → 放箭 → 收弓。
- 按 A 切换自动演示。关闭自动后，按住鼠标左键或空格举弓、蓄力，松开放箭。
- 轻点也能完成一次最小蓄力射击；满蓄力可以持续保持，直到松开。
- 可复用角色：`BlueRobeArcher.prefab`。四段时长在 `ArcherDemo` 组件中调整。

模型来自截图中的 `Assets/Prefab/PlayerCharacterPrefab.prefab`（慕容紫英蓝衣模型）。这是独立的动作原型，使用原始 Bip001 骨架和双臂 IK 实时驱动，未生成美术手工动画剪辑。弓、弦、箭为演示用几何体；箭飞行不包含战斗伤害系统。

外部逻辑可调用 `BeginDraw()`、`ReleaseDraw()`，并订阅 `ArrowReleased(float charge)` 事件接入实际攻击。`CurrentPhase` 与 `Charge` 可用于 UI/音效。当前场景及原角色资源不需要改动。

如需重新生成，可选择 Unity 菜单 `Tools > Archery > Create Blue Robe Demo`。该菜单会重建演示目录中的场景和角色预制体。
