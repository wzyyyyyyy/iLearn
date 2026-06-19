# iLearn

iLearn 是一个面向“学在吉大”课程学习场景的非官方桌面客户端。它把课程浏览、视频播放、课程下载和本地复习整理到一个更稳定的跨平台应用里，适合在课后复习、通勤前缓存课程、或整理离线资料时使用。

本项目不提供课程资源，不绕过账号权限，也不破解平台限制。你只能访问自己账号本来有权限查看的内容。

## 功能

- 支持 Windows、macOS、Linux 的 Avalonia 桌面端。
- 使用统一认证登录，并保留微信二次验证流程。
- 按学期浏览课程，课程卡片显示平台返回的课程封面。
- 在线播放课程视频，支持字幕与常用播放控制。
- 选择 HDMI / 教师画面加入下载队列。
- 下载管理显示任务状态、实时速度、大小、进度和剩余时间。
- 取消下载后立即从队列移除，失败任务可重试。
- 支持打开下载目录和管理本地视频。

## 下载

请前往 [Releases](https://github.com/wzyyyyyyy/iLearn/releases) 下载最新版本。

常见文件：

- `iLearn-v*-win-x64.zip`：Windows x64
- `iLearn-v*-osx-arm64-unsigned.dmg`：Apple Silicon Mac
- `iLearn-v*-osx-arm64.zip`：Apple Silicon Mac 压缩包
- `iLearn-v*-osx-x64-unsigned.dmg`：Intel Mac
- `iLearn-v*-linux-x64.tar.gz`：Linux x64

macOS 包目前未签名、未公证，首次打开时可能出现 Gatekeeper 提示。

## 从源码运行

需要安装 .NET 10 SDK。

```bash
git clone https://github.com/wzyyyyyyy/iLearn.git
cd iLearn
dotnet restore iLearn.sln
dotnet run --project iLearn/iLearn.csproj
```

## 本地打包

打包产物会输出到 `artifacts/package`。

```bash
scripts/package.sh osx-arm64
scripts/package.sh osx-x64
scripts/package.sh linux-x64
```

Windows 使用 PowerShell：

```powershell
./scripts/package/windows.ps1 -Rid win-x64
```

如果 Windows 环境安装了 Inno Setup CLI `iscc`，脚本会额外生成安装包。

## 技术栈

- .NET 10
- Avalonia
- Semi.Avalonia
- Ursa
- CommunityToolkit.Mvvm

## 说明

iLearn 是个人维护的开源项目，与吉林大学及其官方平台没有隶属关系。平台接口或认证流程变化可能导致功能暂时不可用。

请遵守学校平台规则和课程版权要求。本项目仅用于改善个人学习体验，禁止用于传播受版权保护的内容或任何商业用途。

## 反馈

问题和建议请提交到 [GitHub Issues](https://github.com/wzyyyyyyy/iLearn/issues)。
