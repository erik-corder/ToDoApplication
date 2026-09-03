using Application.Tasks.Dtos;
using FluentValidation;

namespace Application.Tasks.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(1, 200);

        RuleFor(x => x.Description)
            .MaximumLength(8000);

        RuleFor(x => x.Priority)
            .Must(p => p == "Low" || p == "Medium" || p == "High" || p == "Critical")
            .WithMessage("Priority must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.Status)
            .Must(s => s == "Open" || s == "InProgress" || s == "Done" || s == "Blocked")
            .WithMessage("Status must be one of: Open, InProgress, Done, Blocked.");
    }
}
