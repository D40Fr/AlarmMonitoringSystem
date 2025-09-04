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
    public class AlarmMappingProfile : Profile
    {
        public AlarmMappingProfile()
        {
            // Alarm Entity ↔ AlarmDto
            CreateMap<Alarm, AlarmDto>()
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.Name))
                .ForMember(dest => dest.ClientIdentifier, opt => opt.MapFrom(src => src.Client.ClientId));
            CreateMap<AlarmDto, Alarm>()
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.RawData, opt => opt.Ignore());

            // IncomingAlarmDto → Alarm Entity (Critical for TCP processing)
            CreateMap<IncomingAlarmDto, Alarm>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ClientId, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.GetAlarmType()))
                .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.GetAlarmSeverity()))
                .ForMember(dest => dest.AlarmTime, opt => opt.MapFrom(src => src.Timestamp ?? DateTime.UtcNow))
                .ForMember(dest => dest.NumericValue, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.IsAcknowledged, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AcknowledgedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.RawData, opt => opt.MapFrom<JsonSerializationResolver>());

            // Value Object ↔ DTO mappings
            CreateMap<Domain.ValueObjects.AlarmData, AlarmDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ClientId, opt => opt.Ignore())
                .ForMember(dest => dest.ClientName, opt => opt.Ignore())
                .ForMember(dest => dest.ClientIdentifier, opt => opt.Ignore())
                .ForMember(dest => dest.IsAcknowledged, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AcknowledgedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AcknowledgedBy, opt => opt.Ignore());

            CreateMap<AlarmDto, Domain.ValueObjects.AlarmData>()
                .ConvertUsing<AlarmDataConverter>();
        }
    }
    // Custom resolver for JSON serialization
    public class JsonSerializationResolver : IValueResolver<IncomingAlarmDto, Alarm, string>
    {
        public string Resolve(IncomingAlarmDto source, Alarm destination, string destMember, ResolutionContext context)
        {
            return System.Text.Json.JsonSerializer.Serialize(source);
        }
    }

    // Custom converter for AlarmData creation
    public class AlarmDataConverter : ITypeConverter<AlarmDto, Domain.ValueObjects.AlarmData>
    {
        public Domain.ValueObjects.AlarmData Convert(AlarmDto source, Domain.ValueObjects.AlarmData destination, ResolutionContext context)
        {

            return Domain.ValueObjects.AlarmData.Create(
                source.AlarmId,
                source.Title,
                source.Message,
                source.Type,
                source.Severity,
                source.AlarmTime,
                source.Zone,
                source.NumericValue,
                source.Unit,
                null); // AdditionalData set to null
        }
    }
}