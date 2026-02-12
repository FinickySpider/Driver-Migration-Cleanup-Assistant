---
id: ADR-0001
type: decision
status: complete
date: 2026-02-12
supersedes: ""
superseded_by: ""
---

# ADR-0001: UI Framework — WPF vs WinUI 3

## Context

DMCA is a Windows-only desktop application targeting Windows 10/11. We need a UI framework for the desktop shell, inventory table, AI chat pane, proposal review, and execution screens. The two main contenders are WPF (.NET 8) and WinUI 3 (Windows App SDK).

WPF is mature, well-documented, has extensive third-party control libraries, and is proven for complex data-driven desktop applications. WinUI 3 is Microsoft's modern successor with Fluent Design, but has less community support, fewer controls, and some rough edges in packaging and deployment.

## Decision

Use **WPF on .NET 8** for the v1 desktop UI.

Rationale:
- WPF is mature and stable for complex data grids and panel layouts
- Superior DataGrid control for inventory table (sorting, filtering, virtualization)
- Well-understood MVVM ecosystem (CommunityToolkit.Mvvm)
- Simpler deployment without MSIX packaging requirements
- Larger pool of developer knowledge and community resources
- WinUI 3 can be reconsidered for v2 when it matures further

## Consequences

### Positive

- Faster development with proven patterns
- Rich DataGrid for inventory display
- No MSIX packaging complexity
- Extensive documentation and Stack Overflow coverage

### Negative

- Older visual style (mitigated with ModernWpf or custom themes)
- No native Fluent Design (can approximate)
- WPF is in maintenance mode (no major new features)

## Links

- Related items:
  - FEAT-026
  - FEAT-028
