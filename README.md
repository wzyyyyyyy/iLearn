# iLearn - 一款现代化的“学在吉大”跨平台客户端

### 基于 Avalonia、Semi.Avalonia 和 Ursa 的 Windows / macOS / Linux 桌面客户端。

![iLearn](https://socialify.git.ci/wzyyyyyyy/iLearn/image?custom_description=%E5%9F%BA%E4%BA%8E%20Avalonia%20%E7%9A%84%E8%B7%A8%E5%B9%B3%E5%8F%B0%E5%AD%A6%E5%9C%A8%E5%90%89%E5%A4%A7%E6%A1%8C%E9%9D%A2%E5%AE%A2%E6%88%B7%E7%AB%AF%E3%80%82&description=1&font=Inter&forks=1&issues=1&language=1&name=1&owner=1&pattern=Formal+Invitation&pulls=1&stargazers=1&theme=Auto)
<p align="center">
  <img src="https://github.com/wzyyyyyyy/iLearn/actions/workflows/dotnet-desktop.yml/badge.svg" />
  <img src="https://img.shields.io/github/downloads/wzyyyyyyy/iLearn/total.svg" />
  <img src="https://www.codefactor.io/repository/github/wzyyyyyyy/ilearn/badge" />
  <img src="https://img.shields.io/badge/.NET-10.0-blue" />
</p>

-----

## ✨ 项目简介

**iLearn** 是一个**非官方、纯公益的开源项目**，用于改善“学在吉大”平台的课程浏览、视频播放、下载和离线复习体验。

> **⚠️ 请注意：** 本项目不提供课程资源，不绕过账号权限。它只能访问您原本有权查看的内容。

## 🚀 核心功能

  * **跨平台桌面体验：** 使用 Avalonia 构建，支持 Windows、macOS、Linux。
  * **在线与离线播放：** 支持播放课程视频，也可下载课程供离线复习。
  * **播放控制：** 提供 **0.5x 至 3.0x** 倍速播放。
  * **课程字幕：** 自动提取课程字幕，点击字幕可跳转到对应时间。
  * **下载管理：** 支持下载队列、进度、取消、失败重试、速度统计和快捷打开下载目录。
  * **状态提示：** 登录、课程加载、下载准备和更新检查均会显示处理状态。
  * **本地视频支持：** 支持导入并播放本地视频文件。

## 📸 界面预览

<p align="center">
  <img src="https://github.com/user-attachments/assets/f9365add-28e6-4588-905e-ab51a45da9ac" width="45%" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/f10d9431-ecf8-4d5b-a83b-f3ee277a4a82" width="45%" />
</p>

## 📥 下载与使用

只需简单几步，即可开始使用 iLearn。

1.  **下载：** 前往 [**📦 Releases 页面**](https://github.com/wzyyyyyyy/iLearn/releases) 获取最新版本。
2.  **环境：** 推荐使用 Releases 中的自包含包；从源码运行需要安装 .NET 10 SDK。
3.  **安装与运行：**
      * **Windows：** 下载 `iLearn-v*-win-x64.zip`，解压后运行 `iLearn.exe`。
      * **macOS：** Apple 芯片下载 `iLearn-v*-osx-arm64-unsigned.dmg`，Intel 芯片下载 `iLearn-v*-osx-x64-unsigned.dmg`。安装包暂未签名和公证，首次运行时可能出现系统安全提示。
      * **Linux：** 下载 `iLearn-v*-linux-x64.tar.gz`，解压后运行 `iLearn`。

## 📦 本地打包

项目提供三平台打包脚本，产物会输出到 `artifacts/package`。

```bash
scripts/package.sh osx-arm64
scripts/package.sh osx-x64
scripts/package.sh linux-x64
```

Windows 打包在 PowerShell 中运行：

```powershell
./scripts/package/windows.ps1 -Rid win-x64
```

Windows 默认生成 zip；如果本机安装了 Inno Setup CLI `iscc`，会额外生成安装向导 exe。macOS 默认生成 unsigned app zip，在 macOS 上会额外生成 unsigned dmg。

## 🗺️ 开发路线图

以下是项目关键功能的当前开发状态：

#### **🔧 核心功能**

  - [x] 在线播放课程
  - [x] 视频下载与离线播放
  - [x] 倍速播放控制
  - [x] Avalonia / Semi.Avalonia 跨平台界面
  - [x] 本地视频导入

#### **📋 字幕系统**

  - [x] 自动提取课程字幕
  - [x] 点击字幕跳转时间轴

-----

## 📄 免责声明

本项目为个人开发的学习交流项目，与**吉林大学及其官方平台无任何关联**。

  * 本软件仅供技术交流与学习使用，**严禁用于传播受版权保护的内容**。
  * 作为独立项目，**不承诺长期维护或保证与平台更新的适配稳定性**。
  * **本工具严禁用于任何商业用途**，违者后果自负。
  * 所有内容的版权归原平台所有，本项目开发者**不承担任何因使用本工具而产生的法律责任**。

-----

## ❤️ 支持与贡献

如果您觉得这个项目对您有帮助，请在 GitHub 上点亮一颗 ⭐ **Star**！

我们欢迎各种形式的贡献与反馈。您可以随时提交 Issue 或 Pull Request。

  * **📧 邮箱：** `382271046@qq.com`
  * **🌐 GitHub Issues:** [wzyyyyyyy/iLearn/issues](https://github.com/wzyyyyyyy/iLearn/issues)
