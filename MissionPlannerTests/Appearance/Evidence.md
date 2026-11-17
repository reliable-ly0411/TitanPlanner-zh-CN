# 外观定制测试记录

基线 `dd38ee30b51274b836af8039d415de0d67af597d`，独立分支 `feat/custom-window-icon-splash-20261117`；精确候选 SHA 见 [Issue #22](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/issues/22)。未发布、未合并。

## 主窗口不在 OpenForms 时的刷新修复

首个候选 `4f3f4610a32be571849c0dc2146010b6ab650308` 的 Windows 验证反馈：主程序和测试编译完成，存储测试 14/14、三语言共 18 个独立进程阶段通过；完整应用导入图标时普通 Form 更新，但主窗口托管 `Icon` 和原生 `WM_GETICON` 仍为旧值。实际主窗口不在 `Application.OpenForms`，首个候选仅枚举该集合而漏掉它。这些是旧 SHA 的验证反馈，不是后续修复的 Windows 验收结果。

修复使用主窗口构造时建立的 `MainV2.instance`，将其与 `Application.OpenForms` 合成去重快照；忽略空引用和已释放窗体。导入和恢复默认共用该入口。生产变更仅在 `DesktopAppearance.cs`，没有改变 EXE、安装器图标或主窗口启动/销毁流程。

新增 `WindowIconRefreshTests` 两项回归：一个已创建句柄但未 Show、因此不在 `Application.OpenForms` 的主窗口替身，与正常已显示窗口一同更新自定义图标及恢复默认；另验空/已释放主窗口不阻挡其他窗口。Windows 分支还核对 `WM_GETICON / ICON_BIG` 与预期 Icon 的原生句柄。为避免启动飞控/地图子系统，MSTest 通过反射调用生产的窗口赋值方法；隔离宿主另用 `MainV2` 的 Form 引用替身，从真实 Import / RestoreDefault 入口验证整个刷新链。替身未运行真实 MainV2 构造器。

使用 net472 API 引用的 Mono / Xvfb 定向验证：14 项存储测试、2 项窗口刷新测试、473 条 UI 断言、六个独立进程的 12 条读取断言全部通过。Windows 专用 `WM_GETICON` 断言在此环境不执行，须在新候选复验。35 项 Python CI 与资源检查仍通过。

负向对照将同一个回归宿主的 bridge 文件换回 `4f3f4610` 原文：普通窗体先确认已更新，随后准确失败于 `main window absent from OpenForms kept original icon after import`。修复后同一入口通过导入和恢复默认，且主窗口全程仍不在集合中。[正向与负向简明记录](Evidence/window-refresh-results.txt)。可用 `run-isolated.py --desktop-appearance-source <git-show导出的旧DesktopAppearance.cs>` 复跑负向对照；该对照预期返回非零。

新候选 Windows 定向复跑时，将下方 dotnet test 的 filter 改为 `FullyQualifiedName~AppearanceAssetStoreTests|FullyQualifiedName~WindowIconRefreshTests`，在独立/非交互桌面执行；还需完整应用实际导入与恢复默认，检查真实 MainV2 的托管 Icon、WM_GETICON 和可见图标。

## 首个候选 4f3f4610 的云端检查

- Debian 13 / Mono 6.12.0.199 / libgdiplus / 私有 Xvfb，使用 Microsoft `.NETFramework.ReferenceAssemblies.net472` 1.0.3 的真实 .NET Framework 4.7.2 API 引用定向编译后运行。没有系统级安装或注册工具。
- **14 项存储测试通过，0 失败、0 跳过**。同一 `AppearanceAssetStoreTests.cs` 测试体在本轮使用轻量 MSTest 断言兼容层运行；不是 Windows MSTest 执行记录。
- **468 条 UI 断言通过**：设置页三语言 zh-CN / zh-Hans / en-US，320 / 640 / 1024 像素客户区宽度，字体 100% / 150% / 200%，检查文本高度、横向范围、末条提示的滚动可达性及真实恢复按钮；另测当前窗口图标更新/恢复，以及启动页三语言 × 黑/白合成图片共六场景（默认字体）的独立署名区。字体放大不是 Windows 系统 DPI，绘图实际为 96 DPI。
- 六个独立进程按 seed → verify → damage → fallback → reset → defaults 执行，读取检查共 **12 条断言通过**。原图删除后仍读取副本；破坏副本后新进程和新 Splash 都回退默认；不改写坏设置证据；恢复默认后新进程不再报警。
- 256 像素 PNG 压缩 ICO、常见 256 像素 32 位 DIB ICO、透明像素/中心颜色通过；原 ICO 字节保持不变。未选中的损坏帧同样拒绝。
- PNG / JPEG / JPG / BMP、空文件、超大文件及巨大声明尺寸、扩展名伪装、非法/含外部实体 XML、路径穿越、设置锁冲突、各项独立恢复、不同 store 实例保存及备份保留通过。配置和日志哨兵保持不变。
- 三个新增桌面文件在 `LIB` 下定向编译为空库通过；不等于完整 Android/Mac 构建。
- Windows `NativeProbe.cs.txt` 已用上述 .NET Framework 4.7.2 引用编译通过，**尚未在 Windows 执行**。
- 35 项已有 Python CI 回归通过；资源校验通过：2297 条词典、96 个增强简体资源、原版中文基线、占位符及链接。`git -c core.whitespace=cr-at-eol diff --check` 通过；既有 CRLF 文件保持 CRLF。

核心和 UI 采用真实生产文件 `AppearanceAssetStore.cs`、`DesktopAppearance.cs`、`ConfigAppearance.cs`、`Splash.cs`、`Splash.Designer.cs`、`UiText.cs` 及实际启动资源、中文卫星资源。`ThemeManager.FormIcon` 从真实源码提取原样编译。首个宿主替身限于 `Settings.GetUserDataDirectory`、`Program.IconFile` 的初始默认来源及 `MyUserControl` 基类；窗口刷新回归另增加上述 MainV2 Form 引用替身。没有全应用入口、串口、遥测或飞控通信。所有替身和测试命令均可审阅。

简明结果在 [isolated-results.txt](Evidence/isolated-results.txt)。[黑背景启动页](Evidence/splash-zh-CN-Black.png)、[白背景启动页](Evidence/splash-zh-CN-White.png) 均来自合成测试，版本 `0.0.0.0` 是隔离夹具版本，不是产品版本。截图没有用户路径、位置、设备或私人素材；[截图 SHA-256](Evidence/screenshots.sha256.json)。

## 复跑隔离测试

需要现成的 Mono / mcs / libgdiplus 与私有 Xvfb。工具可独立解包到工作目录；无需系统级安装。

```sh
xvfb-run -a python3 MissionPlannerTests/Appearance/run-isolated.py \
  --output /tmp/titan-appearance-evidence \
  --framework-references /path/to/net472/build/.NETFramework/v4.7.2
python3 scripts/localization/verify_zh_hans.py
python3 -m unittest discover -s scripts/ci/tests -v
```

`--mcs` / `--mono` 可指定独立工具路径。运行器只写指定结果目录和随机命名的临时测试目录。未传引用目录时使用 Mono 自带 API；本轮上列结果使用的是 net472 引用。

## Windows Git 构建与原生待验

使用独立 checkout、精确候选 SHA、独立输出和结果目录；不要改用户已安装软件或抢占正在操作的桌面。默认的 Windows UI 探针要求 Guest Agent / 非交互会话或真正的 CI 会话；脚本不设置绕过标志。

```powershell
git fetch origin feat/custom-window-icon-splash-20261117
git switch --detach <Issue中记录的40位候选SHA>
git rev-parse HEAD
git submodule update --init --recursive
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/localization/build_windows.ps1 -OutputDirectory D:\TitanPlanner-appearance-test
dotnet test MissionPlannerTests\MissionPlannerTests.csproj -c Release --filter 'FullyQualifiedName~AppearanceAssetStoreTests|FullyQualifiedName~WindowIconRefreshTests' --logger 'trx;LogFileName=appearance.trx' --results-directory D:\TitanPlanner-appearance-results -p:TitanPlannerOutputDirectory=D:\TitanPlanner-appearance-test\ -p:GenerateResourceUsePreserializedResources=true -p:GenerateSerializationAssemblies=Off
powershell -NoProfile -ExecutionPolicy Bypass -File MissionPlannerTests/Appearance/run-windows.ps1 -BuildDirectory D:\TitanPlanner-appearance-test -ResultsDirectory D:\TitanPlanner-appearance-native-results
```

`run-windows.ps1` 编译反射探针，直接载入完整构建的 MissionPlanner.exe；三语言各六个独立进程验证保存、重开、缺失/损坏回退及恢复，只使用合成素材和其自己的数据目录。其结果不能由本轮 Mono 夹具替代。

- [ ] 精确 SHA 的 Windows Release 完整编译、上述 MSTest 与 NativeProbe；绑定构建 SHA / 测试结果及截图哈希。
- [ ] 完整应用中通过设置页实际选择文件和取消，重新启动应用后检查图标及启动图，分别恢复默认；包括保存目录只读、损坏图片及源文件移走。
- [ ] 当前窗口 / 新窗口 / 分离 3D 窗口、窗口任务栏按钮、Alt-Tab、固定任务栏项、开始菜单和 EXE 图标分别观察。验证设置没有改动已安装文件、快捷方式或注册表。
- [ ] 原生 100% / 125% / 150% / 200% 系统 DPI 与跨屏，深浅主题、小屏滚动、键盘访问。六张本轮启动图只代表 96 DPI 夹具。
- [ ] 大小/比例不同、透明、超宽/超高及含 EXIF 方向的图片符合已说明的范围；默认资源、两套中文与英文仍可辨认。
- [ ] 与其他候选整合后重新验证页面注册、主题图标入口和词典；保留各任务的独立提交。此分支不包含其他分支的 HUD、品牌、参数或布局修复。

当前环境没有 Windows 桌面入口；首个候选的原生反馈已在上方分开记录，本次主窗口刷新修复仍待新 SHA 的 Windows/GPD 复验。Issue 保持开放。没有发布、安装、快捷方式变更或系统级持久化。
