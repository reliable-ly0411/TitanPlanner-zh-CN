# 窗口图标与启动画面

在“配置/调试”内的“窗口图标与启动画面”独立页面，选择文件后立即校验并保存；取消文件选择不更改设置。页面显示预览，并提供分别恢复默认的按钮。该页沿用“地面站设置”入口的显示权限，未连接飞控时也可使用。此功能只修改桌面外观，不涉及飞控命令。

## 实际生效范围

| 对象 | 当前实现与入口 | 本功能的生效方式 |
| --- | --- | --- |
| EXE 的内嵌图标、资源管理器中的 EXE | `MissionPlanner.csproj` 的 `ApplicationIcon=mpdesktop.ico` | 设置不会改写 EXE。替换内嵌图标必须另行更换构建输入并重新编译；资源管理器还可能缓存旧图标。 |
| 已打开的应用窗口、通常由窗口表示的任务栏图标 | `ThemeManager.FormIcon`，已有 `Program.IconFile` 来源为安装目录 `icon.png` 或内嵌 `mpdesktop` | 导入后向当前进程已打开窗体赋值；后续经过主题入口的窗体读取同一图标。重启保留。FixedDialog、插件自行覆盖图标等不承诺统一。Windows 固定项、分组及缓存可能继续显示快捷方式图标，须原生验证。 |
| 中文 MSI 在“应用和功能”及开始菜单的图标 | `scripts/ci/package_msi.py` 从 EXE 提取 `AppIcon`，快捷方式引用该资源 | 设置不会修改安装器或已安装资源；需要重建并正常安装新版安装器。 |
| 旧 WiX 安装器快捷方式、用户自行创建的快捷方式、固定任务栏项 | 旧 `wix/Program.cs` 指向 EXE；快捷方式可另存自身图标 | 不自动修改。用户可在快捷方式属性中另选自己的 ICO；或在构建/安装变更后重新创建、重新固定。单纯重建快捷方式仍指向同一 EXE 时，不会自动采用此运行时设置。 |
| 启动画面 | `Splash.Designer.cs` 的 `titanLogo.Image` 为默认图，非 `Program.SplashBG` | 导入 PNG / JPEG / BMP 后下次创建启动窗体时读取。保持比例，完整缩放，不裁切；不会在当前会话重新弹出启动页。 |

Windows 的 `Form.Icon` 决定窗体在控制框及任务栏中的表示，但并不等于改写 Shell 的快捷方式/固定项资源，见 [Microsoft Form.Icon 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.icon?view=netframework-4.7.2)。实际任务栏表现仍以该候选的原生操作为准。

## 格式和限制

- 图标：真正的 `.ico`，最大 4 MiB，1–64 帧，每帧宽高 1–256 像素。检查 ICO 目录、帧边界、声明尺寸，并逐帧解码；可选帧损坏也拒绝。推荐同时包含 16、32、48、256 像素版本。CUR、PNG 改扩展名等不接受。
- 启动图：真正的 `.png`、`.jpg` / `.jpeg` 或 `.bmp`，最大 16 MiB，宽高各不超过 4096。先检查格式与尺寸，再解码为脱离源文件的 32 位位图。GIF、SVG、TIFF 不在本轮范围；不应用 EXIF 方向或动画。
- Mono 的图标解码器不识别 PNG 压缩帧，并忽略字节尺寸为 0/0 的 256 像素项。仅在 Mono 显示时将 PNG 转为内存 DIB，256 像素帧使用内存中的 32 像素显示副本；原文件及导入副本字节保持不变。常见 256 像素 32 位无压缩 DIB 同样处理，其他 256 像素 DIB 编码会明确拒绝；Windows 使用系统原生 ICO 解码。此兼容行为基于 [Mono Icon 实现](https://github.com/mono/mono/blob/main/mcs/class/System.Drawing/System.Drawing/Icon.cs)，不是对所有 ICO 编码的承诺。
- 极大、损坏、不支持或扩展名不符的输入被拒绝，原选择保留。外观错误不显示用户文件的绝对路径。

## 保存、恢复和异常回退

只在用户操作保存/恢复时写入 `Settings.GetUserDataDirectory()/appearance/`。该目录属于本软件原有的个人数据目录，不是安装目录；外观设置适用于当前用户的所有配置档。启动读取不会创建或修复任何文件。

每次导入使用新的 `SHA256-随机标识.扩展名`，不覆盖源文件或先前副本。`settings.xml` 只允许同目录下符合格式的文件名，禁止任意路径；XML 禁用 DTD/外部实体，长度限制 4096 字节。读取副本时核对文件名中的 SHA-256，再校验和解码。

保存先校验输入，然后取得目录内的写锁并重新读取其他项；写入并刷出新副本与临时设置，最后原子移动/替换设置文件。替换保留唯一命名的 `settings.xml.backup-*`。锁冲突、无写权限或提交失败会保留旧设置；不以截断旧文件作为后备写法。已写入但未被指针引用的副本可留下，保留证据与用户素材。图标保存成功但个别运行窗口刷新失败时明确提示重开窗口，不把已保存误报为未保存。

恢复默认只清空对应选择，保留另一项、全部导入副本、备份、原图、`config.xml`、配置档、地图和日志。默认窗口图标沿用该安装原有的 `icon.png` / 内嵌图标规则；默认启动图为原有嵌入资源。文件丢失、内容损坏、非法设置或解码失败，按项回退默认；设置文件整体无效时两项都回退，不自动覆盖损坏证据。

副本和备份没有自动清理或总容量上限。若需人工清理，应先退出程序并备份该外观目录；不要删除原软件的个人数据目录。

## 署名、默认资源与许可

启动页仅将本次修改者明确为用户要求的 `lyy0405`。已存在的作者 Michael Oborne 保留，另外一行标明上游 TitanPlanner 的 Mohammad Adib；不新增或推断版权持有人。署名和版本显示在不透明、按内容计算高度的深色页脚，避免任意自定义图片遮挡文字。

`Splash.Designer.cs`、`Splash.resx`、`mpdesktop.ico`、`Properties/Resources*`、原版 `zh-Hans` 资源和 `COPYING.txt` 均保留。可见署名在构造启动页时设置，不改写上游原图片或既有历史署名文本。用户导入素材的使用权限由其自身来源决定；不会将用户素材纳入本仓上游资源或改换原有许可。

## 实施和验证

主要实现为 `AppearanceAssetStore`（导入、验证及原子保存）、`DesktopAppearance`（运行窗口桥接）、独立 `ConfigAppearance` 页面与 `Splash` 布局。窗口刷新显式包含 `MainV2.instance`，与 `Application.OpenForms` 去重，覆盖主窗口暂不在后者集合中的情况；导入与恢复默认使用同一路径。共享入口只在 `SoftwareConfig` 插入一处页面注册、在 `ThemeManager.FormIcon` 插入桌面偏好读取；新桌面代码均有 `!LIB` 防护。不修改 `ConfigPlanner`、HUD、右上角品牌入口、安装器、自动发布或系统注册表。

测试范围、复跑命令和原生待验项见 [外观测试记录](../MissionPlannerTests/Appearance/Evidence.md)。当前候选和精确 SHA 由 [Issue #22](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/issues/22) 记录；功能分支不等于已发布安装包。
