using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for DecisionPlan persistence.
/// </summary>
public interface IPlanRepository
{
    Task CreateAsync(DecisionPlan plan);
    Task<DecisionPlan?> GetByIdAsync(Guid id);
    Task<DecisionPlan?> GetCurrentBySessionIdAsync(Guid sessionId);
    Task UpdatePlanItemAsync(Guid planId, PlanItem item);
}
