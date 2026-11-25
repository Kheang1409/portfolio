using FluentValidation;
namespace KaiAssistant.Application.Contacts.Commands;

public class ContactCommandValidator : AbstractValidator<ContactCommand>
{
    public ContactCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be valid.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(120).WithMessage("Name cannot exceed 120 characters.");
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MinimumLength(2).WithMessage("Message at least 2 characters!")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
    }
}