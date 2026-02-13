# DMCA — User Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
4. [Getting Started](#getting-started)
5. [Full Workflow](#full-workflow)
6. [Understanding Scores & Recommendations](#understanding-scores--recommendations)
7. [Hard Blocks](#hard-blocks)
8. [AI Advisor](#ai-advisor)
9. [Execution](#execution)
10. [Delta Report & Verification](#delta-report--verification)
11. [Troubleshooting](#troubleshooting)
12. [FAQ](#faq)

---

## Introduction

The **Driver Migration Cleanup Assistant (DMCA)** is a Windows desktop utility that helps you clean up leftover drivers, services, driver-store packages, and installed programs after a major hardware change (e.g., switching from an Intel to an AMD platform).

DMCA:
- **Inventories** all drivers, services, driver packages, and programs on your system
- **Scores** each item using deterministic rules to recommend safe removal
- **Blocks** critical items (boot drivers, inbox drivers, present hardware) from removal
- **Integrates with OpenAI** for intelligent cleanup suggestions
- **Executes** approved removals with full audit logging and restore-point protection
- **Verifies** results with a post-execution rescan and delta report

---

## Prerequisites

- **Operating System:** Windows 10 or Windows 11
- **.NET 8 Runtime:** Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) (the Desktop Runtime)
- **Administrator Privileges:** Required for driver/service operations
- **Internet Connection:** Only required if using the AI Advisor (OpenAI API key needed)

---

## Installation

1. Download the latest DMCA release
2. Extract to a folder of your choice (e.g., `C:\Tools\DMCA`)
3. Ensure the .NET 8 Desktop Runtime is installed
4. Run `Dmca.App.exe` as Administrator

> **Note:** DMCA must run as Administrator to access driver and service information, create restore points, and perform cleanup actions.

---

## Getting Started

### First Launch

When you launch DMCA, you'll see the main window with a sidebar navigation:

| Page | Purpose |
|------|---------|
| **Interview** | Describe your hardware migration scenario |
| **Inventory** | View all discovered drivers, services, packages, and programs |
| **Details** | Inspect individual item properties |
| **AI Advisor** | Chat with the AI for cleanup recommendations |
| **Proposals** | Review and approve/reject AI-suggested changes |
| **Execute** | Run approved cleanup actions with dry-run and live modes |
| **Delta Report** | Compare before/after snapshots and verify results |

---

## Full Workflow

### Step 1: Interview

The Interview page guides you through three steps:

1. **Old Hardware** — Enter details about your previous hardware (e.g., "Intel Core i7-12700K, ASUS Z690 motherboard")
2. **New Hardware** — Enter details about your current hardware (e.g., "AMD Ryzen 9 7950X, MSI X670E motherboard")
3. **Additional Context** — Any other relevant info (e.g., "Keeping the same NVIDIA GPU", "Did a clean Windows install but drivers carried over")

Click **Finish Interview** to save your hardware context and create a session.

### Step 2: Scan

Navigate to the **Inventory** page and click **Scan**. DMCA will:

- Enumerate all PnP drivers (via WMI)
- Check device presence for each driver
- List all driver-store packages (via `pnputil /enum-drivers`)
- Enumerate services (via Service Control Manager)
- List installed programs (via registry)

The scan typically completes in 10–30 seconds.

### Step 3: Review Inventory

The inventory table shows all discovered items with:

- **Type:** DRIVER, SERVICE, DRIVER_PACKAGE, or APP
- **Display Name:** Human-readable name
- **Vendor:** Manufacturer or provider
- **Item ID:** Unique identifier (e.g., `drv:oem12.inf`, `svc:IntelHaxm`)

Use the **filter** to search by name, vendor, or item ID. Click any item to see its full details.

### Step 4: Generate Plan

After scanning, DMCA automatically generates a decision plan by:

1. Evaluating each item against the rules engine (vendor matching, hardware presence, service criticality)
2. Computing a baseline removal score (0–100)
3. Applying hard blocks for protected items
4. Assigning a recommendation: KEEP, REVIEW, REMOVE_STAGE_1, REMOVE_STAGE_2, or BLOCKED

### Step 5: AI Advisor (Optional)

Navigate to the **AI Advisor** page to chat with the OpenAI-powered advisor. The advisor can:

- Analyze your specific migration scenario
- Suggest additional items for removal
- Explain why certain items are recommended for keeping
- Create proposals with evidence-backed rationale

> **Safety:** The AI can only read data and create proposals — it cannot approve, execute, or modify the system directly.

### Step 6: Review Proposals

If the AI creates proposals, review them on the **Proposals** page:

- Each proposal lists specific changes (score adjustments, recommendation changes)
- **Approve** merges the proposal into the plan
- **Reject** discards the proposal

### Step 7: Execute

Navigate to the **Execute** page:

1. **Build Queue** — Creates an ordered action queue from the plan
2. **Dry Run** — Simulates all actions without making changes (recommended first!)
3. **Live Execution** — Requires two-step confirmation:
   - Check "I acknowledge the risk"
   - Type `EXECUTE` in the confirmation field

A system restore point is always created before any destructive actions.

### Step 8: Verify

Navigate to the **Delta Report** page:

1. Click **Rescan & Generate** to run a fresh scan and compare with the original
2. Review removed, changed, and unchanged items
3. Follow the next-steps guidance (typically: reboot, verify stability)
4. Optionally export the report as Markdown

---

## Understanding Scores & Recommendations

### Scoring

Each item receives a **baseline score** from 0–100:

| Score Range | Meaning |
|-------------|---------|
| 0–25 | Very safe to remove — likely leftover from old hardware |
| 26–50 | Probably safe — review recommended |
| 51–75 | Caution — may be needed by current hardware |
| 76–100 | Critical — should not be removed |

### Recommendations

| Recommendation | Meaning |
|----------------|---------|
| **REMOVE_STAGE_1** | Safe to remove — low risk |
| **REMOVE_STAGE_2** | Remove with caution — moderate risk |
| **REVIEW** | Needs manual review before deciding |
| **KEEP** | Should be kept — high risk to remove |
| **BLOCKED** | Cannot be removed — protected by hard block |

### AI Score Delta

The AI advisor can adjust scores by ±25 (default) or ±40 (with user-fact confirmation). This allows fine-tuning based on your specific hardware context.

---

## Hard Blocks

Hard blocks are non-overridable protections:

| Block Code | Meaning |
|------------|---------|
| `MICROSOFT_INBOX` | Microsoft inbox driver — part of Windows |
| `BOOT_CRITICAL` | Required for system boot |
| `PRESENT_HARDWARE_BINDING` | Currently bound to present hardware |
| `POLICY_PROTECTED` | Protected by system policy |
| `DEPENDENCY_REQUIRED` | Required by other critical components |

Hard-blocked items are always marked as **BLOCKED** and cannot be removed by the engine or the AI advisor.

---

## AI Advisor

### Setup

To use the AI Advisor, set the `OPENAI_API_KEY` environment variable:

```
set OPENAI_API_KEY=sk-your-key-here
```

Or set it permanently in System Properties → Environment Variables.

### How It Works

The AI advisor:
1. Reads your session context (user facts, inventory, plan)
2. Analyzes items using its knowledge of hardware and drivers
3. Creates proposals with specific changes and evidence
4. You approve or reject each proposal

### Safety Guardrails

- **8 allowed tools only:** read data + create proposals (no execution access)
- **Max 5 changes per proposal**
- **Forbidden phrases detected:** "auto-approve", "executing now", etc.
- **Hard-blocked items protected:** AI cannot propose removal of blocked items
- **Score delta clamped:** ±25 default, ±40 with user-fact confirmation

---

## Execution

### Restore Point

Before any destructive action, DMCA creates a Windows System Restore Point. If the restore point fails, all subsequent actions are cancelled.

### Action Types

| Action | Description |
|--------|-------------|
| Create Restore Point | Always first — safety net |
| Uninstall Driver Package | Removes via `pnputil /delete-driver` |
| Disable Service | Sets service start type to Disabled |
| Uninstall Program | Runs the program's uninstaller |

### Cancellation

You can cancel execution between actions (not mid-action). Remaining actions will be marked as CANCELLED.

### Audit Log

Every action is logged with:
- Start/end timestamps
- Command executed
- Output/error messages
- Status (completed, failed, dry-run, cancelled)

---

## Delta Report & Verification

After execution, use the Delta Report to verify results:

- **Removed:** Items present before but absent after execution
- **Changed:** Items with different properties (e.g., service start type changed)
- **Unchanged:** Items that remain identical
- **Added:** New items that appeared since the initial scan

### Next Steps Guidance

The delta report provides actionable guidance:
- Reboot recommendation after driver removal
- Service verification suggestions
- Stability check reminders

### Export

Export the report as Markdown for documentation or sharing.

---

## Troubleshooting

### "No snapshot found"
Run a scan first from the Inventory page before generating plans or running the AI advisor.

### Scan takes too long
Some collectors may timeout on systems with many devices. If a collector fails, the scan continues with partial results.

### AI Advisor not responding
- Verify your `OPENAI_API_KEY` environment variable is set
- Check your internet connection
- The AI client retries transient errors up to 3 times with exponential backoff

### Restore point creation fails
- Ensure System Protection is enabled on your system drive
- Run DMCA as Administrator
- Check that Volume Shadow Copy service is running

### Driver removal fails
- Some drivers may be in use — reboot and retry
- Check the audit log for specific error messages
- The remaining actions continue even if one fails (except after restore point failure)

### Service disable fails
- Ensure you're running as Administrator
- Some services may be protected by Windows policies

### Application crashes
DMCA has a global error handler that shows user-friendly messages. If you see an unexpected error, note the message and report it.

---

## FAQ

**Q: Will DMCA damage my system?**
A: DMCA creates a system restore point before any destructive action. You can always restore to the pre-cleanup state. The dry-run mode lets you preview all actions safely.

**Q: Can the AI remove things without my approval?**
A: No. The AI can only create proposals. You must explicitly approve each proposal, and then confirm execution with a two-step safety check.

**Q: What if I want to undo changes?**
A: Use Windows System Restore to roll back to the restore point created by DMCA.

**Q: Do I need an OpenAI API key?**
A: No. The AI Advisor is optional. The deterministic rules engine works without any API key.

**Q: What Windows versions are supported?**
A: Windows 10 (version 1903+) and Windows 11.

**Q: Can I run DMCA without admin privileges?**
A: The scan will work with limited results, but execution (driver removal, service changes) requires Administrator privileges.

**Q: How do I know which items are safe to remove?**
A: Items scored 0–25 with a REMOVE_STAGE_1 recommendation are the safest. Always review REMOVE_STAGE_2 and REVIEW items carefully. BLOCKED items are protected and cannot be removed.
