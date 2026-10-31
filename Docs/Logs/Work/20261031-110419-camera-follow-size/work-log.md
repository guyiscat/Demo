# 拉远普通跟随镜头

## 需求与参数

2026-10-31，用户要求拉远一点，使玩家看到更多画面，并说明手动调节方法。将当前SeasideArtPreview及Seaside01的SeasideCamera.tuning.followSize从10改为12，可见宽高各增加约20%；不修改原有bg3边界、平滑参数或全图适配。

## 备份与未保存内容

备份在Docs/Backups/20261031-110419-camera-follow-size/before/，包括两份已保存场景和修改的说明文档。before-hashes.json记录工程文件的修改前SHA256。

检查Unity窗口发现SeasideArtPreview已带未保存标记。通过computer-use技能仅修改当前Inspector中的Follow Size为12，清除临时搜索过滤并保留相机参数面板；没有按Ctrl+S、切换场景或重载，没有代为保存用户原有未保存编辑。磁盘场景采用单字段文本修改，逐字节验证除followSize的10→12外完全一致。游戏构建只能包含这些已保存场景，不含用户仍在内存中的其他编辑。

## 改动

- SeasideArtPreview.unity、Seaside01.unity：各一处followSize 10→12。
- Docs/CameraBounds.md：补充手动调节入口、数值方向、Play内试调和全图/跟随区别。
- Docs/CHANGELOG.md：增加本次条目。
- C#相机算法、动作参数、素材、碰撞和复活点均不修改。

## 验证与交付

scene-parameter-checks.json与validation.json均通过：两份场景只有该数值发生变化，其它既有工程文件哈希不变，构建场景与当前磁盘场景一致。

隔离镜头专项41143项断言通过，原有镜头舒适度检查通过；follow-edge.png和overview-letterbox.png已渲染。构建退出0，新的运行回归、校准及美术预览启动检查均退出0、无超时。Play64更新148个文件并逐项哈希验证，旧试玩完整保留于before/Play64。完整结果见build-status.json、runtime-checks.json、play-deployment.json。

交付ZIP使用PackageRelease.ps1更新，最终包内容及CRC检查结果在打包后生成的package-verification.json中，不包含在同次ZIP快照内。

沿用已有CameraBackgroundBoundsChecks隔离构建入口。旧标准流程因用户预览地图与原白盒不同而失败的已知限制，见前次20261030-222238-bg3-camera-bounds记录，本次不重复运行该历史一致性检查，也不修改地图以通过它。没有人工全关通关或Git提交/推送。

## 手动设置与回滚

当前预览：Seaside/Systems/MainCamera → Seaside Camera → Tuning → Follow Size。数值越大，视野越广；12为当前值，14更远。停止Play后填写并按Ctrl+S保存；Play内试调退出后通常会还原。不要只改原生Camera的Size，因为运行时会覆盖。Tab全图模式不使用此跟随大小，应按Tab切回跟随观察。

回滚时只将两份场景和当前Inspector中的Follow Size改回10，避免整份恢复场景覆盖之后手调。确需文件对照时使用before/同路径副本；新试玩替换前的完整目录将在本次备份保留。用户原有未保存编辑一直保留在当前Unity会话中，需要用户自行决定何时保存。
