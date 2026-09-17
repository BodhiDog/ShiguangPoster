# 屏幕海报 (Screen Poster)

仿照早期 WPS 画报风格的屏幕保护程序。使用 **C++ / Qt (Widgets)** 编写，可跨平台（Windows / macOS / Linux）。

## 功能

- 后台常驻系统托盘，检测鼠标键盘无动作时间，超时自动进入屏保。
- 屏保全屏显示 `./pictures/` 下的图片，自动轮播（可设置间隔），支持多显示器。
- 显示可配置格式的日期时间，以及可自定义的名言（含小一号字体的作者）。
- 日期时间 / 名言的**位置与布局**（居中、靠左、靠右等）均可配置；相同垂直位置时自动错开（时间在上、名言在下）。
- **退出**：点击空白区域或按下任意键退出（仅移动鼠标不退出）。
- 右上角有 **设置** 和 **喜欢** 按钮；点击“喜欢”提升该图片的出现概率。
- 移动鼠标在屏幕左右两侧出现**箭头**，点按切换上一张 / 下一张。
- 切换动画可选：**渐变切换 / 百叶窗切换 / 非线性动画切换 / 无动画**。
- 名言与图片的**语义 + 色调自动匹配**（仅在添加名言或图片时计算），结果存于 `./sentences.csv`。
- 预留云端图片 / 更新图库接口（`src/cloud.h`）。

## 目录结构

```
Screen Poster/
├── src/            源码（Qt）
├── pictures/       屏保图片（放入 .jpg/.png/.bmp 等即可）
├── config/         配置文件（config.ini、likes.txt 运行时生成）
├── resources/      图标（backstage.png / settings.svg / like1.png / like2.png）
├── sentences.csv   名言及与图片的匹配信息
├── tools/          generate_resources.ps1（图标/示例图片生成脚本）
└── CMakeLists.txt
```

## 安装 Qt（部署方法）

本机未安装 Qt，需先任选一种方式安装 Qt 6（含 Widgets 模块）：

### 方式 A：Qt 官方在线安装器（推荐，自带配套 MinGW）

1. 到 https://www.qt.io/download-qt-installer 下载并运行 Qt Online Installer。
2. 登录（可免费注册）后，在 **Qt → Qt 6.x.x** 下勾选 **MinGW 13.x 64-bit**（会一并安装配套的 MinGW 与 Qt Creator）。
3. 安装完成后记录 Qt 的 MinGW 安装路径，例如 `C:\Qt\6.7.3\mingw_64`。

### 方式 B：MSYS2（轻量，命令行）

```bash
pacman -Syu
pacman -S mingw-w64-ucrt-x86_64-qt6-base mingw-w64-ucrt-x86_64-cmake \
          mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-ninja
```
之后在 **MSYS2 UCRT64** 终端里构建。

### 方式 C：aqtinstall（仅下载 Qt 库）

```bash
pip install aqtinstall
aqt install-qt windows desktop 6.7.3 win64_mingw -O C:/Qt
```
注意：还需一个与 Qt 匹配的 MinGW 工具链。

## 编译

```powershell
# 指定 Qt 路径（方式 A/B 按需调整）
$env:CMAKE_PREFIX_PATH = "C:/Qt/6.7.3/mingw_64"

cmake -S . -B build -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release
cmake --build build
```

编译后构建脚本会自动把 `pictures/`、`resources/`、`config/`、`sentences.csv` 复制到可执行文件同目录。

## 运行与部署

直接运行 `build/ScreenPoster.exe`（常驻托盘）：

- **左键点击托盘图标**：立即进入屏保。
- **右键点击托盘图标**：菜单（显示屏保 / 设置 / 退出）。

分发到其他机器时，用 Qt 自带的 `windeployqt` 把 Qt 运行库拷贝到可执行文件旁：

```powershell
& "C:/Qt/6.7.3/mingw_64/bin/windeployqt.exe" build/ScreenPoster.exe
```

### 屏保内操作

| 操作 | 效果 |
|------|------|
| 移动鼠标到左右两侧 | 显示切换箭头 |
| 点击左/右箭头 | 上一张 / 下一张 |
| 点击右上角 ♥ | 喜欢/取消喜欢当前图片（提升概率） |
| 点击右上角齿轮 | 退出屏保并打开设置 |
| 点击空白处 / 按任意键 | 退出屏保 |

## 配置说明

- `config/config.ini`：无操作超时、轮播间隔、切换动画、日期时间格式与布局、名言布局、作者位置等。
  - 日期时间格式使用 Qt 格式串，例如 `yyyy年MM月dd日 hh:mm:ss`（`yyyy`=年 `MM`=月 `dd`=日 `hh`=时 `mm`=分 `ss`=秒）。
- `config/likes.txt`：已喜欢的图片文件名列表（自动维护）。
- `sentences.csv`：每行 `名言,作者,图片名=匹配度;...`。匹配度 0~1，仅在添加名言或图片时由程序自动计算。

## 添加名言与图片

1. 右键托盘 → 设置 → 在“名言管理”点击“添加名言”，程序自动分析语义并与图库色调匹配，写入 `sentences.csv`。
2. 把图片丢进 `pictures/` 后，点“扫描新图片”，程序自动为新增图片计算与各名言的匹配度。

## 预留接口（云端图库）

`src/cloud.h` 中定义了：

- `ICloudImageProvider`：鉴权、拉取图片列表、下载图片的抽象接口。
- `GalleryUpdater`：`update()` 负责拉取、下载新图片、重扫图库并仅对新增图片做匹配。

未来接入云端时，实现 `ICloudImageProvider` 并 `setProvider(...)` 即可。

## 备注

- 屏幕保窗口覆盖每个显示器的虚拟桌面（多屏每屏独立显示同一张图）。
- 图标：`backstage.png` / `like1.png` / `like2.png` 由 Qt 原生加载；“设置”齿轮由 QPainter 绘制，`settings.svg` 保留为资源供后续矢量渲染。
- 无操作检测：Windows 用 `GetLastInputInfo`，macOS 用 `CGEventSource`，Linux 留了 X11 参考实现。
