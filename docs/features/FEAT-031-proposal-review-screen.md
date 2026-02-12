---
id: FEAT-031
type: feature
status: complete
priority: high
phase: PHASE-03
sprint: SPRINT-06
owner: ""
depends_on: [FEAT-026, FEAT-018]
---

# FEAT-031: Proposal Review Diff Screen

## Description

Implement the proposal review screen where users can see pending proposals, review the diff of changes, and approve or reject each proposal.

## Acceptance Criteria

- [ ] List of pending proposals with title, risk level, and change count
- [ ] Diff view for selected proposal showing: change type, target item, delta/value, reason
- [ ] Evidence references displayed per change
- [ ] Approve and Reject buttons per proposal
- [ ] Approved proposals immediately trigger plan merge
- [ ] Confirmation dialog for approval
- [ ] Empty state when no proposals pending

## Files Touched

| File | Change |
|------|--------|
| `src/Dmca.App/Views/ProposalReviewScreen.xaml` | Review UI |
| `src/Dmca.App/ViewModels/ProposalReviewViewModel.cs` | Review logic |

## Testing

- [ ] Proposals listed correctly
- [ ] Approve/reject updates status
- [ ] Plan updated after approval

## Done When

- [ ] Acceptance criteria met
- [ ] Verified manually
