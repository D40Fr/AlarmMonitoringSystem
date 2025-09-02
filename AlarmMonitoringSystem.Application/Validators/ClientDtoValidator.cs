using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using FluentValidation;

namespace AlarmMonitoringSystem.Application.Validators
{
    public class ClientDtoValidator : AbstractValidator<ClientDto>
    {
        public ClientDtoValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("Client ID is required.")
                .Length(1, 100)
                .WithMessage("Client ID must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z0-9_\-\.]+$")
                .WithMessage("Client ID can only contain letters, numbers, underscores, hyphens, and dots.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Client name is required.")
                .Length(1, 200)
                .WithMessage("Client name must be between 1 and 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.IpAddress)
                .NotEmpty()
                .WithMessage("IP address is required.")
                .Must(BeAValidIpAddress)
                .WithMessage("Please provide a valid IP address.");

            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535)
                .WithMessage("Port must be between 1 and 65535.");
        }

        private static bool BeAValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

    }

}

