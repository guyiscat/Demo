# 2026-10-28：比较URP迁移与回退Unity6.4

## 需求与范围

用户询问如何解决Built-In弃用警告，以及迁移到6.4是否合适。本次仅说明和提出建议，不自行安装编辑器、降级工程或改变管线。

## 核对

当前Demo/Project为6000.6.7f1，本机仅安装此编辑器。此前核对的Graphics/Quality没有SRP资产，依赖中无URP，仍使用Built-In。原game1来源工程为6000.4.12f1，原件保留。

官方Built-In弃用起点为6.5。回到6.4通常能避开这项新提示，但这仍保留旧管线，不能替代渲染管线迁移。当前Demo已经由6.6导入、保存，回退6.4属于降级，应考虑场景/设置序列化及依赖兼容性，不能保证无损。

## 建议

建议保留目前已验证的6.6编辑器，单独迁移URP并选择2D Renderer；编辑器版本更改与管线更改是两个独立决定，不同时进行更便于定位问题。备份当前未提交修改，再安装匹配编辑器的URP包，配置管线资产和质量档位、处理Sprite/残像/预制件材质及相机，重新执行实际渲染、已有运动回归和Windows构建。

项目包含Camera.Render离屏截图路径，迁移时需核对其URP适配；运动回归通过不代表截图和画面通过。不将后续方案写成已完成操作。

如果必须使用6.4，优先在game1的6000.4.12f1原始工程副本上工作，另行迁入并验证之后新增的内容，不直接覆盖现有Demo，也不只改ProjectVersion.txt。新素材仍需保留最新副本。

## 资料

- Unity2026渲染管线策略：https://unity.com/topics/render-pipelines-strategy-for-2026
- URP官方迁移流程：https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/migrating-from-birp-workflow
- Graphics默认管线配置：https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/comp-manager-group/class-graphics-settings
- 旧版编辑器打开新版工程的数据损失提示：https://docs.unity3d.com/ru/2020.1/Manual/GettingStartedOpeningProjects.html
- Unity6支持政策：https://unity.com/releases/unity-6/support

## 实际改动、验证及回滚

只读工程版本、安装目录、构建代码及截图代码；查询上述官方资料。新增本记录并追加CHANGELOG，原总表已备份到Docs/Backups/20261028-urp-versus-64/。没有改工程、素材或试玩，没有执行构建/游戏测试，没有提交或推送。恢复本次备份总表即可回滚旧文档修改。
