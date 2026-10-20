# TitanPlanner-zh-CN 仓库补充

本文件仅定义本仓库的分支、版本映射与上游例外；与通用规范一并读取。具体任务、检查结果和整改进度不写在本文件。

## 分支映射
- 实际维护入口为 `master`；独立长期开发线名称未约定。
- 本仓汉化维护线与上游原有分支说明分开；专项使用 `docs/`、`fix/` 等，未经确认不创建或重命名长期线。

## 版本与产物映射
- 程序版本来自 `MissionPlanner.csproj`，与 `AssemblyFileVersion` 核对；Windows 四段文件版本和 MSI 升级排序版本单独记录。
- 历史标签结构为 `v<程序版本>-zh2-<commit12>`；这是先行标识，哈希并不提供时间递增语义。新有序映射未约定，不用示例代替约定。
- 下载名遵循上游固定名称结构：`TitanPlanner-zh-CN.zip` 与 `TitanPlanner-zh-CN.msi`。

## 上游与适用例外
- 直接上游为 [Titan-Dynamics/TitanPlanner](https://github.com/Titan-Dynamics/TitanPlanner) 的 `master`；保留两套中文资源及其各自归属。
- 本仓自动化由 `AUTOMATION.zh-CN.md` 描述；其发布触发行为属于仓库特定例外，文档维护不能被解释为新软件发版权限。
- 程序、依赖库、MSI 安装器版本是不同身份；真实飞控/飞行验收不由构建成功替代。
