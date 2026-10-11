# 拾光画报

一个受早期 WPS 画报体验启发的 Windows 原生屏保：大幅风景、简洁时钟、轻柔轮播，以及自己的照片和收藏。

**使用 GPT-6 Astra、GPT-5.6 Terra、DeepSeek V4.1 Flash 等AI模型生成。**

## 新手安装

适用于 **Windows 10 / 11**。若提示缺少 .NET Framework，请先安装微软的 [.NET Framework 4.8 运行时](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)；已安装 4.8 / 4.8.1 可跳过。

**方式一：从 GitHub 下载压缩包**

- **成品包**：在 **Releases → Assets** 下载 `ShiguangPoster-*-win.zip`，右键 **全部解压**，双击其中的 `ShiguangPoster.exe`。
- **源码包**：通过 **Code → Download ZIP** 或 **Source code (zip)** 下载并全部解压，在含 `build.ps1` 的文件夹地址栏输入 `powershell` 回车，执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

编译完成后双击 `dist\ShiguangPoster\ShiguangPoster.exe`。

**方式二：克隆源码**

先安装 [Git for Windows](https://git-scm.com/install/windows)。在仓库页 **Code → HTTPS** 复制地址，打开 PowerShell，替换下面第一行的中文后依次执行：

```powershell
git clone https://github.com/BodhiDog/ShiguangPoster.git ShiguangPoster
cd .\ShiguangPoster
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
.\dist\ShiguangPoster\ShiguangPoster.exe
```

**启用自动屏保**：打开软件，进入 **屏保设置**，选择空闲时间，点击 **启用系统屏保**。默认按 **Esc** 退出，也可改为点击空白位置退出。

**登录自动启动与卸载**：`build.ps1` 会为当前 Windows 用户设置“登录后打开拾光画报”，下次登录或重启生效；发布包用户可在 **屏保设置** 打开同名开关。卸载时双击发布目录中的 `uninstall.cmd`，它只移除登录启动项、屏保注册和本地数据，不会删除原始照片，也不会改动你后来在 Windows 中修改的屏保选项。

## 项目预览

可运行版本：`dist/ShiguangPoster/ShiguangPoster.exe`。使用 .NET Framework 4.8 编译，**不需要预装任何运行环境或第三方依赖**。

![屏保预览](docs/screensaver.png)

## 开始使用

1. 双击 `ShiguangPoster.exe` 打开主界面。
2. 点击 **预览屏保**，或在画报上点击 **沉浸欣赏**。
3. 需要自动屏保时，进入 **屏保设置**，选择空闲分钟数，点击 **启用系统屏保**。
4. 需要播放自己的照片时，进入 **我的相册 → 添加照片文件夹**。

启用后可以关闭主窗口，Windows 仍会在空闲时启动屏保。屏保和登录启动直接使用安装目录中的程序文件，因此启用后请不要移动或删除这个目录；`build.ps1` 默认会登记当前用户登录启动（不会在 Windows 登录界面之前运行，组织策略或启动应用管理仍可禁用启动项）。

首次打开不会自动启用系统屏保；停用时点击 **停用拾光屏保**，程序会恢复启用前保存的屏保路径、启用状态和等待时间，但不会覆盖你后来在 Windows 中改用的其他屏保。已启用旧版系统屏保时，请在 **屏保设置** 中点击一次 **更新系统屏保** 同步已安装副本。恢复时是否显示登录屏幕由 Windows 屏保设置管理，软件内提供对应入口。

## 已实现

- **画报精选**：三张内置 AI 风景、山野/海岸/旷野分类、实时时钟预览。
- **本地相册**：读取 JPG、JPEG、PNG、BMP、TIF、TIFF，可含子文件夹；支持手机照片的 EXIF 旋转和镜像方向。
- **收藏**：画报爱心收藏，可设置只播放收藏。
- **屏保播放**：顺序或随机轮播、淡入淡出、暂停、前后切换、亮度调整。
- **退出方式**：在 **屏保设置 → 播放的节奏 → 退出屏保方式** 中选择 **按退出键（Esc）退出** 或 **鼠标点击屏保空白位置退出**；设置自动保存，对下一次全屏预览和自动屏保生效，旧配置默认 Esc 模式。
- **时钟日期**：12/24 小时制、公历、星期、农历、左下或居中布局、心情短句开关。
- **多显示器**：全部屏幕同步显示同一张画报，也可只显示主屏；显示器配置变化时退出屏保。
- **每日壁纸**：默认关闭；打开后从必应下载最近八天的图片，缓存可选择 1 周、1/3/6 个月、1 年自动删除或不自动删除，打开主程序或运行系统屏保时检查当日更新，断网时继续使用已有图片。
- **Windows 原生屏保**：随包提供 `.scr`，支持 `/s` 运行、`/c` 设置、`/p HWND` 系统缩略预览。
- **本地保存**：设置自动保存；损坏设置备份后回退，无法读取的图片会跳过。

相册最多载入 1500 张图片、50 个文件夹，画廊分页显示；移除相册只移除目录引用，原图保留在原处。所选播放来源或收藏为空时使用内置画报。必应接口和图片由第三方提供，可能受网络或服务变动影响；在线画报显示来源说明，内置图片明确标记为 AI 生成风景。

## 快捷键

| 场景 | 按键 | 操作 |
| --- | --- | --- |
| 主界面 | F5 | 开始全屏预览 |
| 全屏预览 | ← / → | 上一张 / 下一张 |
| 全屏预览 | 空格 | 暂停 / 继续 |
| 全屏预览 | F | 收藏 / 取消收藏 |
| Esc 退出模式（预览 / 自动屏保） | Esc | 退出；预览时返回主界面 |
| 空白点击模式（预览 / 自动屏保） | 鼠标左键点击空白位置 | 退出；预览时返回主界面 |

全屏预览的操作按钮在鼠标静止五秒后自动隐藏，移动鼠标重新显示。两种模式下，移动鼠标和其他普通按键都不会退出；空白点击模式下 Esc 也不退出，图片、日期、文字等非操作区域均可点击退出；点击切图、暂停或收藏按钮，以及从按钮拖到背景后松开鼠标，都不会触发空白退出。Esc 模式的全屏预览也保留屏幕上的“退出”按钮。

## 文件与数据

便携包包含可执行程序、`build.ps1`、`package.ps1`、`package.cmd`、卸载脚本、`VERSION`、`src/`、`assets/`、`README.md` 和 `docs/`；三张风景图和界面资源均嵌入程序，不需要额外复制图片目录。

用户数据默认位于软件安装目录的 `data/`（首次运行后创建）：`settings.json` 保存屏保偏好、本地相册路径、收藏列表和缓存保留期限；`cache/`、`bing.json` 缓存必应图片及来源；`previous-screensaver.json` 备份启用前的屏保设置；`app.log` 记录启动、读取或网络错误。

本地照片只在本机读取。默认不联网，只有开启每日壁纸后才连接必应。运行不需要管理员权限；电脑上的组织策略可能限制 Windows 屏保设置。

## 构建与验证

目标为 Windows 10/11、.NET Framework 4.8，使用系统已有的 Framework C# 编译器及 WPF，无 NuGet、Node.js 或 Python 依赖。在项目目录运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Verify
```

`ExecutionPolicy Bypass` 只作用于这一次构建进程，不会修改系统或用户的执行策略；脚本找不到编译器时会报错，不会自动下载或安装环境。构建成功后会注册当前用户的登录启动，若只生成文件而不创建启动项，追加 `-NoLoginStartup`。

## 更新版本并一键打包

1. 打开根目录的 `VERSION`，将其唯一一行改为版本号，例如 `1.0.1`。
2. 双击 `package.cmd`。

脚本会同步更新程序版本并生成 `dist\ShiguangPoster-1.0.1-win.zip`。压缩包包含构建、打包和卸载脚本，以及 `src/`、`assets/`；临时文件自动清理，`dist` 只保留 ZIP 压缩包，且不会注册当前电脑的登录启动项。也可在 PowerShell 中运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\package.ps1`。

命令行示例：

```powershell
.\dist\ShiguangPoster\ShiguangPoster.exe --preview
.\dist\ShiguangPoster\ShiguangPoster.scr /s
.\dist\ShiguangPoster\ShiguangPoster.scr /c
```

`--data-dir <目录>` 可将数据隔离在指定目录（测试用，不接触正式用户配置）；`--verify` 执行离屏检查并生成截图；`--check-online` 验证实际图片下载；`--smoke-test` 短暂打开原生窗口和全屏测试，结束后自动关闭。这些测试不会执行系统屏保启用/停用操作。

验证覆盖设置读写与损坏恢复、旧退出设置迁移、参数解析、相册读取去重、图片解码与 EXIF 方向、随机轮播、收藏筛选、农历日期、界面渲染，以及原生预览、切图、暂停、两种退出方式和按钮误触防护。实际的空闲触发与恢复登录行为，需要用户启用后由当前 Windows 设置决定。

项目结构：`src/` 源代码，`assets/` 内置图片，`build.ps1` 构建脚本，`artifacts/` 验证产物，`dist/` 便携成品。

内置图片的生成方式、文件路径和完整提示词见 [素材说明](docs/ASSETS.md)。实现与测试详情见 [开发说明](docs/DEVELOPMENT.md)。
