# 2026-10-30：场景恢复备份弹窗核对

用户打开SeasideArtPreview时出现Recovering Scene Backups，询问原因。截图文字明确说明Unity检测到上次编辑器会话的场景备份，并询问是否复制保存在Assets/_Recovery。

只读核对发现Project6.4/Temp/__Backupscenes/0.backup存在，大小459204字节，修改时间2026-10-30 10:54:31；当前正式场景Assets/Scenes/SeasideArtPreview.unity为1702842字节，修改时间10:02:17。上次editor-reopen.log确有Loaded scene 'Temp/__Backupscenes/0.backup'，说明恢复状态曾被加载。不能仅凭该提示确定是崩溃、强退或哪一次会话结束造成，也不能断言是重命名/脚本故障。

已额外复制0.backup、当前正式场景及meta到Docs/Backups/20261030-scene-recovery-prompt/，保留两种来源，不覆盖或删除临时恢复文件。0.backup不是文本YAML，因此不能用文本对象数或名字搜索判定其内容是否新旧；recovery-file-comparison.json中的文本匹配仅用于格式识别，不作场景差异结论。

进一步从已交付Seaside-Unity64-Hierarchy-Project.zip读取正式预览场景计算SHA256，当前磁盘场景与最后交付版本相同，记录于saved-scene-verification.json。这只证明已保存的文件一致，不代表恢复备份里没有用户未保存的编辑。

建议在弹窗选Yes先保留恢复副本，再从Assets/Scenes打开正式场景。若恢复副本有需要的未保存编辑，应比较后迁回，不直接覆盖整理后的场景。该提示本身不是编译报错或场景损坏的证明。

本次未点击用户弹窗、启动或关闭Unity、替换场景、恢复旧备份、改脚本、构建或测试。只新增诊断记录/保护性副本并追加CHANGELOG；原总表已备份于本次Backups目录。
