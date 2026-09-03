using Application.Tasks.Dtos;
using FluentValidation;

namespace Application.Tasks.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(1, 200);

        RuleFor(x => x.Description)
            .MaximumLength(8000);

        RuleFor(x => x.Priority)
            .Must(p => p == "Low" || p == "Medium" || p == "High" || p == "Critical")
            .WithMessage("Priority must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.DueDate)
            .Must(d => d is null || d.Value > DateTimeOffset.MinValue)
            .WithMessage("DueDate must be a valid ISO-8601 timestamp if provided.");
    }
}
