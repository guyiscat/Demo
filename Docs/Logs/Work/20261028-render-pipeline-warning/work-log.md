# 2026-10-28：Built-In Render Pipeline弃用警告核对

## 需求

用户打开项目看到Built-In Render Pipeline已弃用，询问是否影响项目。此次仅检查并解释，没有要求迁移管线。

## 本地证据

- ProjectVersion.txt为6000.6.7f1。
- GraphicsSettings.asset的m_CustomRenderPipeline为fileID 0。
- QualitySettings.asset的6个质量配置customRenderPipeline均为fileID 0。
- Packages/manifest.json没有URP依赖。
- 结论：此Demo仍使用来源工程的Built-In Render Pipeline，前次引擎升级时没有迁移到URP；警告符合当前配置。

## 官方资料与判断

Unity官方2026管线策略说明Built-In从Unity6.5开始标记弃用，尚未决定移除版本；6000.6官方手册明确仍支持至整个6.7 LTS生命周期。

- https://unity.com/topics/render-pipelines-strategy-for-2026
- https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/built-in-render-pipeline

弃用提示不等于加载、编译或运行失败。仅凭这条警告不需要重建项目或立即转换管线，可以继续编辑和查看PNG分层美术。前次在6000.6.7f1已有构建和实际渲染通过，但本次未重新测试，不把历史通过描述为当前现场运行保证。

项目后续开发宜评估URP 2D迁移，在正式大量制作材质与光效前，于备份/场景副本中处理管线配置、材质、相机和视觉验证。迁移会改变渲染配置，不应仅为消除提示未经验证直接转换整个工程。

## 改动、验证与回滚

只新增本工作日志并追加Docs/CHANGELOG.md。原CHANGELOG备份在Docs/Backups/20261028-render-pipeline-warning/。未改Project工程文件、场景、素材、管线、试玩或压缩包，未启动Unity或执行构建测试，未提交/推送Git。恢复备份CHANGELOG可撤回文档修改，新增日志保留用于追溯。
