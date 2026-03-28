$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ProgressPreference = 'SilentlyContinue'
$logPath = Join-Path $PSScriptRoot 'install-flutter-shiphub.log'
try { Stop-Transcript | Out-Null } catch {}
Start-Transcript -Path $logPath -Append | Out-Null
$logsDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

# Self-elevate to Administrator if needed
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
  $psi = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
  Start-Process -FilePath "powershell.exe" -ArgumentList $psi -Verb RunAs
  exit
}

function Ensure-Command($name) {(Get-Command $name -ErrorAction SilentlyContinue) -ne $null}

Write-Host "==> Check Chocolatey..." -ForegroundColor Cyan
$chocoBin = Join-Path $env:ProgramData "chocolatey\bin"
$chocoExe = Join-Path $chocoBin "choco.exe"
if (-not (Test-Path $chocoExe)) {
  Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
  [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
  Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
}
$env:Path = "$env:Path;$chocoBin"

Write-Host "==> Installing dependencies to D: drive (Git, Flutter, Android Studio)..." -ForegroundColor Cyan

# Install Git to D:\Git
$gitPath = "D:\Git\bin\git.exe"
if (-not (Test-Path $gitPath)) {
  Write-Host "Installing Git to D:\Git..." -ForegroundColor Cyan
  choco install git -y --install-arguments="'/DIR=D:\Git'" *> (Join-Path $logsDir 'choco-git.log')
} else {
  Write-Host "Git is already installed." -ForegroundColor Green
}
$env:Path += ";D:\Git\bin"

# Install Flutter SDK to D:\flutter
$flutterPath = "D:\flutter\bin\flutter.bat"
if (-not (Test-Path $flutterPath)) {
  Write-Host "Installing Flutter SDK to D:\flutter..." -ForegroundColor Cyan
  $flutterZipUrl = "https://storage.googleapis.com/flutter_infra_release/releases/stable/windows/flutter_windows_3.22.2-stable.zip"
  $flutterZipPath = Join-Path $env:TEMP "flutter.zip"
  Invoke-WebRequest -Uri $flutterZipUrl -OutFile $flutterZipPath
  Expand-Archive -Path $flutterZipPath -DestinationPath "D:\" -Force
  Remove-Item $flutterZipPath
} else {
  Write-Host "Flutter is already installed." -ForegroundColor Green
}

# Check for Android Studio
$androidStudioPathC = "C:\Program Files\Android\Android Studio"
$androidStudioPathD = "D:\Program Files\Android\Android Studio"
if (-not (Test-Path $androidStudioPathC) -and -not (Test-Path $androidStudioPathD)) {
  Write-Host "Installing Android Studio..." -ForegroundColor Cyan
  choco install androidstudio -y --no-progress *> (Join-Path $logsDir 'choco-androidstudio.log')
} else {
  Write-Host "Android Studio is already installed." -ForegroundColor Green
}

Write-Host "==> Refreshing PATH for Flutter..." -ForegroundColor Cyan
$flutterBin = "D:\flutter\bin"
if (Test-Path $flutterBin) {
  # Add to current session PATH
  $env:Path = "$env:Path;$flutterBin"

  # Add to permanent Machine PATH if not already there
  $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
  if ($machinePath -notlike "*$flutterBin*") {
    $newMachinePath = "$machinePath;$flutterBin"
    [Environment]::SetEnvironmentVariable('Path', $newMachinePath, 'Machine')
  }
}

Write-Host "==> Checking Flutter..." -ForegroundColor Cyan
if (-not (Ensure-Command "flutter")) { throw "Flutter not found in PATH. Open a new PowerShell window or restart the PC, then run this script again." }
flutter --version

Write-Host "==> flutter doctor" -ForegroundColor Cyan
flutter doctor

Write-Host "==> Accepting Android licenses..." -ForegroundColor Cyan
try { flutter doctor --android-licenses | Out-Null } catch {}

# ====== Repo Config ======
$RepoUrl     = "https://github.com/hoangvu-8053/ShipHub"
$Branch      = "main"
$ProjectName = "shiphub"
$OrgId       = "com.example"

Set-Location $PSScriptRoot

if (-not (Test-Path ".git")) {
  Write-Host "==> Initializing Git repo..." -ForegroundColor Cyan
  git init
  git branch -m $Branch
  git remote add origin $RepoUrl
}

Write-Host "==> Initializing Flutter project..." -ForegroundColor Cyan
flutter create --project-name $ProjectName --org $OrgId . | Out-Null

Write-Host "==> Adding minimal packages..." -ForegroundColor Cyan
flutter pub add dio flutter_bloc equatable | Out-Null

Write-Host "==> Creating skeleton app files..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path "lib/core/config","lib/core/api","lib/features/auth","lib/features/orders" | Out-Null

$envDart = @'
class Env {
  // Use 10.0.2.2 for Android emulator, localhost for iOS simulator
  static const String apiBase = String.fromEnvironment(
    "API_BASE",
    defaultValue: "http://10.0.2.2:5170",
  );
}
'@
Set-Content -Path "lib/core/config/env.dart" -Value $envDart -Encoding UTF8

$apiClientDart = @'
import "package:dio/dio.dart";
import "../config/env.dart";

class ApiClient {
  final Dio dio;
  ApiClient._internal(this.dio);
  factory ApiClient() {
    final dio = Dio(BaseOptions(
      baseUrl: Env.apiBase,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      headers: {"Content-Type": "application/json"},
    ));
    return ApiClient._internal(dio);
  }
}
'@
Set-Content -Path "lib/core/api/api_client.dart" -Value $apiClientDart -Encoding UTF8

$loginDart = @'
import "package:flutter/material.dart";

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}
class _LoginPageState extends State<LoginPage> {
  final _userCtl = TextEditingController(text: "shipper3");
  final _passCtl = TextEditingController(text: "shipper123");
  bool _loading = false;
  String? _error;
  Future<void> _login() async {
    setState(() { _loading = true; _error = null; });
    try {
      // TODO: call real API /api/shipper/login (JWT)
      if (mounted) Navigator.of(context).pushReplacementNamed("/orders");
    } catch (e) { setState(() { _error = e.toString(); }); }
    finally { if (mounted) setState(() { _loading = false; }); }
  }
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("ShipHub - Login")),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(controller: _userCtl, decoration: const InputDecoration(labelText: "Username")),
            const SizedBox(height: 8),
            TextField(controller: _passCtl, decoration: const InputDecoration(labelText: "Password"), obscureText: true),
            const SizedBox(height: 12),
            if (_error != null) Text(_error!, style: const TextStyle(color: Colors.red)),
            const SizedBox(height: 12),
            ElevatedButton.icon(
              onPressed: _loading ? null : _login,
              icon: _loading ? const SizedBox(width:16, height:16, child:CircularProgressIndicator(strokeWidth:2)) : const Icon(Icons.login),
              label: const Text("Login"),
            )
          ],
        ),
      ),
    );
  }
}
'@
Set-Content -Path "lib/features/auth/login_page.dart" -Value $loginDart -Encoding UTF8

$ordersDart = @'
import "package:flutter/material.dart";
class OrdersPage extends StatelessWidget {
  const OrdersPage({super.key});
  @override
  Widget build(BuildContext context) {
    final items = List.generate(10, (i) => "Order #${i + 1} - Pending");
    return Scaffold(
      appBar: AppBar(title: const Text("ShipHub - Orders")),
      body: ListView.separated(
        itemCount: items.length,
        separatorBuilder: (_, __) => const Divider(height: 1),
        itemBuilder: (ctx, i) => ListTile(
          title: Text(items[i]),
          subtitle: const Text("Tap to see details"),
          trailing: const Icon(Icons.chevron_right),
          onTap: ()=> ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(content: Text("Opening details for: ${items[i]} (placeholder)"))),
        ),
      ),
    );
  }
}
'@
Set-Content -Path "lib/features/orders/orders_page.dart" -Value $ordersDart -Encoding UTF8

$mainDart = @'
import "package:flutter/material.dart";
import "features/auth/login_page.dart";
import "features/orders/orders_page.dart";

void main() => runApp(const ShipHubApp());

class ShipHubApp extends StatelessWidget {
  const ShipHubApp({super.key});
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: "ShipHub",
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.blue),
      initialRoute: "/",
      routes: {
        "/": (_) => const LoginPage(),
        "/orders": (_) => const OrdersPage(),
      },
    );
  }
}
'@
Set-Content -Path "lib/main.dart" -Value $mainDart -Encoding UTF8

flutter pub get | Out-Null

if (-not (Test-Path "README.md")) {
$readme = @"
# ShipHub

Flutter app for Shippers (skeleton).
- Login (placeholder)
- Order list (placeholder)

Next steps: connect to JWT API /api/shipper, SignalR, PoD, location sharing.
"@
Set-Content -Path "README.md" -Value $readme -Encoding UTF8
}

Write-Host "==> Committing & pushing..." -ForegroundColor Cyan
git add .
git commit -m "chore: auto-init Flutter ShipHub (toolchain + skeleton)" | Out-Null
git push origin $Branch

Write-Host "`nCompleted ShipHub setup & initialization!" -ForegroundColor Green
Write-Host "Repo: $RepoUrl"
Write-Host "To run (Android emulator): flutter run"
Write-Host "If Flutter doesn't detect Android, open Android Studio > More Actions > SDK Manager to finish SDK setup."
