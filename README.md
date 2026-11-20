# PCL CEPCLML 修改版

> 本项目是**第三方基于 Plain Craft Launcher 2 (PCL) 独立进行的二次创作产物，与 PCL 官方无关**，也不代表原作者立场。
> 内部标识 / 程序文件名仍为 `CEPCLML`（数据目录 `CEPCLML\`、配置 `CEPCLML.ini`）。

- 上游项目：Plain Craft Launcher 2（PCL），作者 **龙腾猫跃（MelonYue）**
- 上游仓库：https://github.com/Meloong-Git/PCL
- 赞助原作者（爱发电）：https://afdian.com/a/LTCat
- 本项目作者：SH-HE-023（哔哩哔哩：彩CAN）
- 许可：见根目录 [`LICENCE`](LICENCE)（《PCL 分发有限许可》+《PCL 存储库合理使用指南》）

## 命名与许可（已按《PCL 存储库合理使用指南》处理）

| 要求 | 本仓库的做法 |
| --- | --- |
| 明确声明是第三方二次创作 | 本 README、程序集描述、启动页公告、声明页均已写明 |
| 名称以 `Plain Craft Launcher (PCL)` 开头 + 第三方修改后缀 | 显示名 `PCL CEPCLML 修改版`（窗口/启动页/关于页/程序集标题） |
| 关于页首位给出原作者署名与赞助链接 | 「设置 → 系统」与「更多 → 帮助 → 声明」两处首位均已放置 |
| 保留 Minecraft 正版购买劝导 | `Modules\Launch\ModLaunch.vb` 的 `McLaunchPrecheck` 原样保留 |
| 继续以 `LICENCE` 作为使用指南 | 根目录保留 LICENCE，未改名未删改 |
| 无与 PCL 赞助解锁类似的功能或表现 | 上游的解锁逻辑本身是空实现；本版进一步删除了「赞助 ¥23.33 解锁」文案与相关提示，界面主色保持蓝色 |

## 构建环境

| 项 | 要求 |
| --- | --- |
| 系统 | Windows 10 / 11 |
| 运行时 | .NET Framework 4.8 |
| 工具链 | Visual Studio 2019/2022（含 .NET 桌面开发 + MSBuild），或单独安装 MSBuild / Build Tools |

解决方案：`Plain Craft Launcher 2.sln`，包含四个工程：

- `Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj` —— 启动器主工程（VB / WPF，输出 `CEPCLML.exe`）
- `PCLCS\PCLCS.csproj` —— 核心库
- `MeloongCore\Shared\MeloongCore.csproj`、`MeloongCore\Wpf\MeloongCore.Wpf.csproj` —— 上游依赖库

## 编译

```bat
msbuild "Plain Craft Launcher 2.sln" /p:Configuration=Release /p:Platform="Any CPU" /m /verbosity:minimal
```

产物在 `Plain Craft Launcher 2\bin\`：

```
CEPCLML.exe
CEPCLML.exe.config
CEPCLML.xml
PCLCS.dll
MeloongCore.dll
MeloongCore.Wpf.dll
Microsoft.Win32.Registry.dll
```

只把 `bin\` 下的**文件**打包即可；`bin\PCL`、`bin\.minecraft` 一类的运行期目录不要打进去。

## 目录结构

```
Plain Craft Launcher 2.sln
LICENCE                       # 上游许可，必须随源码一起分发
.github/workflows/build.yml   # GitHub Actions 自动编译
Plain Craft Launcher 2/
  FormMain.xaml(.vb)          # 主窗口：顶栏、左侧栏、页面切换与动画
  Pages/                      # 各功能页面（启动 / 下载 / 联机 / 设置 / 更多 / 版本 …）
  Controls/                   # 自绘控件（MyCard、MyListItem、MyScrollViewer …）
  Modules/                    # 启动、下载、Mod 管理、Minecraft 相关逻辑
  Resources/                  # 主题、样式、语言、图片资源
PCLCS/
MeloongCore/
```

## 相对上游的改动

1. **显示名改为 `PCL CEPCLML 修改版`**，并在启动页公告、设置 → 系统、更多 → 声明里标注「第三方基于 PCL 二次修改」。
2. **关于页首位加入龙腾猫跃署名与赞助链接**（https://afdian.com/a/LTCat）。
3. **移除赞助解锁相关表现**：删掉「累积赞助达到 ¥23.33 解锁」等提示文案（上游解锁逻辑本身已是空实现）。
4. **运行时命名去 PCL 化**：数据目录 `PCL\` → `CEPCLML\`，配置 `Setup.ini` → `CEPCLML.ini`，`%AppData%\CEPCLML`，程序集名 `CEPCLML`，并修掉运行时动态 XAML 里残留的旧程序集名（否则报「无法创建未知类型 `MyCard`」）。
5. 启动页账号卡片上的 3D 皮肤模型已移除。
6. 联机页：EasyTier / Campfire 入口与说明，附一行免责声明。
7. 下载、设置页的分类栏位于顶栏下方横条；主内容区高度扣除该横条，避免页面底部被裁。
8. 清理了无引用控件（`MySkinModel`、`MySliderDot`）。

## 免责

本程序免费、开源，与微软（Microsoft）、Mojang 及 PCL 原作者无任何隶属或背书关系。Minecraft 游戏本体、皮肤、Mod、整合包等资源版权归各自权利人所有，本程序不提供、不分发盗版游戏内容。

有细节疑问可按指南到上游讨论区提问：https://github.com/Meloong-Git/PCL/discussions
