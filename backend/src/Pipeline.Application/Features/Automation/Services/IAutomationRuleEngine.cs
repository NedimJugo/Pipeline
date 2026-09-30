using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pipeline.Application.Features.Automation.Services;

public interface IAutomationRuleEngine
{
    Task<int> EvaluateRulesForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> EvaluateAllActiveUsersAsync(CancellationToken ct = default);
}
