using KaiAssistant.Domain.Entities;
using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.DTOs;

public record AssistantRequestDto(
    [Required, MinLength(1), MaxLength(2000)] string Message,
    ConversationMessage[]? History = null,
    AssistantContextDto? Context = null,
    string? ConversationId = null);