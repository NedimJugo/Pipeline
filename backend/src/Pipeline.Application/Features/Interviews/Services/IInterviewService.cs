using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Interviews.DTOs;

namespace Pipeline.Application.Features.Interviews.Services;

public interface IInterviewService
{
    Task<IReadOnlyList<InterviewListItemDto>> GetInterviewsAsync(InterviewFilterDto? filter = null, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewListItemDto>> GetInterviewsForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task<InterviewDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InterviewDetailDto> CreateInterviewAsync(CreateInterviewRequest request, CancellationToken ct = default);
    Task<InterviewDetailDto> UpdateInterviewAsync(Guid id, UpdateInterviewRequest request, CancellationToken ct = default);
    Task<InterviewDetailDto> UpdateDebriefAsync(Guid id, UpdateDebriefRequest request, CancellationToken ct = default);
    Task<InterviewDetailDto> UpdatePrepChecklistAsync(Guid id, UpdatePrepChecklistRequest request, CancellationToken ct = default);
    Task<InterviewQuestionDto> AddQuestionAsync(Guid id, AddQuestionRequest request, CancellationToken ct = default);
    Task DeleteQuestionAsync(Guid interviewId, Guid questionId, CancellationToken ct = default);
    Task DeleteInterviewAsync(Guid id, CancellationToken ct = default);
    Task<(string FileName, string Content)> GenerateIcsAsync(Guid id, CancellationToken ct = default);
}
