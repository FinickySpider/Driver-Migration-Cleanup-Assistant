# Driver Migration Cleanup Assistant (DMCA)
## Version 1.0 — Compiled Application

This folder contains the ready-to-use **DMCA** application.

---

## 🚀 Quick Start

### 1. Prerequisites
- **Windows 10/11** (64-bit)
- **.NET 8 Desktop Runtime** — [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Administrator privileges** (required for driver/service operations)

### 2. Set Your OpenAI API Key

The **AI Advisor** feature requires an OpenAI API key. Set it as an environment variable:

**Option A: System Settings (Permanent)**
1. Press `Win + R`, type `sysdm.cpl`, press Enter
2. Click **Environment Variables**
3. Under **User variables**, click **New**
4. Variable name: `OPENAI_API_KEY`
5. Variable value: `sk-your-actual-key-here`
6. Click **OK**, close all dialogs

**Option B: PowerShell (Permanent)**
```powershell
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-your-actual-key-here", "User")
```

**Option C: PowerShell (Temporary — current session only)**
```powershell
$env:OPENAI_API_KEY = "sk-your-actual-key-here"
.\Dmca.App.exe
```

### 3. Run the Application

Double-click **`Dmca.App.exe`** or run from PowerShell:
```powershell
.\Dmca.App.exe
```

**Important:** Right-click → **Run as Administrator** for full functionality.

---

## 📁 Files Included

### Required Files
- **Dmca.App.exe** — Main application
- **rules.yml** — Scoring rules for driver evaluation
- **openai_tools.json** — AI tool definitions
- **ai_tool_policy_prompt.txt** — AI system prompt
- **All .dll files** — Application dependencies

### Optional
- **SETUP.md** — Detailed setup instructions

---

## 🔑 Getting an OpenAI API Key

1. Go to [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
2. Sign in or create an account
3. Click **Create new secret key**
4. Copy the key (starts with `sk-`)
5. **Save it securely** — you won't see it again
6. Set the environment variable as shown above

---

## ✅ Verify Setup

When you launch `Dmca.App.exe`:

- ✅ **AI Advisor works** → API key is set correctly
- ❌ **"AI Advisor error"** → Check your API key:
  - Ensure it starts with `sk-`
  - Verify it's set as the `OPENAI_API_KEY` environment variable
  - Restart the application after setting the variable
  - Check you have credits in your OpenAI account

---

## 📖 User Guide

See the [full user guide](../../docs/user-guide.md) for:
- Complete workflow walkthrough
- Scoring system explanation
- Hard blocks reference
- Troubleshooting tips
- FAQ

---

## 🛠 Troubleshooting

### Application won't start
- Install [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Right-click → **Run as Administrator**

### AI Advisor doesn't work
- Check `OPENAI_API_KEY` environment variable is set
- Restart the application after setting the key
- Verify your OpenAI account has available credits

### Missing features (scoring, execution)
- Ensure all files in this folder are present
- Don't move or delete `rules.yml`, `openai_tools.json`, or `.dll` files
- Run as Administrator

### Database errors
- Database is stored in: `%LOCALAPPDATA%\DMCA\dmca.db`
- Delete this file to reset (you'll lose all session data)

---

## 📦 Distribution

To share this application:
1. Zip the entire `win-x64` folder
2. Users extract and run `Dmca.App.exe`
3. Each user must set their own `OPENAI_API_KEY`

**Note:** Do NOT include your API key in the distribution. Each user needs their own key.

---

## 🔒 Security

- **Never commit your API key to source control**
- **Never share your API key publicly**
- Environment variables keep your key secure and separate from the application
- OpenAI charges are tied to your API key — protect it!

---

## 📄 License & Credits

**Driver Migration Cleanup Assistant (DMCA)**  
Copyright © 2026 FinickySpider  

Icon: `Wrench_Black.ico` from assets/icons

For source code and developer documentation, see the repository root.
