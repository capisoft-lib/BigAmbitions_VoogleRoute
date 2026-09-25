# 我的企业：地图快捷导航

## 功能说明

打开城市地图（默认按 **M**）后，左侧导航面板默认显示 **我的企业**。列表从当前存档自动读取企业，不需要逐一添加收藏。

- 每行显示企业名称和地址，按名称排序；同名企业按地址区分。
- 点击企业名称、地址或该行的定位按钮，地图会聚焦到对应建筑。
- **设置目的地** 按钮设置导航目标；原有 **步行 / 驾驶** 按钮沿用 MOD 的导航流程。点击名称本身仅定位，不启动移动或快速旅行。
- 搜索框支持按名称或显示地址筛选；企业较多时可滚动浏览。
- **收藏** 标签保留原有收藏和车辆列表；最近车辆、住宅、商店快捷入口仍位于面板顶部。
- 无企业或搜索无结果时显示对应提示。

## 企业范围与刷新

列表包含玩家经营且已命名的企业，包括零售、办公室以及总部、仓库、工厂等配套场所。住宅、空置单位、其他人的企业不进入列表。仅拥有房产而没有经营其中的企业，不算“我的企业”；暂时停业的自有企业仍会保留。

每次打开地图或切换标签时重新读取当前存档，因此新增、改名、出售企业和切换存档都会反映在下一次刷新中。列表不依赖是否曾经访问企业，不写入收藏文件或游戏存档，也不按帧扫描所有企业。

企业列表没有删除或清空操作。要管理手动收藏，请切换到 **收藏** 标签。

新增界面文字提供英文、简体中文、繁体中文和法文；其他语言回退到英文。

## 开发与验证

此改动基于公开仓库 `main` 的 `4f7be6a`（`VERSION` 为 1.0.3）。它不是对创意工坊 1.0.5 源码的修改；上游合并时需保留尚未公开的版本改动。

实现分为三部分：

1. `PlayerBusinessBookmarkStore` 生成仅驻留内存的企业快照，按玩家经营标记筛选、按地址去重、排序，并隔离单个不可用建筑。
2. `CityMapBusinessRows` 在原有滚动区域绘制可点击的两行企业条目，复用已有地图定位和导航接口。
3. `CityMapBookmarksPanel` 增加标签切换，复用搜索框、地图可见性、拖动位置和输入处理。切换标签会退出添加收藏模式并重置搜索和滚动位置。

### 已完成

- 全部 `Scripts` C# 源文件已使用本机 Big Ambitions 游戏程序集和已安装的三个 MOD 依赖完成编译，零错误；三条 TMP 弃用警告来自已有代码。
- 10 项独立回归检查通过，覆盖未加载存档、所有权和空置过滤、配套企业、无效建筑、地址去重、同名企业排序、中文名称、改名/出售、新增和存档切换。
- 回归检查编译实际 `PlayerBusinessBookmarkStore.cs`，使用轻量游戏 API 替身；不验证 Unity 布局或实际游戏导航。

运行回归检查需要 .NET 8 SDK：

```powershell
./tests/business-shortcuts/run.ps1
```

完整游戏内安装仍按仓库 README 的 Unity Modding SDK 构建流程执行。编译验证产物未自动替换创意工坊 MOD。

### 待游戏内验收

- 打开包含多家企业的存档，核对名称、数量和总部/仓库/工厂覆盖情况。
- 测试点击名称仅定位，以及设置目的地、自动步行和驾驶确认流程。
- 测试超过 8 家企业的滚动、中文和长名称、地址搜索、无结果和空列表。
- 测试改名、出售、新建企业后重新打开地图，以及加载另一个存档。
- 测试切换收藏、添加/删除收藏、收藏选点退出、关闭地图、隐藏 MOD 界面和不同分辨率。

尚未进行游戏内视觉和交互验收，因此不能把编译通过视为完整游戏兼容性验证。

## English summary for reviewers

The city-map bookmarks panel gains a **My Businesses** tab, selected initially, that lists named businesses operated by the player in the current save. Each row shows a name and formatted address. Clicking its name/address focuses the existing map; the existing destination and walk/drive actions remain available separately.

The list is refreshed when the map becomes visible or a tab is selected. It includes headquarters, warehouses and factories, excludes residential/empty units and NPC businesses, supports search and scrolling, and never persists generated entries as bookmarks. Player operation uses `RentedByPlayer`, not real-estate ownership. Existing bookmarks and vehicles remain in the Bookmarks tab, and quick rows remain available above both tabs.

Validation: full source compilation against locally installed game/dependency assemblies and 10 fixture-based store regression checks passed. In-game UI/navigation acceptance remains pending. This patch targets public source version 1.0.3, not unpublished Workshop 1.0.5 source.
