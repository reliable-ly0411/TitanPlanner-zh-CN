# TitanPlanner-zh-CN 仓库补充

本文件仅定义本仓库的分支、版本映射与上游例外；与通用规范一并读取。具体任务、检查结果和整改进度不写在本文件。

## 分支映射
- `Titanmain` 精确镜像 `Titan-Dynamics/TitanPlanner/master`；`missionmain` 精确镜像 `ArduPilot/MissionPlanner/master`。镜像不加入汉化、流程或文档提交，仅快进；上游重写历史时停止并报告。
- `zh-CN` 为汉化稳定维护线；`zh-CN-dev` 为汉化开发线，接收 Titan 上游普通合并及汉化改进。开发线通过验收后才普通合并至稳定线，不将 MissionPlanner 直接混入 Titan 汉化线。
- 稳定线晋级或维护提交后，自动将稳定历史回合并开发线；可快进时直接快进，否则普通合并并运行轻量检查。冲突由维护者处理，不重写历史。
- `release-policy-state` 为发布状态数据，不作为产品线或临时分支删除。旧 `master` 历史保留在汉化长期线中。
- 纯镜像更新及开发线同步不等于验收或发布。发布只提升稳定线已验原包；旧 `master` 原包仍按相同证据门槛兼容。专项使用 `docs/`、`fix/` 等分支。

## 版本与产物映射
- 程序版本来自 `MissionPlanner.csproj`，与 `AssemblyFileVersion` 核对；Windows 四段文件版本和 MSI 升级排序版本单独记录。
- 历史标签结构为 `v<程序版本>-zh2-<commit12>`；这是先行标识，哈希并不提供时间递增语义。新有序映射未约定，不用示例代替约定。
- 下载名遵循上游固定名称结构：`TitanPlanner-zh-CN.zip` 与 `TitanPlanner-zh-CN.msi`。

## 上游与适用例外
- 直接上游为 [Titan-Dynamics/TitanPlanner](https://github.com/Titan-Dynamics/TitanPlanner) 的 `master`；保留两套中文资源及其各自归属。
- 本仓自动化由 `AUTOMATION.zh-CN.md` 描述；其发布触发行为属于仓库特定例外，文档维护不能被解释为新软件发版权限。
- 程序、依赖库、MSI 安装器版本是不同身份；真实飞控/飞行验收不由构建成功替代。
