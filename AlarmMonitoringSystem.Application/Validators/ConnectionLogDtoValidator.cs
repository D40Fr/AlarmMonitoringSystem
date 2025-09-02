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
    public class ConnectionLogDtoValidator : AbstractValidator<ConnectionLogDto>
    {
        public ConnectionLogDtoValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("Client ID is required.");

            RuleFor(x => x.ClientName)
                .NotEmpty()
                .WithMessage("Client name is required.")
                .MaximumLength(200)
                .WithMessage("Client name cannot exceed 200 characters.");


            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid connection status.");

            RuleFor(x => x.LogLevel)
                .IsInEnum()
                .WithMessage("Invalid log level.");

            RuleFor(x => x.Message)
                .MaximumLength(500)
                .WithMessage("Message cannot exceed 500 characters.");

            RuleFor(x => x.Details)
                .MaximumLength(1000)
                .WithMessage("Details cannot exceed 1000 characters.");

            RuleFor(x => x.IpAddress)
                .Must(BeAValidIpAddress)
                .When(x => !string.IsNullOrEmpty(x.IpAddress))
                .WithMessage("IP address must be valid when provided.");

            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535)
                .When(x => x.Port.HasValue)
                .WithMessage("Port must be between 1 and 65535 when provided.");
        }

        private static bool BeAValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }
    }
}