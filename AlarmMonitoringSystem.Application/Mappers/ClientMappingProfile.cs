using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Entities;
using AutoMapper;

namespace AlarmMonitoringSystem.Application.Mappers
{
    public class ClientMappingProfile : Profile
    {
        public ClientMappingProfile()
        {
            // Client Entity ↔ ClientDto
            CreateMap<Client, ClientDto>()
                .ForMember(dest => dest.ActiveAlarmCount, opt => opt.MapFrom(src =>
                    src.Alarms.Count(a => a.IsActive && !a.IsAcknowledged)))
                .ForMember(dest => dest.LastAlarmTime, opt => opt.MapFrom(src =>
                    src.Alarms.Where(a => a.IsActive).OrderByDescending(a => a.AlarmTime).FirstOrDefault().AlarmTime));

            CreateMap<ClientDto, Client>()
                .ForMember(dest => dest.Alarms, opt => opt.Ignore())
                .ForMember(dest => dest.ConnectionLogs, opt => opt.Ignore());

            // Value Object ↔ DTO mappings
            CreateMap<Domain.ValueObjects.ClientInfo, ClientDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ActiveAlarmCount, opt => opt.Ignore())
                .ForMember(dest => dest.LastAlarmTime, opt => opt.Ignore());

            CreateMap<ClientDto, Domain.ValueObjects.ClientInfo>()
                .ConvertUsing<ClientInfoConverter>();
        }
    }

    // Custom converter for ClientInfo creation
    public class ClientInfoConverter : ITypeConverter<ClientDto, Domain.ValueObjects.ClientInfo>
    {
        public Domain.ValueObjects.ClientInfo Convert(ClientDto source, Domain.ValueObjects.ClientInfo destination, ResolutionContext context)
        {
            return Domain.ValueObjects.ClientInfo.Create(
                source.ClientId,
                source.Name,
                source.IpAddress,
                source.Port,
                source.Description,
                source.Status,
                source.IsActive);
        }
    }
}