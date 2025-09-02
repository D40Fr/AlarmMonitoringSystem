using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using FluentValidation;

namespace AlarmMonitoringSystem.Application.Validators
{
    public class IncomingAlarmDtoValidator : AbstractValidator<IncomingAlarmDto>
    {
        public IncomingAlarmDtoValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("alarmId is required in JSON payload.")
                .Length(1, 100)
                .WithMessage("alarmId must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z0-9_\-\.]+$")
                .WithMessage("alarmId can only contain letters, numbers, underscores, hyphens, and dots.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("title is required in JSON payload.")
                .Length(1, 200)
                .WithMessage("title must be between 1 and 200 characters.");

            RuleFor(x => x.Message)
                .MaximumLength(1000)
                .WithMessage("message cannot exceed 1000 characters.");

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("type is required in JSON payload.")
                .Must(BeValidAlarmType)
                .WithMessage("type must be one of: temperature, pressure, voltage, current, motion, door, system, network, security, other.");

            RuleFor(x => x.Severity)
                .NotEmpty()
                .WithMessage("severity is required in JSON payload.")
                .Must(BeValidAlarmSeverity)
                .WithMessage("severity must be one of: low, medium, high, critical.");

            RuleFor(x => x.Zone)
                .MaximumLength(50)
                .WithMessage("zone cannot exceed 50 characters.");


            RuleFor(x => x.Unit)
                .MaximumLength(20)
                .WithMessage("unit cannot exceed 20 characters.");

            RuleFor(x => x.Value)
                .InclusiveBetween(-999999999, 999999999)
                .When(x => x.Value.HasValue)
                .WithMessage("value must be within reasonable range.");

            RuleFor(x => x.Timestamp)
                .GreaterThan(DateTime.UtcNow.AddYears(-1))
                .LessThan(DateTime.UtcNow.AddMinutes(5))
                .When(x => x.Timestamp.HasValue)
                .WithMessage("timestamp must be within the last year and not more than 5 minutes in the future.");
        }

        private static bool BeValidAlarmType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            var validTypes = new[] { "temperature", "pressure", "voltage", "current", "motion", "door", "system", "network", "security", "other" };
            return validTypes.Contains(type.ToLowerInvariant());
        }

        private static bool BeValidAlarmSeverity(string severity)
        {
            if (string.IsNullOrWhiteSpace(severity))
                return false;

            var validSeverities = new[] { "low", "medium", "high", "critical" };
            return validSeverities.Contains(severity.ToLowerInvariant());
        }

    }
}

