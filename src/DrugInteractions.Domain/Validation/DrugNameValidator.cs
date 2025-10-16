using DrugInteractions.Domain.Models;
using FluentValidation;

namespace DrugInteractions.Domain.Validation;

public class DrugNameValidator : AbstractValidator<string>
{
    public DrugNameValidator()
    {
        RuleFor(name => name)
            .NotEmpty()
            .Length(3, 60)
            .Matches("^[a-zA-Z- ]+$")
            .WithMessage("Drug name must be 3-60 characters and contain only letters, spaces, and hyphens");
    }
}