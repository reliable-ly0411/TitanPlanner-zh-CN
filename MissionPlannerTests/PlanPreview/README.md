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

- FlightData 对 Map3D.WPs 只有赋值，Map3D 属性只有声明，不能据此说真实绘制消费该属性。实际在 Map3D.RenderFrame 的第二遍绘制，MissionOverlay.DrawPlanLine/DrawMarkers/NavigationTarget 经 Waypoints 读取设备字典。NavigationTarget 还为已连接载具提供引导、自动及返航指示。不得让本地数据进入该对象、该缓存或它的指示器入口。
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

在 Map3D.EnsureRenderers 附近给本地批次一个独立、受当前 Map3D 实例拥有的适配器。RenderFrame 第二遍复用同一视矩阵和 WorldFrame，在正式 MissionOverlay.DrawPlanLine 后、设备 NavigationTarget 及其指示器之前走独立分支，只吃经过同一 PLAN 所有者、世代及数据源守卫的只读批次。另用既有屏幕覆盖层形式输出常驻“本地规划 · 只读”说明；不要借设备的 marker、目标或动作列表，不把标签由缓存存在与否反向推断。WorldFrame 原点变动时从同一当前证据重投影。Suspend/Deactivate 期间不画且清除旧渲染批次和标签；重新显示时只接受仍当前的同一个 PLAN 上下文。DisposeRenderers 里先撤自己的批次，不接管共享 LineRenderer 或现有 GL 状态的所有权；销毁或切换会话同时撤 2D 及 3D。异步转发只能携带当前票据，晚到时丢弃。

应先在真实离线 PLAN↔FLY 验证源身份及选择确认，不假定新按钮或导入规则已批准。Ctrl+M 必须安排 2D+3D 一起切换；无合法高度数据就明确提示 3D 不能绘制，绝不能读 live wps、Home、当前目标来补。原型未将任何结果或记录写回用户规划、设备、参数或实际运行环境。

## 第七阶段：同源、同世代的 2D+3D 成对提交

**接线选择已收敛：2D 使用纯绘制通道，不将本地路线或标记放进 FLY 的 Overlays。** 原生 GMap 的 GetRectOfAllMarkers 会将可见、不可点击的标记也纳入边界；只把 IsHitTestVisible 设为 false 不足以保证原有自动缩放、边框和其他查询语义不变。正式候选宜在 FLY 的 myGMAP 已有正常绘制之后消费独立绘制回调。基线 GMapControl.OnPaint 的 GDI 与 Skia 分支均完成底图后调用 base.OnPaint；FromLatLngToLocal 负责从坐标换到控件像素。原型使用这条 Paint 路径和独立紫色虚线，标明只读来源，不改变任何地图集合、设备航点显示或操作入口。后续仍应在真实离线视图核对旋转/分数级缩放、样式和高 DPI。

- 封闭的成对接口需要同一 PLAN 视图拥有者、世代票据、冻结快照及 3D 场景实例。确认普通 PLAN 的已有 2D 投影后只复制显示点列；3D 则独立预备有效、可证明高度的线条批次。2D 和 3D 都准备完成前，候选不碰绘制、地图集合和现有有效成对画面。
- 成功提交时只更换一个成对可见状态，随后安排两个控件重绘。两条绘制路径从同一状态读取；这不是两个窗口系统帧物理同步的承诺。任何一边不能通过当前合法性检查都不将另一边单独发布。优化在 UI/渲染同一拥有者内运行，3D 如未来移到异线程须再设计只读交接。
- 有效的新候选在准备阶段不先清旧画面；当前世代的输入或有效高度失效、被拒的新投影、无计划、切换/断开时，整对失效。较新的投影或暂存取代较早的暂存票据；晚到、异主或已退休的票据被拒绝，不能复活旧路线、清除新发布画面或改变任何设备图层。WorldFrame 重定位后，清除两边可见状态直至两边都以当前输入重新准备。若生产场景控件销毁/替换，应先撤销当前计划世代，不能用另一个坐标基准接管。
- 第六阶段的显式 Home 声明只记录“哪份本地计划的操作者明确认领了哪个数、单位、基准及来源”；不代表文件、实测或基准已独立核验。本阶段夹具使用合成的已核验高度凭据；正式层不得自行升级操作者声明为外部验证，或拿运行中的设备 Home/地形猜测数值。缺可信高度时两层一起不显示，并由入口告知原因。无生产 Ctrl+M、缓存、指示器、飞控参数、更改真实行为或发布。

### 复验范围

PairedPlanPreviewProbe.cs.txt（独立入口）另带 PairedPlanPreview、LocalPlanPreviewSession、ReadOnlyPlanOverlay、LocalPlan3DLineAdapter、LocalPlan3DProjection、LocalPlanElevationEvidence、ReadOnlyPlanSnapshot、OfflineDatumPair，及第五阶段的实际 GMap.NET、OpenTK、WorldFrame 与 LineRenderer/着色器依赖。一律复制去掉 .txt 后单独编译。空地图源、软件 GL 和合成路线，不加载真实地形、运行正式程序或连接设备。

Acer 隔离实际 GMap Paint + Mesa llvmpipe OpenGL 测 24 项；像素证明成对初次发布、替换、过期回调、失败清理、只读 2D 不在边界 API、已发布的场景重定位同时取消两边绘制、以及模拟设备的黄色像素完全不变。此前单独的 2D 会话 19 项也重新验证。尚需上线前的真实离线 PLAN→FLY 事件与景观验证、运行时高度来源／垂直基准和发布准入，以及整合后 UI 错误提示；不得将夹具当作产品已完成。

### Phase 8: narrow host, scene, and actual first-swap seams (offline only)

* **PLAN source:** observe a detached snapshot at the start of the normal UI-thread `FlightPlanner.writeKML()`, after its `quickadd` / disposal guards and before the legacy `GetCommandList()` call; sample again only once the Mission branch has inserted and refreshed that invocation's actual `WPOverlay` (`WPOverlay2` is default; both branches yield a `GMapOverlay`). The normal call remains unchanged. Do not invoke `writeKML` just to service preview and do not infer provenance from whatever overlay happens to remain by name. Invalidate the capture on PLAN row edit/add/remove/reorder, bulk load/clear/replacement, mission type, Home/frame/unit changes, and direct grid edits even if redraw has not happened. A fresh capture after bulk work is possible only for the final complete redraw. The product event audit/guard for all mutation entry points is still required. Compare immutable contents at publication too, rather than relying on events exclusively. Switching from PLAN to FLY is normal; merely deactivating PLAN is not unloading its content.
* **FLY destination:** `FlightData.gMapControl1` is a destination only, not a source. Keep the separate paint-only layer, so no published plan may feed map bounds, route selection, mission indexes, or device overlays. The private `FlightData.map3DControl` is the host instance. Attach a host-scoped read-only paint binding after `Create3DMap` installs that instance; retire on `Hide3DMap` before `Suspend`, `Dispose3DMap` before detach, or WinForms handle loss/disposal. `Resume` requires a fresh first-frame receipt even when reusing the GL context. Never use mutable `Map3D.instance` or `WPs` as the binding identity. Main form closes via `OnFormClosing` calling ForceHide/Dispose before base close; queued delegates must become no-ops, with any cancelled-close recovery waiting for a valid new host/epoch rather than reviving old callbacks.
* **Real 3D seam:** the active draw method is `Map3D.RenderFrame` (not `RenderScene`). The frame is not ready merely because the GL handle exists, it is active, or `SwapBuffers` returns: `RenderFrame` can return early for a blank centre and `doPaint` still draws the settings button and swaps. Prepare a candidate readiness stamp only after a successful nonblank render, `_frame.Update` and view/projection setup; report it only once that frame swaps successfully. Use identity for the exact Map3D/control/context epoch and a stamp regenerated whenever the scene origin/frame version changes. Render the local layer through its own line pass just after `_mission.DrawPlanLine`, before markers/indicators, with the already established matrices and no changes to camera, terrain, vehicle waypoint sources, or navigation state. Release any preview-only GL resources while the owning context is still current. No vendor changes are needed for this seam; the app-owned `Map3D.cs` can expose a private integration callback rather than abusing GLControl's generic Paint event (before the frame). An excluded/failed/blank frame after a previously ready one must explicitly retire the receipt; sampling only good swaps is insufficient for lost readiness.
* **Prototype result:** `PreviewDispatch` uses a stable UI host's message queue and coalesces to one pending callback in each epoch, with rechecks at dispatch time. Actual WinForms and OpenTK GLControl prove that native handle existence alone is insufficient, correct-owner valid swaps post once (never execute inline inside paint), and pending callbacks after content retirement, hide, HWND recreation, rebase, or form close cannot publish. This fixture supplies synthetic scene stamps and reports its own actual software-GL swap; it does not run the network-/device-connected Map3D host or prove production scene readiness. Use `PairedPlanPreview`'s single payload pointer as the logical switch, then invalidate both views only after successful revalidation. Their OS paint and GPU presents are independent and cannot be called an atomic physical raster change. When an edit/rebase/host failure retires an already visible pair, the production coordinator must retire that pair as well as its pending dispatch; this dispatcher only guards new publication. No deployment or Ctrl+M wiring here. Retire stale proof on owner/device-scope transition; never derive local altitude from a device, a blank frame, or rendered terrain fallback.

### Phase 9: PLAN mutation coverage and final detached recheck

Source locations refer to the phase-8 draft (the phase-9 files do not change production):

| Boundary | Actual source and implications |
|---|---|
| User grid editing and row actions | `GCSViews/FlightPlanner.Designer.cs:1427–1433` wires CellEndEdit, RowsAdded and RowsRemoved, not CellValueChanged. `FlightPlanner.cs:2606–2684` handles delete/reorder and edit completion; `:2700–2838` initializes Frame and handles row validation. Treat a still-dirty edit as unavailable; never commit/cancel it as a side effect of preview. |
| Programmatic values, metadata | `FlightPlanner.cs:2845–2899` updates command defaults and directly writes `Command.Tag`; `:3717–3755` sets a row/location and parameters; `:5261–5296` performs direct Alt assignments; `:5884–5985` bulk loader assigns command tag/frame. A Tag write is not a CellValueChanged signal, and ordinary programmatic cell assignments are not user CellEndEdit. Validate semantic command ID, exact row order, raw/numerical and normalized fields plus frame independently of notifications. Unclassified opaque commands remain rejected, not just coerced from a tag. Ignore display-only derived Grad/Angle/Dist/AZ (`:6995–7005`) in content equality. |
| Bulk/quickadd | `FlightPlanner.cs:1463–1473`: normal redraw exits while quickadd. Clear `:2419–2436`, route generation `:3265–3311` and `:3371–3399`, load/append `:5884–6066` (clears, can early exit, temporarily inserts/removes Home), reverse `:6230–6248`: retire pending work at start; only the completed final redraw can stamp matching geometry. Do not resurrect an identical-valued reload using final content equality alone. On error/early return no complete redraw is not evidence of freshness. |
| Frame, Home, kind, units | `FlightPlanner.cs:2512–2561` handles frame/kind transitions; Home text is written from terrain/map (`:837–840, 7021–7025`), from imported Home (`:6043–6047`), and from vehicle (`:4627–4629, 7599–7622`), not just the keyboard. Unit normalization is a global multiplier (`MainV2.cs:4627–4638`), so not all unit changes come via grid events. Capture current units and compare; keep Home height evidence a different, revision-scoped decision. |

Final publication requires a fresh, side-effect-free capture on the owner UI dispatch: compare against the entire staged immutable content while keeping the original snapshot identity bound to the height evidence. Check owner/revision/stage first (a retired actor cannot retire newer state). For an already-visible pair, revalidate before retaining it at the same explicit PLAN/redraw/bulk boundaries; mismatch or unavailable capture retires the generation and both views, not just the pending stage. No capture on every GL paint and no polling or call to the legacy mutable reader. This closes the data mismatch at an observed boundary, not changes departing and returning to identical values before inspection, not unobserved intermediate paint, and not an unreported identical reload/owner/datum change. The integration must still synchronously retire before bulk/owner/kind/datum transitions and before any user-visible stale frame is accepted. Geometry-only style changes must continue through the exact before/after projection stamp; matching rows do not attest to those inputs.

Actual offscreen GMap/WinForms and GL fixture (28/28) additionally verifies equivalent fresh captures can commit with evidence identity unchanged; an unsubscribed direct Alt write clears both visible layers without touching the simulated-device pixels; restoring the old value cannot resurrect a retired pair; a bulk clear between stage and queued commit cannot publish either layer. Existing 2D/3D and stale-generation pixel cases still pass.

No real AMSL provenance was established by this audit. Verified absolute altitude requires a datum explicitly carried by a supported and trusted plan source and compatibility with the scene vertical reference; an operator attestation is evidence of a declaration, not independent verification. Relative requires an exact same-plan, same-generation datum for Home; AGL requires a trusted datum for every used point. Label any separately approved operator-supplied workflow as attested, not verified. Until product defines and validates the accepted source/vertical-reference contract, reject unsupported or ambiguous inputs for both projections together and leave Ctrl+M unchanged in this draft.

### Phase 10: detached offline Map3D feasibility decision

**The current `Map3D` control has no safe, instance-local offline mode.** Do not instantiate it as an offline preview, wrap it in a different Form, set a process-wide cache mode, or pass an empty vehicle list and call the result isolated. The phase-8 independent local draw seam inside the existing FLY host is still technically available, but it does not detach or make that host offline.

| Dependency | Why it defeats an offline, detached host |
|---|---|
| Constructor and teardown | `Controls/Map3D/Map3D.cs:98–124,152–170` sets the global `instance`, loads global settings and custom STL, and can enqueue deletion of the old shared 3D tile cache. `:672–697` stops the engine and asynchronously releases static `ElevationSampler` cells. A second instance could interfere with the live one. |
| Camera, routes and other state | `Map3D.cs:190–198,350–379,427,443–468,544–562,787` resolves vehicle connectivity, aircraft state, ADS-B/HUD and the FLY 2D map even without an injected feed. `Overlays/MissionOverlay.cs:47–84,146–176,205,249–251` reads the live mission, Home and operational navigation directly. `Map3D.Input.cs:176` calls the ordinary terrain intersection. A caller cannot supply a plan-only camera/route contract to this control. |
| Imagery | `Map3D.cs:323–347` creates Google satellite imagery, with no constructor parameter or override. `Terrain/GMapImagerySource.cs:20–33` can initialize the provider before tile retrieval. `ExtLibs/GMap.NET.Core/GMap.NET.MapProviders/Google/GoogleMapProvider.cs:92–115` can start an HTTP request during initialization independently of tile retrieval. Global cache-only is not an isolation boundary. The existing empty map provider and fixture imagery are not injectable into Map3D. |
| Elevation | `Terrain/TerrainEngine.cs:45–50` only accepts imagery, then builds a `TileLoader` using the default static elevation sampler. The loader itself has an injectable delegate (`Terrain/TileLoader.cs:50–65`) used by synthetic tests, but the actual host does not expose it. Separately, `Map3D.cs:532–537` calls SRTM for camera ground; despite its name, `ExtLibs/Utilities/srtm.cs:522–574` can post `GetCell` work on miss (`:402–448` enters shared disk/queue handling). Missing heights may be filled from ancestor/reference heights (`TileLoader.cs:197–223`) or from neighboring samples/zero when filling DEM voids (`ElevationSampler.cs:118–140`). These are display fallbacks, not height evidence. |

**Decision:** To preserve an exact terrain-backed view, wait for an explicit app-owned offline camera/terrain context and a supported, licensed, local offline DEM with verified vertical datum and valid per-point coverage; keep #29 draft until that scope exists. If a distinct *“本地规划 · 只读 · 无地形”* schematic is acceptable, recommend a separate inert GL control reusing only the already-proven `WorldFrame` / line renderer, with its camera and both panes fed by the same immutable plan and one paired publication. That is not a detached instance of Map3D and does not preserve satellite imagery, live overlays or terrain-clearance semantics. No basemap or DEM is required for this alternative, and no arbitrary height plane should be shown as ground. Label normalized plot axes and any user-attested datum; do not imply vehicle state or terrain clearance.

An isolated reference-plane plot can show **altitude relative to the declared same PLAN Home** for plans whose plotted rows all use that consistent relative frame, if this explicitly non-georeferenced vertical interpretation is approved. That does not turn Home into AMSL and says nothing about actual ground. An absolute plot requires trustworthy absolute datum metadata; a coherent AMSL/relative/AGL mix further requires exact same-plan Home and pointwise compatible terrain evidence. Unknown frames, unproven datum mixes, or missing AGL terrain must fail closed for both panes. Do not make up ground, infer datum from imagery/file suffix/frame alone, or auto-upgrade an operator declaration to verified evidence.

No new GL prototype was run: the inert renderer and paired atomic logical handoff already have isolated pixel proof in earlier phases. A new alternate host without a product choice would repeat those tests without resolving terrain/altitude semantics. No production or Ctrl+M change.

### Phase 11: independent preview context, no source admission yet

Keep the accepted end state: switch **2D and 3D together**, after absolute, Home-relative and terrain-relative references validate. No reduced no-terrain release. Do not reuse legacy Map3D as the standalone host. Build an app-owned **local preview context** with only these dependencies: immutable PLAN + owner/content ticket; copied 2D geometry; an admitted, read-only local elevation resolver; a preview-owned coordinate frame/camera and renderer; and the existing paired dispatcher/publication boundary. No link to FlightData, live device state/cache, network, cloud, shared GMaps mode, global settings, or provider initialization. Treat an external-data revision independently from the PLAN and GL-handle revisions.

| PLAN vertical reference | Exact resolver path |
|---|---|
| Absolute, frames 0/5 | Accept only when an approved schema or reviewed local import contract explicitly identifies the vertical reference of the PLAN source and its unit. Normalize once, convert to the fixed scene vertical CRS using an approved, offline transform if required, then tag the resulting finite ordinate with source/version and exact row. A frame ID or “AMSL” without a named reference is insufficient; legacy JSON/Home text is not an approved schema. The current phase-4/5 builder's absolute branch accepts the fixture frame alone; it must **not** become a product rule. |
| Home-relative, frames 3/6 | Require normalized offsets and the same immutable PLAN's exact Home coordinate and same-generation ground-independent Home height from the approved local PLAN/import provenance. Resolve that Home to the scene vertical CRS once, then add each signed metre offset. The approval must cover this exact Home, not a nearby point or last-used Home. Do not substitute a terrain sample, device Home, edited display text, or operator attestation marked as verified. |
| Terrain-relative, frames 10/11 | Require a finite signed offset and an admitted local DEM sample at each exact normalized geodetic row position, in the scene vertical CRS or with an admitted offline transform. Match receipt to exact snapshot/row/coordinate and source revision. If a displayed terrain layer or segment depends on continuous coverage, require covered, verified sampled mesh/segments as well; waypoint samples alone must not be represented as terrain or as clearance evidence. No synthetic fills or interpolation across voids/boundaries. |

**Local terrain admission contract:** expose no URL or provider; a read-only, immutable local dataset handle is admitted separately from the renderer. Require dataset/version, integrity digest and provenance, license/use terms, horizontal/vertical CRS and units, extent/grid spacing and interpolation/no-data convention; a datum-transform manifest includes identity/version and the integrity of local grid files when needed. Return either a successful sourced sample (finite metre value, exact query, dataset and transform identity/version, validity/quality flag) or a typed unavailable result (not admitted, outside coverage, no-data, corrupt, conflicting/ambiguous datum, transform unavailable). Legitimate zero is success, never the missing sentinel. Interpolate only on the documented grid with all required valid neighbors. Freeze handle plus revision and require one consistent vertical reference; switching datasets, manifests, resolver policy or Home evidence retires both panes before any reuse. Approved metadata/receipt creation must be internal to source admission, not a caller-supplied “verified” boolean or arbitrary receipt string. Test receipts cannot be admitted as real data.

**Smallest offline increment before admission:** a pure injected resolver and scene/pair builder in isolated tests, with an inert manifest type and a fixture-only in-memory grid. Prove exact normalized values, explicit absolute receipt (including wrong datum rejection), matched Home-relative, exact pointwise terrain including verified zero, negative/feet values and conversion, dataset/transform revision invalidation, and no-data/ambiguous/out-of-extent/mixed-frame missing-proof failures. Require an all-or-nothing detached result with both immutable payloads; erased/retired proof must defeat stale callbacks. Zero capability to open a device, URL, cloud service, cache or settings; no new GL prototype needed. A product candidate additionally needs real source admission and both hosted views verified against one plan and one evidence revision. Unsupported command/frame/coordinates/receipts fail closed for both panes with a reason; never quietly downgrade to 2D or hide uncovered geometry within a “verified” 3D route.

No product/source selection needed now. Ask the user only if multiple materially different eligible local dataset/import choices remain (coverage, storage, rights, vertical reference) or their local files/license are necessary. If no suitable source exists, report that blocker; do not reinterpret the acceptance criteria or auto-select an online provider. This phase is design only.

### Phase 12: isolated synthetic data contract check

`OfflineDatumPair.cs.txt` and its probe exercise the pure CPU decision path only. Admission accepts an explicitly test-generated, copied in-memory grid in one **fictional** horizontal and vertical reference, with units and a version. There is no file, URI, network, cloud, GL or device capability in the fixture path. Absolute and Home receipts bind to the exact snapshot identity, row (Home = 0), coordinates, units and fictional reference. Terrain is read at the exact plan coordinate from the current admitted grid; zero succeeds, but missing/non-finite weighted nodes and outside coverage fail. Only one immutable payload containing both vertex lists can be published; conflicting/missing proofs retire the active epoch, and switch/dispose prevents a stale callback from disturbing a newer result.

Isolated Roslyn/Mono console probe: **23/23**, including one mixed-reference success (negative feet absolute, signed Home offset, exact zero terrain), manifest gates, original-grid mutation, version switch, mismatched snapshot/row/coordinate/datum, unsupported units/frame, duplicate receipts, missing/no-data/out-of-extent grid, and dispose. This checks paired vertices, not a real map, terrain mesh or arbitrary datum transform. No real source has been admitted. Production would still require actual source authentication, dataset/transform policy and integrity, canonical numeric normalization consistent across capture/receipt paths (no per-caller rounding or silent tolerances), independent view hosts and the existing full paired acceptance. No Ctrl+M or service changes.

### Phase 13: repo import audit and first implementable typed source

| Existing path | Evidence it preserves | Why it cannot admit a live preview source |
|---|---|---|
| PLAN JSON `.mission` | `FlightPlanner.cs:2209–2220,6474–6495` and `ExtLibs/Utilities/MissionFile.cs:106–140,201–220,252–283` preserve per-item frame on the current export and store Home in a numeric `plannedHomePosition` list. | Neither path binds a named vertical reference, provenance or authorized source to Home or to frame-0/5 absolute elevations. Frame describes the intended altitude interpretation, not evidence that the original number belongs to EGM96. Imported/copied data remain display-only and do not obtain a verified receipt. |
| QGC WPL 110 / legacy text | `FlightPlanner.cs:1062–1078,6500–6544`, `MissionFile.cs:18–85` parse coordinates and per-row frame; the writer emits a synthetic first Home row using frame 0. | Neither format records a named vertical CRS or provenance. A generated, blank or imported Home is not elevated to trusted by its frame, column number or file origin alone. |
| App GeoTIFF raster | `ExtLibs/Utilities/GeoTiff.cs:117–142,497–527,530–592`: static directory scan of the shared SRTM store; parses horizontal georeferencing, selects raster from the index, returns altitude and a generic “GeoTiff” label. `ExtLibs/GDAL/GDAL.cs:82–122,257–341` is a scanning/rendering imagery utility. | Neither existing response carries enough per-sample named vertical CRS, units, explicit no-data policy, source version/licensing or integrity. Do not wrap the global lookup. The [OGC GeoTIFF 1.1 standard](https://docs.ogc.org/is/19-008r4/19-008r4.html) itself defines vertical CRS and units keys (4096 and 4099); future support can read these, but they cannot by themselves authenticate a raster. |
| DTED / HGT terrain | `ExtLibs/Utilities/DTED.cs:180–217,214–283` searches a directory tree and returns a generic altitude response, not a dataset receipt. `srtm.cs:91,402–448,522–574,841–843` generates HGT names and couples shared cache/load/remote queues. | Neither cache path provides a reviewed dataset/version/digest chain or stable local-only capability. A matching extension, tile name, sample count or apparent “offline” cache status is not proof of a datum, source, rights or current valid coverage. |

**Narrow initial adapter scope:** one static, app-owned source specification for official **USGS EROS SRTM 1 Arc-Second Global**, [DOI 10.5066/F7PR7TFT and product specifications](https://doi.org/10.5066/F7PR7TFT): geographic WGS84, EGM96 orthometric heights in metres, one-degree tiles, one arc-second sample spacing. Treat EGM96 as explicitly different from WGS84 ellipsoidal height. [USGS EROS use and citation](https://www.usgs.gov/centers/eros/science/usgs-eros-archive-data-use-and-citation) describes unrestricted use unless marked otherwise and requests attribution. That is policy for official receipts, not a license finding about a local or third-party copy. No tiles, mesh, transformation grids or signed manifests have been acquired or verified; exact access, per-area coverage, version and tile format/digests remain source admission checks.

This metadata is sufficient to develop the **typed contract only**: accept a bounded in-memory descriptor plus admitted raster object with exact horizontal/vertical reference, metre unit, still-valid approved identity/revision/digest and explicit extent/post/no-data conventions. Start with WGS84 input + EGM96 scene and strictly identical supported reference, so there is no transform or geoid grid dependency in the first adapter. A detached source-specific decoder, if added after verifying encoding metadata, should take bounded immutable bytes and a trusted manifest, not a filename/URL/cache pointer; parse only the exact documented grid variant and reject mismatched shape, unknown no-data, mismatched identity/hash, ambiguity between neighboring tiles, or coverage gaps. For early synthetic tests instantiate an entirely separate fixture manifest that cannot enter the real-source admission path. Real admission still needs an explicit trusted manifest for a legally available local dataset, with measured/declared coverage restrictions, and separately trusted PLAN absolute/Home evidence (e.g., a reviewed dataset-bound sidecar/schema for the exact immutable PLAN). The SRTM label does not qualify original PLAN heights and no legacy import auto-qualifies. Bind scene coordinates and data revision to the same immutable pair; no device/cache/network fallback, and no “clearance” wording.

**Independent parallel development:** prepare a disabled, unregistered preview host contract with an offscreen 2D/3D presentation owner and a single paired payload sink. It can be developed without source data and without another GL rendering experiment: inject only fixture scene/payloads, prove typed rejection before show, both views share the same revision, scene/handle recreation and Dispose release the preview-owned resources, and require a real, enabled admission service before a production host could be registered. No user-visible command, menu, setting, Ctrl+M change or wiring to FLY is part of this increment. Keep exact paired 2D+3D definition and all three reference checks.

**Suggested draft PR summary:** “Separate the local PLAN preview from device mission state. This draft defines paired 2D/3D activation and typed offline datum/source admission, with synthetic checks only; current PLAN/DEM importers do not establish real height provenance. Ctrl+M and the existing Map3D/vehicle paths are unchanged. Leave draft until approved local data, synchronized lifecycle and all three altitude-reference acceptance cases are verified.”

### Phase 14: unregistered app-owned paired host

`PairedPreviewHost` starts disabled, has no application registration or production enablement, and consumes only the phase-12 fixture resolver and its immutable pair. Two distinct WinForms leaves own presentation readiness; each receipt matches the monotonic host revision, exact control and specific staged payload. A single pointer publishes both. An active revision cannot re-stage a replacement; a fresh explicit revision is required. Hide, zero-sized pane, native HWND destruction, disposal and active errors retire both; a source switch/disposal invalidates retained and staged data. Host disposal relinquishes the fixture resolver. This proves a shared logical state, not simultaneous raster visibility; no renderer was linked.

Isolated WinForms/Xvfb probe: **20/20**, covering disabled default, explicit fixture-only enable, stage invisibility, wrong/missing panes, replaced stage, shared publication, source switch, late and current errors, actual HWND recreation, transient zero size, hiding, immutable PLAN mismatch, form close and final disposal. No GL, files/data providers, network, hardware, legacy Map3D or Ctrl+M. Remaining integration: wire *only* independent preview-owned controls to the existing first-frame/coalescing seam and repaint both after publication, and provide true local PLAN/DEM datum admission before registering any production host. The real host must mint readiness after its own actual frame; WinForms readiness in this fixture is not proof of real rendering readiness.

### Phase 15: composed offline presentation boundary

A single integration probe composes the existing phase-8 dispatcher, phase-12 fixture resolver and phase-14 host without altering their implementations. Two distinct actual WinForms panes display fixture-only harmless paint marks, and share no publish callback inside Paint. Under Xvfb **16/16** assertions passed: opening stays disabled until explicit fixture-only enablement; rapidly revising the same PLAN owner prevents its earlier first-frame ticket from publishing; the later frame queues instead of publishing inline; both independently painted panes draw only the latest immutable pair bearing the non-navigation label; hiding or closing before the queued callback suppresses both. This tests control painting and queue timing, not a production GL swap, actual route rendering, nor simultaneous physical frames. Phase 8's isolated GL test remains the render-side evidence; no GL or legacy dynamic services here.

Stop repeating fixtures once this boundary is covered. Remaining work is introducing independent app-owned rendering hosts and a real, locally admitted PLAN and elevation-source chain under the established three-reference and paired-release acceptance. No production shortcuts, registration or data access were added.

## 清空后不再接收旧计划的投影

在 `8fa7b267` 的成对预览中，Home-only 会撤下两个图层，但原先的计划版本仍可接收投影。如果清空前的五点路线稍后再完成投影，它能重新准备并提交。现在清空会一起撤销旧版本和已缓存的投影。重新载入相同路线时，从新的普通计划版本开始。

沿用第七阶段的 `PairedPlanPreviewProbe` 和编译依赖。新增 19 个断言，先取两点基线，再加三个位置彼此分开的尾点。五点换回两点时，在地图与 OpenGL 场景分别比较整份本地路线的像素位置，再交替替换八次。清空期间重发旧投影与旧提交，同时核对模拟设备路线和像素。可从同一入口复验断开、场景重定位及缺少计划 Home 基准。

本次云端局部结果如下。

- 无窗口对照保留候选的计划版本、投影和成对提交代码，用替身代替窗口和地图集合。旧版在清空后仍接受旧投影，修正后该投影和提交均被拒绝，16 项生命周期断言通过。
- `OfflineDatumPairProbe` 的 23 项纯 CPU 断言通过。绝对高、相对计划 Home 和相对地形高的输入仍使用合成凭据。
- 完整成对夹具已按 .NET Framework 4.7.2 引用程序集局部编译。本次环境不允许创建 Xvfb 所需的 Unix socket，新增的 19 个像素与状态断言尚待运行。原有 28 项记录对应此前版本。Windows 的两个实际视图、真实离线高程来源及垂直基准仍待共同验收，Ctrl+M 继续沿用现行入口。

## 成对绘制采用已核验的三种高度

在 `ae92cb59` 的成对入口，一个只有绝对高度 frame 编号的点仍能在没有高程凭据时进入待显示状态并提交。现在绘制入口只接收 `OfflineDatumPair` 为同一冻结计划发布的高程结果。绝对高逐行对照凭据，相对高使用这份计划的 Home，地形高使用该版本网格在点位的有效样本。三维只做水平投影，高程不会再加一次。在准备后或显示中重新核验数据，旧结果在提交和下次绘制时都会失效。两个视图的来源标签标明「本地计划 · 只读 · 合成高程」。

- 运行第七阶段的成对夹具时，编译输入还需加上 `OfflineDatumPair.cs.txt`。新样本用 -12 米绝对高、120 米 Home 上的 -5 米偏移，以及有效零地形上的 7 米偏移，断言投影结果为 -12、115、7 米。逐一去掉凭据时，两边均应为空；同计划重新核验后，旧提交也应失效。
- 云端无窗口对照共 25 项通过，包含上一批的 16 项生命周期检查。原 CPU 高程夹具 23 项、独立投影 17 项、线段与高度凭据 18 项均通过。后两组运行早期独立入口；成对入口使用同计划的新版高度结果。
- 含 GMap 和 OpenGL 的完整夹具已定向编译。本批的真实像素与原窗口首帧留待可运行的显示环境。所有高度仍来自纯内存的虚构基准。实际本地计划与地形的来源、垂直基准和覆盖仍待确认。
