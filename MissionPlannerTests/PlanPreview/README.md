# #29 · 本地计划预览的隔离绘制试验

基线：TitanPlanner-zh-CN 开发线 `f47f7bbb853c8b0ec2cee0def41ef0c6b9aa4a57`。
本目录仅为离线测试夹具，两个 `.cs.txt` 不编入应用。未替换 Ctrl+M，也没有改变数据缓存、协议、设备操作、键盘绑定、程序地图库或 3D 行为。不能据此关闭 #29。

## 问题与隔离边界

- 当前 `ShortcutPlanAsMission` 在 PLAN 同步读取规划表后逐项覆写共享任务字典，缩短和空计划不会清除旧尾。读写者不只 GUI，故不能以清空/换引用解决这个快捷键。
- `DataViewtoLocationwp` 对负的 WAYPOINT 延迟会归零并写回单元格；不能直接调用 `GetCommandList` 包装一个只读预览。
- FLY 的真实航点下拉使用设备缓存项目数，选择可被现有动作读取；现有 2D route 和距离条还使用实时航点/距离。3D 有独立的设备任务赋值入口，不能把本地计划伪装成当前设备航线。试验不读取这些对象。
- 可行切口：把**已经投影的 PLAN 显示几何**复制成独立点列，在地图增加专有 ID 的只读、非命中图层。与正式层保留不同颜色、虚线和常驻“Local plan · read-only”来源标记。装入前在同一 GUI 线程冻结及验证整批；失败保留此前完整预览；替换或空列表移除旧层，永不让它进入动作下拉、设备距离/进度、3D 基线或接口。

## 如何离线复验

1. 开一个隔离的 WinForms 测试会话。将两个 `.cs.txt` 复制到临时目录并去掉 `.txt`。
2. 用 C# 编译器为 .NET Framework 4.7.2 编译两个文件，引用 System.Drawing、System.Windows.Forms 和同一个可信 TitanPlanner 测试包的 GMap.NET.Core、GMap.NET.Drawing、GMap.NET.WindowsForms（必要时添加 netstandard facade）。输出目录不得在正式应用常用路径。
3. 只运行新编的夹具程序 `overlay.exe optional-output.png`。夹具使用空地图源、CacheOnly、虚构坐标及模拟设备层/缓存；不运行主应用、不连硬件/地图服务。提供文件名时额外检查避开标记区域的本地虚线和同帧设备模拟线路像素；若不提供，将少两项像素验证。

## 尚待明确的生产接线

- 建立当前规划表的只读快照源。需要保留与当前输入相同的 Home、原命令、行号、frame、参数和单位，不能反向从画出的折线重构命令；同时要验证当前规划图是否与最新有效工作表对应（编辑过程中、解析异常、批量载入、Home 不可用、空计划、FENCE 或 RALLY）。夹具只验证显示几何，不是该完整解析器。
- 只把合法计划的显示副本交给 FLY 的 UI 生命周期；验证切换或断开时移除、窗口重建/日志回放时不会重现旧副本、UI 调度不让旧回调覆盖新快照。是否要单独展示 3D 的本地几何、目前 2D 视图的自动缩放/截图会不会带入新层、如何避免视觉上和设备路径混淆，均需决定和真实离线交互测试。
- 当前已证明（离屏 18 项检查）：在独立 GMapControl 内可和模拟设备层共存，相互不改，并可定向检查缩短/清空/异常/线程约束。未验证生产 WinForms 接线、GPD、原生 Windows、设备和 3D；协议、真实任务和可执行下拉维持原代码。

## 第二阶段：只读取已提交的规划表

`ReadOnlyPlanSnapshot.cs.txt` 与 `ReadOnlyPlanSnapshotProbe.cs.txt` 是独立的 WinForms 读表试验。单独复制并去掉后缀，用 .NET Framework 4.7.2、System.Windows.Forms 和 System.Drawing 编译；不需要地图库、主程序，运行得到 25 项离线检查。不要与上一阶段同时编译（两个不同的入口点）。

- 在同一 UI 线程读真实 DataGridView 的已提交 `Value`，不提交、取消或覆盖正在编辑的值；遇到未提交的编辑立即返回错误，由操作员决定是否结束编辑。
- 由调用方一次性提供普通任务来源、计划 Home、Home 的独立 frame 和显示米制换算、当前数字文化、以及从**同一 PLAN 上下文**冻结的命令和字段换算表。可判定的 FENCE/RALLY 通过 tab 类型、命令键及 ID 排除；不接受仅靠 `UNKNOWN` 的数字标签作为普通任务来源。生产版需根据同一份维护的元数据准确分类不透明 ID；无法确认则不预览。
- 生成完整不可变快照后才返回。Home 固定为第 0 行；普通行序号按行顺序连续。分开保存初始显示数值与由当前字段比例归一化的值；不通过字符串往返降低 typed double 精度。Frame 原值保留，不做 frame 转换；XYZ 通用字段不武断套经纬度范围。Home 明确按计划 UI 输入读取，不猜测设备的 Home。
- 不调用 `GetCommandList` 或 `DataViewtoLocationwp`，不复制内部任意 `TagData` 对象。负 WAYPOINT 延迟在只读快照里保留原值（同时验证界面值和 `CellValueChanged` 不变），不是模拟设备将如何执行或修正它。非法行、坏单位或无效 Home 使整批返回错误，原完整输出不可变。空表合法，包含 Home 且无任务行；交给图层时视作清空所选普通任务的本地路线，不显示陈旧尾部。

### 生命周期与 3D 后续界限

建议以 PLAN 编辑器实例和当前数据范围的局部世代标记进行配对，不缓存静态全局快照。Ctrl+M 的编辑版本、显示投影所属版本和当前页面范围必须一致后再发布 UI 专有层；其间不等待 I/O。切换任务类型、活动设备会话/断开、日志模式切换、窗口关闭/重建等事件使旧世代失效。在 UI 线程处理的异步回调不得接受旧世代，清空只作用于本地层。如果没有任何可交接的有效投影（刚加载、表仍在编辑、转换失败或未触发重绘），报告预览不可用，不能复用旧路线标作新输入。

3D 不能把本地数据传入设备路径、当前位置航点或旧的 `Map3D.WPs` 属性，后者实际不参与现行绘制。若确实需要保留 3D，先明确独立的本地几何输入、专用显示 ID/标注、frame 和高度基准/地形语义；不能从平面折线恢复高度，也不能用实时设备高度替代。该阶段未定义或验证 3D 组件，亦未连接读表试验与原快捷键、真实地图、列表或设备。

## 第三阶段：读表、投影交接与窗口生命周期

LocalPlanPreviewSession.cs.txt 及 LocalPlanPreviewSessionProbe.cs.txt 验证独立 2D 本地显示交接；单独编译时另带 ReadOnlyPlanSnapshot.cs.txt 和 ReadOnlyPlanOverlay.cs.txt。引用及运行方法与第一阶段相同，入口改为 LocalPlanPreviewSessionProbe，无输出文件参数。真实 WinForms 网格与两个独立 GMapControl 在同一个离线 UI 线程运行；既有设备图层用固定合成几何代替，普通 PLAN 完成投影事件用测试夹具模拟。未构造生产 FlightPlanner 或 FLY。

- 会话固定绑定两个不同控件。每次已提交编辑或范围变化使当前本地层失效，并给普通任务发不可串用的世代票据。围栏和集结点没有票据；断开、窗口回收或来源丢失清除本地层，不碰其他层。新范围不得把进入时已有的旧投影对象重新贴成新版本。
- 只接受在绑定的 PLAN 地图中唯一、当前、且不属于世代起点的普通任务层。调用者在同一 UI 线程、同次正常渲染前后取只读快照；仅在两者完整内容相等后冻结新图层的路线点。不接收围栏层或设备地图上的同名层，也不把可点击标记、原画笔、原地图引用或 Locationwp 对象交给目的层。
- Ctrl+M 的未来入口只取当前已提交表的只读快照，与已冻结投影的 Home / frame / 行号 / ID / 原显示及归一化数字逐一核对；没有新鲜同源结果则显示“本地预览暂不可用”，不隐式触发重绘。业务错误或未提交的单元格不冒充新路线；Home-only 请求清空，而非沿用之前的投影。
- 已实证 19 项：异主票据、错地图、异步旧世代、进入范围前旧对象、重连/断开、前后改表、遗漏更新事件的内容不一致、短路线及空计划、非 UI 线程和两边控件/设备模拟层的隔离。它们不验证生产回调触发时机或 3D。

### 生产拆分建议（尚未接线）

1. 所有正常 PLAN 2D 普通路线（旧版和新版绘制器）都经过 FlightPlanner.writeKML 的 activeOverlay 创建及 MainMap.Overlays.Insert；建议在正常渲染路径上分别记录只读的前/后快照，再在本次普通任务的显示层完成时交接。程序自身的编辑重绘可能规范化来源，若前后不等应拒绝发布此次本地预览；不要让只读入口代调 writeKML 或辅助提取方法。时间戳、同对象或同条数均不能取代内容匹配。
2. 监听留在当前 PLAN/FLY 控件生命周期，不能订阅过期的 comPort 对象；核对 MainV2.comPort setter、comPort_MavChanged、断开、日志回放、设备重新选择、页面和实际控件销毁（离开 PLAN 但切到 FLY 不等于销毁来源）。跨线程通知传回 UI 时携带来源身份，到达后复核来源及票据，不能让迟到的断开清走新会话，也不能让旧投影复活。试验模拟不是生产事件已接好。
3. 可以先做独立 2D 显示，不改设备路径、距离条、下拉和 3D 源。现有旧快捷键原本间接影响设备 3D 任务显示；直接改成仅 2D 会改变可见行为。若批准 2D-only，标明 3D 仍仅显示设备任务；若要求 3D 一致，先另建显式本地 3D 显示模型并验 frame/高度/地形，不能向旧的 Map3D.WPs 或设备航点字典注入本地值。真实 UI 中位置任务若只有 marker 没有路线点，原型安全拒绝，不自行估算；生产可另验从明确同源的 marker 或渲染结果取点。未确认这些范围前，不切换真实 Ctrl+M，也不关闭 #29。

## 第四阶段：独立 3D 投影可行性（不接现行 3D）

### 精确源码结论

- FlightData 对 Map3D.WPs 只有赋值，Map3D 属性只有声明，不能据此说真实绘制消费该属性。实际在 Map3D.RenderScene 的第二遍绘制，MissionOverlay.DrawPlanLine/DrawMarkers/NavigationTarget 经 Waypoints 读取设备字典。NavigationTarget 还为已连接载具提供引导、自动及返航指示。不得让本地数据进入该对象、该缓存或它的指示器入口。
- 旧 MissionOverlay.Waypoints 抽取点时没有保存每行 frame，DrawAltitude 对普通点统一加地面高程；高程未加载还会暂时以 0 参与计算。这个现状不能定义新本地预览的高度。特别是绝对 AMSL 再加地面、相对 Home 当成 AGL、或缺地形时假定海平面，都不应复用。
- WorldFrame 是实际渲染的坐标边界，X/Y 为同一 UTM 坐标系下的局部米，Z 为 AMSL 米，frame 版本变动时须从冻结数据重新投影。未来本地层宜作为独立同级绘制通道，只接受当前世代的只读几何，颜色与来源标签不同，不附设备跟踪、距离、当前航点或操控事件。

### 第四阶段验证

使用 .NET Framework 4.7.2 编译 LocalPlan3DProjection.cs.txt、LocalPlan3DProjectionProbe.cs.txt、ReadOnlyPlanSnapshot.cs.txt（复制到临时目录时去掉 .txt），加本提交基线的 Controls/Map3D/Scene/WorldFrame.cs、UtmProjection.cs，引用 System.Drawing、System.Windows.Forms 和一致的 MissionPlanner.Utilities（编译 WorldFrame 未使用的 PointLatLngAlt 分支需要该类型）。运行新编夹具；可选传入一个 PNG 路径以生成合成标记示意。示意使用实际 WorldFrame 投影数值和独立 GDI 草图，**不是** OpenGL 或实际地图截图。

17 项通过：仅普通地理 WAYPOINT（键和 ID 均匹配）且明确 AMSL（GLOBAL 或历史 GLOBAL_INT）。冻结后的 PLAN 行经纬字段已用十进制度，不能再次除以 1e7；不加地形或 Home，序号保留，允许负 AMSL。Home 不加入折线；单点保留点、不画虚假路径。若任何点缺失、越界、跨 UTM 区、frame 未知、需地形或相对 Home 的参考未经核验、不是可按直线显示的普通点，整层不生成而不是连过缺点。WorldFrame 的对象归属和 rebase 版本也有检查。输出只表示局部图形位置，不是实际航迹、可执行任务或避障判断。

### 范围选择

- **建议：先把 Ctrl+M 明确定义为仅本地 2D，不混设备任务；3D 继续显示它本身的设备来源。** 这会改变旧快捷键间接让 3D 显示本地规划的行为，须先得到产品范围确认；不能称为对旧视觉的完全保持。
- 如果本地 3D 必须同版交付，可另做明确标注的独立本地 3D 层。此处只证明绝对高度的普通点子集。普通任务的相对 Home 高度应有同一计划、明确的 Home AMSL 数据来源，不能从显示模式猜测或取设备 Home。地形依赖需独立核验同一垂直基准的地形；未知帧、控制点、曲线路径及缺样本需有明确未支持状态。若要求覆盖旧有全部行为，在这些验收完成前保留 Issue，不能以复用旧数据耦合来代替。

## 第五阶段：同一计划的高度凭据与独立 3D 线层

**产品决定：本地 2D 和独立本地 3D 一起接入后才切换 Ctrl+M，不先发布 2D-only。** 上文的备选是历史论证，不代表当前安排；当前结果仍是离线原型。

- 不能从 PLAN 的 Home 输入框、frame 或某个数值反推垂直基准。实际 FlightPlanner 同一输入框可由选择地形点、已保存计划、手工输入、车辆位置/已知 Home 填充；其中现存 PlannedHomeLocation 分支与其他赋值的显示单位处理也不相同。新层不读取活跃飞控 Home、当前位置、设备任务、导航目标或导航状态。
- 显式 AMSL 帧沿用已归一化值。相对 Home（3/6）要求该冻结计划的只读 Home 凭据，坐标和已归一化高度都相同；地形帧（10/11）要求同一冻结计划、同一行且坐标精确匹配的单份线下地形凭据。各凭据有可核验的来源标识、已确认与地图场景兼容的 AMSL 基准和有限高程；不能用文档或 UI 标记代替数据出处。没有凭据、重复/不匹配/未知基准，整层不画；只接受经确认的 0，而不是把无数据默认为海平面。原有 SRTM TryGetLoadedAltitude 可能排队读取数据，本身没有这套局部计划/数据版本凭据，不可直接充当验证提供者。
- LocalPlan3DLineAdapter 走完全独立的 VertexList，交给项目原有 LineRenderer 画紫色虚线和只读点标记。该层不进入设备 MissionOverlay、WPs、导航目标、动作和选择列表。呈现要求显示自身来源说明，帧 rebase、快照身份不匹配或上下文变化时不再绘制。没有定位依据时不串接其他点，不把几何显示当作航迹预测或安全验证。

### 验证及仍欠缺

LocalPlanElevationProbe.cs.txt 的入口单独编译，附 ReadOnlyPlanSnapshot.cs.txt、LocalPlan3DProjection.cs.txt、LocalPlanElevationEvidence.cs.txt、LocalPlan3DLineAdapter.cs.txt，去掉 .txt 后与项目真源 WorldFrame.cs、UtmProjection.cs、GLResources.cs、GLProgram.cs、Renderers.cs、Shaders.cs 编译。沿用上一节依赖，另加同包的 OpenTK.dll 和 OpenTK.GLControl.dll。夹具显式禁止真实地形/影像调用。默认只跑 18 项凭据/批次检查；隔离软件 GL 会话加 --gl-smoke，多跑两项实际像素读回。Acer 使用 Xvfb 软件 Mesa llvmpipe，验证同一帧中本地紫色及独立模拟设备黄色都可见，清除本地后黄色像素保持、紫色消失。17 项上一阶段投影回归也单独运行。

下一步仍须在真实 PLAN 的输入/读取入口记录可验证的 Home 来源及单位，确定离线地形版本与垂直基准，再审实际 Map3D 更新/销毁生命周期以及 2D+3D 接线是否只有一条本地数据流。必须跑真实离线入口，覆盖高程缺失的醒目提示、单点/复杂指令限制，确认不会向设备路线、缓存、动作和 3D 指示器传入本地值。此阶段凭据仅是合成验证，结构化记录本身不是外部数据真实性证明；未更新生产快捷键或发布。

## 第六阶段：实际 PLAN 来源考证与显式 Home 基准确认

**没有现有事件能单独出具可信的 Home AMSL 凭据。** 以下是具体入口，而不是笼统说“文件不支持”：

| 当前入口 | 可用事实 / 不能推断的事项 |
|---|---|
| FlightPlanner.TXT_homealt_TextChanged / TXT_homelat_TextChanged / TXT_homelng_TextChanged | 来源可为自写、选点、文件或车辆状态，事件只观测值，不声明 datum；现有处理还有规划 Home / writeKML 副作用。新层借用事件做本地凭据失效，不能调用旧处理来提取证明。 |
| FlightPlanner.readQGC110wpfile → WaypointFile.ReadWaypointFile → processToScreen | [MAVLink WPL](https://mavlink.io/en/file_formats/index.html) 记录有每行 frame，但当前读入器可补没有 index 0 的空 Home，审批式 Reset Home 也可能被取消。当前保存器给 Home 强制 frame 0，无论文字从何而来，甚至写过全零占位。因此仅凭旧文件首行或扩展名不能证明真值。可信外部文件若明确给出独立 Home AMSL，后续应在同一次确证载入、用户已采用该 Home 的分支取得来源及单位，不能在通用 processToScreen 入口重新认可。 |
| JSON 分支 MissionFile.ReadFile / ConvertToLocationwps / processToScreen | plannedHomePosition 只给经纬高数组，自己的 MissionFile.ConvertFromLocationwpsCore 不给它附 datum，读入还转成 float。名称、groundStation 标记和外部文件模式都不是测量来源认证；Home 可能被保留。 |
| setHomeHereToolStripMenuItem_Click，updateHomeText，label4_LinkClicked | 分别可能来自地形、车辆缓存、车辆当前位置。旧数据基准或显示单位混用，且新本地层不能借设备值。现有地形读取也不可代替独立、可查版本且基准已核验的地形凭据。 |

### 已证实的明确边界

PlanHomeDatumSession 原型使用独立的显式确认事件。当前冻结计划展示实际 Home 坐标、单位和数值；必须另行选择与场景兼容的 AMSL、填可核查的来源说明，确认的坐标和经同一显示比例归一化的值须等于捕获快照。允许明确声明的零，不从空值、加载提示或 frame 推断。试验对照现行英尺系数及米制 1，拒绝未知或不匹配的比例。普通文本事件无权生成，编辑（即使后来打回同值）、单位改变、任务范围/来源更换都要失效；同数据的新对象、旧票据、异主和后台线程均不得附用。该认可是本地操作者声明，不能被当作测量、定位或文件作者真实性的认证。

PlanHomeDatumSessionProbe.cs.txt 与 PlanHomeDatumSession.cs.txt、ReadOnlyPlanSnapshot.cs.txt、LocalPlanElevationEvidence.cs.txt、LocalPlan3DProjection.cs.txt 去除 .txt 后编译，加实际 WorldFrame.cs 和 UtmProjection.cs，引用上一节相同基础 WinForms/Utilities/地图库依赖，不需 OpenGL。隔离 WinForms 看真实 TextChanged、Button.Click 和数据表，通过 21 项，额外从显式事件送入真实投影代码并检查相对 Home 高度；失效后证明同样点不再得到任何 3D 几何。旧读表 25、图层寿命 19、凭据和软件 OpenGL 20 项均重新通过。没有加入任何产品确认按钮，也没有启动正式程序。

### 最小生产接线位置（仍未接）

在 Map3D.EnsureRenderers 附近给本地批次一个独立、受当前 Map3D 实例拥有的适配器。RenderScene 第二遍复用同一视矩阵和 WorldFrame，在正式 MissionOverlay.DrawPlanLine 后、设备 NavigationTarget 及其指示器之前走独立分支，只吃经过同一 PLAN 所有者、世代及数据源守卫的只读批次。另用既有屏幕覆盖层形式输出常驻“本地规划 · 只读”说明；不要借设备的 marker、目标或动作列表，不把标签由缓存存在与否反向推断。WorldFrame 原点变动时从同一当前证据重投影。Suspend/Deactivate 期间不画且清除旧渲染批次和标签；重新显示时只接受仍当前的同一个 PLAN 上下文。DisposeRenderers 里先撤自己的批次，不接管共享 LineRenderer 或现有 GL 状态的所有权；销毁或切换会话同时撤 2D 及 3D。异步转发只能携带当前票据，晚到时丢弃。

应先在真实离线 PLAN↔FLY 验证源身份及选择确认，不假定新按钮或导入规则已批准。Ctrl+M 必须安排 2D+3D 一起切换；无合法高度数据就明确提示 3D 不能绘制，绝不能读 live wps、Home、当前目标来补。原型未将任何结果或记录写回用户规划、设备、参数或实际运行环境。
