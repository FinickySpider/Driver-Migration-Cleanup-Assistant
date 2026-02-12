---
id: FEAT-029
type: feature
status: planned
priority: medium
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-028]
---

# FEAT-029: Item Detail Pane

## Description

Implement the detail pane that shows comprehensive information for a selected inventory item, including all metadata, scoring breakdown, evidence, AI rationale, and hard-block reasons.

## Acceptance Criteria

- [ ] Pane shows: display name, item type, vendor, version, signature info, present/running state
- [ ] Scoring breakdown: baseline score components (which signals matched), AI delta, final score
- [ ] Engine rationale displayed as bullet list
- [ ] AI rationale displayed (if any proposals modified this item)
- [ ] Hard blocks listed with code and message
- [ ] Hardware IDs, paths, dependencies shown if available

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/ItemDetailPane.xaml` | Detail layout |
| `src/Dmca.App/ViewModels/ItemDetailViewModel.cs` | Detail logic |

## Testing

- [ ] All fields populated correctly
- [ ] Hard-blocked items show block reasons prominently

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
