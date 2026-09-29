using System.Collections.Generic;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Interviews.Templates;

public static class DefaultPrepChecklists
{
    public static List<PrepChecklistItemDto> GetDefaultChecklist(InterviewType type)
    {
        return type switch
        {
            InterviewType.HR => new List<PrepChecklistItemDto>
            {
                new("Research company history, mission, recent news, and leadership"),
                new("Review job description requirements and match with past experience"),
                new("Prepare concise 2-minute elevator pitch ('Tell me about yourself')"),
                new("Prepare clear compensation range expectation answer"),
                new("Prepare 3-5 thoughtful questions on company culture and hiring timeline"),
                new("Test microphone, webcam, and stable internet connection")
            },

            InterviewType.Technical => new List<PrepChecklistItemDto>
            {
                new("Review core data structures, algorithms, and system architecture patterns"),
                new("Prepare 2 deep-dive examples of challenging technical problems solved"),
                new("Review company's tech stack, open source work, or engineering blog"),
                new("Prepare thoughtful technical questions for the engineering team"),
                new("Test code editor, local development environment, and screen sharing")
            },

            InterviewType.Culture => new List<PrepChecklistItemDto>
            {
                new("Review company core values and principles in detail"),
                new("Prepare 3 STAR/SAR stories highlighting cross-functional collaboration"),
                new("Prepare 2 stories demonstrating ownership, handling ambiguity, and overcoming mistakes"),
                new("Prepare questions about team rituals, communication styles, and feedback culture")
            },

            InterviewType.Manager => new List<PrepChecklistItemDto>
            {
                new("Prepare examples of project delivery, timeline management, and stakeholder trade-offs"),
                new("Prepare stories on mentorship, peer coaching, and team alignment"),
                new("Formulate questions about the manager's leadership style, team goals, and first 90 days expectations")
            },

            InterviewType.Final => new List<PrepChecklistItemDto>
            {
                new("Review executive leadership background, market positioning, and competitors"),
                new("Prepare strategic alignment story on how your skills drive long-term business impact"),
                new("Formulate 3 high-level vision and organizational growth questions")
            },

            InterviewType.Assignment => new List<PrepChecklistItemDto>
            {
                new("Thoroughly review take-home prompt and requirements rubric"),
                new("Prepare slide deck or clean repository walkthrough"),
                new("Document architecture trade-offs, assumptions, and edge cases made during development"),
                new("Outline what you would build or improve given more time and production scale")
            },

            _ => new List<PrepChecklistItemDto>
            {
                new("Research company and interviewers on LinkedIn"),
                new("Review key role responsibilities and your relevant accomplishments"),
                new("Prepare questions about next steps in the evaluation process"),
                new("Test audio, video, and environment setup")
            }
        };
    }
}
