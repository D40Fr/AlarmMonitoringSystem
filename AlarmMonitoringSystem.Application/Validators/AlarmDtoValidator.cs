using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Enums;
using FluentValidation;

namespace AlarmMonitoringSystem.Application.Validators
{
    public class AlarmDtoValidator : AbstractValidator<AlarmDto>
    {
        public AlarmDtoValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("Alarm ID is required.")
                .Length(1, 100)
                .WithMessage("Alarm ID must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z0-9_\-\.]+$")
                .WithMessage("Alarm ID can only contain letters, numbers, underscores, hyphens, and dots.");

            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("Client ID is required.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Alarm title is required.")
                .Length(1, 200)
                .WithMessage("Alarm title must be between 1 and 200 characters.");

            RuleFor(x => x.Message)
                .MaximumLength(1000)
                .WithMessage("Alarm message cannot exceed 1000 characters.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid alarm type.");

            RuleFor(x => x.Severity)
                .IsInEnum()
                .WithMessage("Invalid alarm severity.");

            RuleFor(x => x.Zone)
                .MaximumLength(50)
                .WithMessage("Zone cannot exceed 50 characters.");

            RuleFor(x => x.Unit)
                .MaximumLength(20)
                .WithMessage("Unit cannot exceed 20 characters.");

            RuleFor(x => x.NumericValue)
                .InclusiveBetween(-999999999, 999999999)
                .When(x => x.NumericValue.HasValue)
                .WithMessage("Numeric value must be within reasonable range.");
        }
    }
}