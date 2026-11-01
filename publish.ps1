# 一键把本项目发布到 GitHub
# 用法（在项目文件夹里右键“在终端中打开”，或 PowerShell 里 cd 进来）：
#   powershell -ExecutionPolicy Bypass -File publish.ps1
# 或直接指定仓库地址：
#   powershell -ExecutionPolicy Bypass -File publish.ps1 -Remote https://github.com/你的名字/crosshair-overlay.git

param([string]$Remote = "")

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '=== Crosshair Overlay 发布脚本 ===' -ForegroundColor Cyan

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host ''
    Write-Host '没检测到 Git。' -ForegroundColor Yellow
    Write-Host '请先安装：https://git-scm.com/download/win （一路默认下一步即可）'
    Write-Host '装完关掉这个窗口，重新运行一次。'
    exit 1
}

if (-not (Test-Path '.git')) {
    Write-Host '初始化本地仓库 ...'
    git init | Out-Null
    git branch -M main
}

$name = (git config user.name)
if ([string]::IsNullOrWhiteSpace($name)) {
    $name = Read-Host '你的名字（显示在提交记录里，随意填）'
    git config user.name $name
}
$mail = (git config user.email)
if ([string]::IsNullOrWhiteSpace($mail)) {
    $mail = Read-Host '你的邮箱（可随便填，例如 you@example.com）'
    git config user.email $mail
}

# 不要把你自己的运行时配置和构建产物提交上去
$gitignore = @'
*.exe
*.pdb
dist/
crosshair.ini
presets.ini
profiles.ini
smoke_result.txt
selftest*.ini
*.lnk
'@
Set-Content -Path '.gitignore' -Value $gitignore -Encoding UTF8

git add -A
git -c core.quotepath=false commit -m "Crosshair Overlay 1.0.0" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host '没有新的改动需要提交（或已经是第一次提交）。'
}

if ([string]::IsNullOrWhiteSpace($Remote)) {
    Write-Host ''
    Write-Host '现在去 GitHub 网页建一个空仓库（不要勾 README），然后复制它的地址：' -ForegroundColor Cyan
    Write-Host '  https://github.com/<你的用户名>/crosshair-overlay.git'
    $Remote = Read-Host '把仓库地址粘贴到这里'
}

if ([string]::IsNullOrWhiteSpace($Remote)) {
    Write-Host '没有仓库地址，退出。' -ForegroundColor Yellow
    exit 1
}

git remote remove origin 2>$null
git remote add origin $Remote
Write-Host ''
Write-Host '开始推送；如果弹出浏览器登录窗口，点击授权即可。' -ForegroundColor Cyan
git push -u origin main

Write-Host ''
Write-Host '完成。仓库地址：' -ForegroundColor Green
Write-Host ($Remote -replace '\.git$','')
