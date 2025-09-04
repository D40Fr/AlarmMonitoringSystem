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
    public class ConnectionLogMappingProfile : Profile
    {
        public ConnectionLogMappingProfile()
        {
            // ConnectionLog Entity ↔ ConnectionLogDto
            CreateMap<ConnectionLog, ConnectionLogDto>()
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.Name))
                .ForMember(dest => dest.ClientIdentifier, opt => opt.MapFrom(src => src.Client.ClientId));

            CreateMap<ConnectionLogDto, ConnectionLog>()
                .ForMember(dest => dest.Client, opt => opt.Ignore());

            // Value Object ↔ DTO mappings
            CreateMap<Domain.ValueObjects.ConnectionEvent, ConnectionLogDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ClientName, opt => opt.Ignore())
                .ForMember(dest => dest.ClientIdentifier, opt => opt.Ignore())
                .ForMember(dest => dest.LogTime, opt => opt.MapFrom(src => src.EventTime))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.Port, opt => opt.MapFrom(src => src.Port))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.EventTime));

            CreateMap<ConnectionLogDto, Domain.ValueObjects.ConnectionEvent>()
                .ConstructUsing(src => Domain.ValueObjects.ConnectionEvent.Create(
                    src.ClientId,
                    src.Status,
                    src.Message,
                    src.IpAddress,
                    src.Port,
                    src.Details));
        }
    }
}
