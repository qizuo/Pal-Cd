# F 盘通用特效素材

从 `F:/chiyu/BubShoot/Assets/Effect` 筛选并按内容去重，导入 31 张 PNG（约 6.8 MiB）和 31 个配套材质。每个文件的原路径、尺寸与 SHA-256 见 `SourceManifest.json`。

- Fire：火焰、旋涡及火焰序列图，可用于火箭、落日震和首领技能。
- Blast：冲击、斩击、命中与地面爆发。
- Smoke：烟雾、扬尘及序列图。
- Glow / Ring：聚能、闪光、范围环与提示圈。
- Trail / Line：箭矢、闪避与疾跑拖尾。
- Turbulence / Particle：扰动、渐变及熔岩辅助纹理。

Materials 与 Textures 按类别对应。材质使用本项目 `Houyi/SunburstParticles` 着色器，烟尘默认透明混合，其余默认叠加混合。黑底烟尘素材需要时可将材质 `_DstBlend` 改为 1，使用叠加混合。

部分贴图是多帧图集，使用时需在 ParticleSystem 的 Texture Sheet Animation 中按实际网格设置行列；直接使用材质会显示整张图集。素材库尚未绑定到技能或场景。

重新生成导入 GUID，避免与现有资源冲突。未复制旧项目的脚本、场景和插件，也未导入依赖不完整的预制体。F 盘另外发现 UnityLobby 的棋牌 UI 粒子、EpicVictoryEffects 的金币与钞票特效，暂未纳入本项目。
