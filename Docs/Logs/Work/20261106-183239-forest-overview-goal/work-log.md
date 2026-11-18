# 森林全景遮挡与终点触发修正

日期：2026-11-06（Asia/Shanghai）

## 需求与原因

保留 Tab 近景摄像头受瀑布限制；全景恢复另一侧黑布遮挡，必须整身穿越后才交换可见侧。用户发现到达绿色终点没有结束：ForestGoal 额外要求 wheel.UnlockCount==2，但原场景 forceRotation=true 只控制水车转动，因此可到达终点但未满足隐藏门槛。按原到达终点的关卡规则，移除此额外通关门槛，双解锁仍仅控制正常水车启动。

## 改动

- ForestWaterfall：近景沿用已有相机边界；全景启用现有 FarSideOcclusion（不创建物体或场景），依 VisibleSide 在瀑布远缘遮住另一侧；全景取景框仍覆盖原地图，只有另一侧内容变黑。黑布不盖来时一侧，整身穿越才切换。切回近景即禁用黑布。
- ForestGoal：玩家触碰绿色旗帜即调用既有 SeasideSession.Complete，保留暂停/死亡/已完成等输入阻塞保护，以及 OnTriggerEnter/Stay；不再要求两个水车解锁点。保留序列化 wheel 字段以兼容原场景。
- ForestHUD：帮助及 Tab 提示与实际规则一致。
- ForestRuntimeChecks、ForestEditorChecks：更新相应期望，覆盖双向全景黑布边界/整身穿越/切换、0/1/2 解锁与预览状态的终点、暂停后站在终点恢复时触发。
- Docs/ForestWhitebox.md、CHANGELOG：同步新规则，旧验证标为上一轮。

## 保护与执行

用户授权在原场景继续修改。Unity 原工程仍打开，不保存/重载/关闭用户编辑器。无需改写现有 ForestWhitebox.unity：只更新其已有组件脚本；不新建关卡场景、不重新生成地图。修改前源码/场景/参数/试玩/旧双 ZIP 已备份至 `D:\UnityProjects\Demo\Docs\Backups\20261106-183239-forest-overview-goal`。protected-before.json 记录所有场景、设置、地图、Seaside 脚本和 Build Settings 哈希。

在独立项目副本运行 Tools/BuildAndTest.ps1 -ForestWhitebox，验证成功后同步森林试玩并运行 Tools/PackageRelease.ps1 -ForestWhitebox。

## 实际结果

第一次 Tools/BuildAndTest.ps1 -ForestWhitebox 机制验证通过（其后检测到外部保存并以最新场景重建，见后续段落），原始日志在 `Docs/Logs/Build/20261106-183457-421/`；editorExit=0，forestExit=0，BuildSucceeded。实际执行：森林编辑器1874 PASS、海边基础40 PASS/回归203 PASS、森林运行106 PASS，全部0 FAIL。test-summary.json 记录实际本轮报告，未以旧结果冒充。未发现 C# error CS/warning CS。

新增检查覆盖全景左右另一侧黑布的精确边界和透明度、双向未完全穿过保持原侧、整身穿过才切换、Tab 切回近景立即关闭遮罩并恢复边界，以及预览0解锁、正常0/1/2解锁、暂停中不结束与恢复后 OnTriggerStay 结束。原顺时针/有限摩擦/跳后重落/普通跳向终点/5复活点等验证继续通过。

D3D11 成品实际 Camera.Render/RenderTexture 抓取1440×900，进程退出0。已查看 visual-qa/contact-sheet.png 的全景左右黑布以及近景左右边界，结果符合要求。此抓图不包含IMGUI HUD，不作为通关弹窗视觉验收。

第一次核对时，37 个地图、场景、设置、Seaside 源码与 BuildSettings 哈希一致；随后检测到外部保存的两项场景调整，最终以 protected-latest-scene-baseline.json 为基准，见下文。本轮自身无需写入任何.scene。source-validation.json 确认所有 15 个森林运行/编辑器源码与实际编译版本一致。

同步更新森林试玩，使用 Tools/PackageRelease.ps1 -ForestWhitebox 更新双ZIP；完整性与关键文件哈希见 packaging-verification.json。未进行人工全路线通关。

## 回滚

仅从上述备份恢复本次森林组件、验证脚本、文档、Play64/Forest 及森林双ZIP，不重置整个 Git 工作区或恢复所有场景。保留已有未提交工作。未提交/推送/发布。

## 收尾检测到外部场景保存

第一次构建已通过并完成视觉检查，但打包收尾哈希核对发现18:37:39原Unity工程的ForestWhitebox.unity被外部再次保存。分块对比仅两处：角色位置(4.2,5.2)→(3.2,5.1)，近景followSize 14→18。未修改或覆盖这些外部更改。检测结果在 protection-drift-diagnostic.json 与 external-scene-change-blocks.json；保留最新场景快照在备份的 ExternalSceneSnapshot/。因此第一次发布完整性断言未通过，双ZIP首次生成物保留在 Packaging 历史，未将其视为最终验证通过。以最新场景建立 protected-latest-scene-baseline.json 并重新构建、截图、同步试玩和双ZIP。其余36个保护文件仍与工作前相同。

## 最终验证与交付

最新场景快照构建 `Docs/Logs/Build/20261106-184003-242/`：editorExit=0、forestExit=0、result=PASS；森林编辑器1874、海边基础40/回归203、森林运行106，全部0 FAIL。没有修改森林源码，仅同步外部保存的原场景快照，因此执行第二次构建以确认近景参数18仍满足边界和机制。

最新成品D3D11抓图在 visual-qa-latest/，退出0；已检查 contact-sheet.png 的双向全景黑布与近景边界。最终 protected-after.json 与最新场景基准的37项均一致，其中36项仍与任务开始前完全一致；latest-scene-validation.json 确认原磁盘场景与实际编译场景相同。本次从未写入原场景。

同步最终森林试玩和实际执行的新报告，重新运行 PackageRelease.ps1 -ForestWhitebox；最终双ZIP CRC与场景/源码/程序集/文档哈希核对记录见 packaging-verification.json。首次打包历史仍保留。
