# 实现与验证

拾光画报用 C# 5 / WPF / .NET Framework 4.8 实现，全部依赖来自 Windows 上已有的 Framework。`build.ps1` 直接调用 `csc.exe`，将 XAML 和三张 PNG 嵌入程序集，输出可执行文件及相同内容的 `.scr` 文件。

## 实现划分

| 文件 | 职责 |
| --- | --- |
| `Program.cs`、`Models.cs` | 入口、屏保参数、设置模型、原子 JSON 保存 |
| `MainWindow.xaml`、`Theme.xaml` | 主窗口与公共控件样式 |
| `MainController.cs`、`SettingsPage.cs` | 画廊、收藏、本地目录和设置交互 |
| `Photos.cs` | 图片目录、必应缓存、受限图片解码、EXIF 方向 |
| `PosterView.cs`、`Saver.cs` | 全屏视图、轮播、输入退出、多屏和系统缩略预览 |
| `Native.cs` | Windows 屏保注册、设置备份与恢复 |
| `Verification.cs` | 离屏验证与可选原生窗口测试 |

主界面异步读取本地目录和缩略图。轮播在后台解码下一张图片，将冻结的位图共享给所有显示器，使用 WPF 不透明度动画过渡。位图解码不放大小图，长边限制为 3840 像素，缓存按解码像素总量和条目数限制。网络请求设有超时、下载大小上限、HTTPS 和缓存路径校验。

全屏预览和自动屏保使用同一个 `ExitMode` 设置：`escape` 为按 Esc 退出，`blank-click` 为鼠标左键点击非操作区域退出；鼠标移动不触发退出。预览额外支持切图、暂停和收藏，Esc 模式保留显式退出按钮。配置版本为 2，版本 1 的 `ExitOnMouseMove` 字段被忽略，缺少或无效的退出方式使用 Esc 默认值。

空白点击通过窗口的预览鼠标事件处理，检查原始事件源的视觉 / 逻辑祖先，将操作栏及其按钮内部内容排除。鼠标按下与松开必须都在同一个屏保窗口的非操作区域，启动后有 0.6 秒防误触间隔。隐藏的操作栏同时停止鼠标命中，移动鼠标后恢复操作栏与指针。

多显示器以屏幕像素边界放置原生窗口；修改显示器配置会关闭当前屏保。`/p` 使用子级 `HwndSource`，跟随父窗口大小，不创建全屏窗口。

“启用系统屏保”只写当前用户注册表和用户数据目录，不复制文件到 Windows/System32。先备份原有配置，再更新 `SCRNSAVE.EXE`，通过 `SystemParametersInfoW` 更新等待时间与启用状态。恢复登录要求保持由 Windows 管理。

## 已执行的检查

- 39 项离屏功能检查通过：参数、设置持久化及损坏恢复、旧配置迁移、退出选项保存与界面提示、按钮内容区域识别、内置图片、相册扫描去重、损坏图片跳过、EXIF 5/6/7/8 方向、来源/收藏筛选、随机无重复轮次、公历农历以及页面渲染。
- 原生窗口测试通过：主界面、设置、嵌入式系统预览、当前所有显示器窗口、全屏图像、下一张、暂停。预览及自动屏保均验证了 Esc 模式与空白左键点击模式，其他按键、鼠标移动、右键、按钮内容点击以及按钮与背景之间的拖动不会误触退出。
- 实际必应请求通过，下载并解码八张图片。
- 构建编译成功，无编译警告。

测试配置和网络图片保存在项目 `artifacts/` 下；测试没有修改用户的系统屏保注册或等待时间。系统注册的启用、回退及 Windows 实际空闲触发没有在用户机器上执行。不同电脑的 DPI / 多屏组合仍需在对应硬件上验证。

## 原生接口资料

- Microsoft：[SystemParametersInfoW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow)，屏保等待时间及启用状态。
- Microsoft：[SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)，显示器窗口位置与大小。
- Microsoft：[HwndSourceParameters.ParentWindow](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndsourceparameters.parentwindow)，系统缩略预览的子窗口。

内置图片属于 AI 生成的风景作品；标题和心情短句为项目文案。必应每日壁纸来自其公开图片接口，保留接口提供的版权说明，不把在线照片打包为内置素材。
