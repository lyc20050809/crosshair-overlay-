# 怎么把这个项目发布到 GitHub（零基础版）

> 前提：这台机器暂时没装 Git（我尝试用 winget 自动安装，没成功）。所以下面给两条**不需要 Git** 的路，选一条就行。

---

## 方式 A：网页拖拽上传（最简单，5 分钟，推荐先用这个）

1. 打开 <https://github.com>，注册或登录账号。
2. 右上角 `+` → **New repository**：
   - Repository name：`crosshair-overlay`
   - 选 **Public**
   - **不要**勾 "Add a README file"（我们已经有了）
   - 点 **Create repository**
3. 新页面中间有一段 "…or create a new repository on the command line"，忽略它；
   找 **"uploading an existing file"** 这个链接并点进去。
4. 回到资源管理器，打开 `crosshair_overlay` 文件夹：
   - **全选**里面的所有文件和文件夹（`src`、`docs`、`README.md`、`LICENSE`…）
   - **拖进**网页的上传区域
   - 注意：`crosshair.ini` / `presets.ini` / `profiles.ini` 这三个是你自己的运行时配置，**不要上传**（里面有你的个人设置）。`crosshair.example.ini` 可以传。
5. 页面下方 Commit changes 填一句 `Initial commit`，点 **Commit changes**。

完成，仓库就上线了：`https://github.com/<你的用户名>/crosshair-overlay`

### 之后怎么改

网页上点某个文件 → 右上角铅笔图标 → 改 → Commit，就能更新。

---

## 方式 B：GitHub Desktop（长期维护推荐）

1. 下载安装 <https://desktop.github.com/>（免费，图形界面，不用敲命令）。
2. 打开后登录 GitHub 账号。
3. 菜单 **File → Add local repository…**，选择
   `C:\Users\47733\Documents\deepseek-harness\default-workspace\crosshair_overlay`
   - 它会提示 "not a Git repository" → 点 **create a repository** 即可。
4. 左下角会列出所有文件，Summary 填 `Initial commit`，点 **Commit to main**。
5. 顶部点 **Publish repository**，取消勾选 "Keep this code private"，点 Publish。

以后每次改完：在 GitHub Desktop 里写一句说明 → Commit → Push。

---

## 发布一个「Release」（让别人直接下载 exe）

仓库页面右侧 → **Releases** → **Create a new release**：

- Tag：`v1.0.0`
- Title：`Crosshair Overlay 1.0.0`
- 把 `dist\CrosshairOverlay-1.0.0-win64.zip` 拖到附件区
- 点 **Publish release**

这个 zip 里是编译好的 `CrosshairOverlay.exe`、示例配置和说明，别人下载就能用，不需要装 .NET SDK。

---

## 上传前检查清单

| 项目 | 状态 |
|---|---|
| `README.md` 有没有写清楚怎么用 | ✅ 已有 |
| `LICENSE` 有没有 | ✅ MIT |
| 有没有泄露个人配置 | ⚠️ `crosshair.ini`、`presets.ini`、`profiles.ini` 不要上传 |
| 有没有可执行文件混进源码 | ✅ `.gitignore` 已排除 `*.exe` |
| 有没有截图 | ❌ 建议补 2–3 张（准星效果 / F6 设置窗 / 存档库） |

---

## 常见问题

**Q：一定要用命令行吗？**
A：不用。方式 A 全程网页操作，方式 B 是图形界面。

**Q：别人下载源码后怎么编译？**
A：装好 .NET Framework（Win10/11 自带），右键 `build.ps1` → 使用 PowerShell 运行；或者在 PowerShell 里执行：

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

**Q：exe 会不会被 GitHub 拒绝？**
A：不会，但源码仓库通常不放 exe；用 Release 附件分发更规范。

**Q：以后想加 CI（自动构建 + 跑测试）？**
A：在仓库里建 `.github/workflows/build.yml`，Windows runner 上跑 `build.ps1` 和
`CrosshairOverlay.CLI.exe --selftest` 即可，需要的话我可以帮你写。

---

## 方式 C：一键脚本 publish.ps1（装好 Git 之后最省事）

1. 安装 Git：<https://git-scm.com/download/win>（一路默认下一步）；
2. 在项目文件夹里按住 `Shift` 右键 →「在终端中打开」；
3. 执行：

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1
```

脚本会自动：初始化仓库 → 设置提交身份 → 写 `.gitignore`（排除 exe 和你自己的 ini）→
提交 → 提示你粘贴仓库地址 → 推送。第一次推送会弹浏览器登录授权。

也可以直接带参数：

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1 -Remote https://github.com/你的名字/crosshair-overlay.git
```