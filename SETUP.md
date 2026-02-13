# DMCA Setup Guide

## Quick Start

### 1. Set Your OpenAI API Key

The AI Advisor requires an OpenAI API key. Set it as an **environment variable**:

**PowerShell (recommended for permanent setup):**
```powershell
# Set for current user (persists across sessions)
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-your-key-here", "User")

# Restart the application after setting
```

**PowerShell (temporary — current session only):**
```powershell
$env:OPENAI_API_KEY = "sk-your-key-here"
# Then run Dmca.App.exe from the same PowerShell window
```

**Windows System Properties:**
1. Open **Start** → type "environment variables"
2. Click **Edit the system environment variables**
3. Click **Environment Variables** button
4. Under **User variables**, click **New**
5. Variable name: `OPENAI_API_KEY`
6. Variable value: `sk-your-key-here`
7. Click **OK** on all dialogs
8. **Restart** the DMCA application

### 2. Get an OpenAI API Key

1. Go to [platform.openai.com](https://platform.openai.com/api-keys)
2. Sign in or create an account
3. Click **Create new secret key**
4. Copy the key (starts with `sk-`)
5. **Save it securely** — you won't see it again

### 3. Verify Setup

When you launch **Dmca.App.exe**:
- If the API key is set correctly, the **🤖 AI Advisor** tab will be functional
- If not set, the AI Advisor will show an error when you try to chat

## Optional Files

The application automatically looks for these files in the same directory as `Dmca.App.exe`:

- **`rules.yml`** — Scoring rules (copy from `Design-And-Data/rules/rules.yml`)
- **`openai_tools.json`** — AI tool definitions (copy from `Design-And-Data/ai/openai_tools.json`)

If these files are not present:
- Scoring will use default rules
- AI Advisor will work without custom tool definitions

## Troubleshooting

### "AI Advisor error" message
- Check that `OPENAI_API_KEY` is set correctly
- Verify the key starts with `sk-`
- Ensure you have API credits in your OpenAI account

### Application won't start
- Ensure **.NET 8 Desktop Runtime** is installed: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- Run as **Administrator** (required for driver/service operations)

### No scoring or AI features
- Copy `rules.yml` and `openai_tools.json` to the application directory
- Check the application log in `%LOCALAPPDATA%\DMCA\`

## Security Note

**Never commit your API key to source control.** The environment variable approach keeps your key out of the codebase and allows each user to use their own key.
