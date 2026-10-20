# 上游同步、验证与发布

源代码同步入口为 `.github/workflows/sync-upstream.yml`，Windows 构建入口为 `main.yml`。

- 每天 03:17 UTC 仅合并上游 `Titan-Dynamics/TitanPlanner/master`，保留本仓历史和双中文版本。普通 push/PR 只执行 Python 资源、打包和策略回归，不运行本机编译、GUI、MSI 或发布。
- 同步候选先运行轻量检查；主分支仍是准备时的起点才普通快进。冲突、上游自动化策略改动或并发主分支更新均停止，不强推、不覆盖用户提交。
- 定时同步后只识别新建且正式发布的 `TitanPlanner-<四段版本>` GitHub Release。`development-build`、draft、prerelease、单纯新 tag 或上游 HEAD 变化均不会启动自动重型构建。
- 2026-10-20 14:06:15 UTC 已存在的 Release ID 记为基线，包括当时最新的 `TitanPlanner-1.3.9789.1792`；不补建历史发布。每个后续 Release 固定其 ID、tag 的 peeled commit、下游补丁起点/提交/摘要与组合后的源码 SHA。源码在构建作业内组装，不推送生成的工作流改动，也不拿上游当前 HEAD 冒充已发布源码。
- Windows 候选仍须通过两套中文资源、翻译运行、GUI/离线回归、Release/Debug 构建、依赖审计、ZIP 与 MSI 安装/修复/卸载及个人数据保留检查。Linux 候选仍执行原生构建与运行依赖测试。成功后保留原始工件及 SHA-256 证据，不在提升时重建。
- 当前正式上游候选的自动发布明确阻塞：组合源码尚无经过审阅的远端不可变提交锚点和发布版本/说明映射。通过构建不等于已经发布；不能以下载附件或重建代替该缺口。`verify-release-candidate.yml` 可核验原 run、源码与工件，但会明确失败退出以报告该阻塞。

## 三个独立的手动入口

1. 同步：手动运行 `sync-upstream.yml`，默认 `action=sync`，只同步。`check-releases` 才额外识别新正式发布；`retry_release_id` 仅重试已失败的新发布，不补建历史。
2. 构建：在目标提交所在分支运行 `main.yml` 或 `linux.yml`；不会同步或发布。Android/macOS 与地形打包仍是独立手动入口，不进入 Windows 发布链。
3. 发布：`promote-release.yml` 填写成功的手动 `main.yml` run ID 和精确 40 位 source SHA。只接受本仓 master 的成功手动构建、完整 Windows 检查及匹配 run/attempt/source/workflow 的原始工件与摘要，拒绝 PR、旧 push 或其他仓库工件。直接提升已验 ZIP/MSI，不编译、不重打包、不移动主分支、不覆盖既有 tag/附件。成功候选必须仍是 master 的祖先；没有新版本时不创建重复发布。

发布保留不可变 `v<程序版本>-zh2-<源码前12位>` tag 和现有预发布分类。自动化仅使用仓库既有 `GITHUB_TOKEN`；构建作业只读，写入 ledger、同步或明确手动发布的作业独立授予所需权限。未新增凭据、签名或仓库设置权限。

成功识别记录保存在独立 `release-policy-state` 分支，不移动产品分支。失败自动重试最多三次且沿用原固定来源；成功记录去重。移动/重命名/撤回或重复使用的上游 Release/tag 会停止。并发队列不取消正在运行的工作。GitHub 的定时运行可能延迟或因仓库长期不活跃被停用。

## 本地复现

在 Windows 的 VS Code 终端中，使用 .NET 8 SDK、Git 和 Python 3.12：

```powershell
python scripts/localization/verify_zh_hans.py
python -m unittest discover -s scripts/ci/tests -v
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/localization/build_windows.ps1 -OutputDirectory D:\TitanPlanner-test
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ci/test_windows.ps1 -BuildDirectory D:\TitanPlanner-test -ResultsDirectory D:\TitanPlanner-test-results
```

界面探针默认要求非交互会话，避免显示测试窗体干扰正在操作的桌面；GitHub 托管测试环境通过 `CI=true` 明确标识。需要插拔飞控并扫描串口的上游测试标记 `Hardware`，自动流程明确排除，实机验证时单独运行。网络相关上游测试标记 `Network`，可给 `test_windows.ps1` 加 `-IncludeNetwork` 单独运行，外部服务中断不会被误报为离线单元测试通过。

发布打包从干净构建目录生成，排除用户配置、航迹日志和地图缓存；包含两套中文卫星程序集及原生依赖。MSI 使用独立的 UpgradeCode，不替换上游 Mission Planner；产品版本随工作流运行编号递增，降级被阻止。安装包不自动安装驱动或启动飞控软件。应用内更新入口指向本仓库 Releases，避免上游二进制覆盖中文改动。

## 发布版本命名

MSI 和 ZIP 沿用固定附件名 `TitanPlanner-zh-CN.msi/zip`，以不可变 tag 区分版本，发布标题和标签同步携带程序版本。版本自动读取 `MissionPlanner.csproj`，并与 `AssemblyFileVersion` 核对，不使用依赖包版本；不一致时停止打包。`BUILD-INFO.json` 和 `build-info.json` 同时记录版本与提交。MSI 内部用于升级排序的安装器版本仍随工作流编号递增，并单独记为 `msi_version`。历史发布名称保持不变。
