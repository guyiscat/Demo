# 新增字体与PSD识别

根目录Assets是用户提供素材所在目录，Unity运行工程仍为Project6.4。原始素材保留，不修改源PSD或字体。

AlibabaPuHuiTi-3-55-Regular.otf为阿里巴巴普惠体3.0 Regular字重，复制到Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf作为可打包的动态字体。原件与运行副本字节一致。GameSystems.systemFont是所有现有/后续关卡的公共入口；GameUIStyles为HUD、帮助、暂停、完成、交互和菜单系统面板使用该字体，GameTypography在场景加载时统一世界中的TextMesh标签。已有主题图片内的文字保持图片原样。

IMG_2979.psd：2578×1153，带透明通道，画面中横向排列6个同一角色的连续奔跑姿态，是角色奔跑动作帧源图。

IMG_2981.psd：1411×707，带透明通道，横向排列5个角色姿态，包含空中展开披风、落下/收身及站立姿态，属于跳跃、下落和站立动作参考帧。

上述识别基于PSD合成预览。PSD是可编辑图像源文件，本身不是已配置好帧率、切片和动画状态机的Unity动画；本次仅识别，不替换现有角色或自动猜测每帧时序。两个PSD预览留在本次工作日志目录。
