// AlarmMonitoringSystem.Application/Mappers/MappingExtensions.cs
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmMonitoringSystem.Application.Mappers
{
    public static class MappingExtensions
    {
        // ✅ REMOVED: Deprecated AddAutoMapperProfiles method
        // AutoMapper 15.x doesn't use this pattern anymore

        // Extension methods for common mapping scenarios
        public static TDestination MapTo<TDestination>(this object source, IMapper mapper)
        {
            return mapper.Map<TDestination>(source);
        }

        public static TDestination MapTo<TSource, TDestination>(this TSource source, IMapper mapper)
        {
            return mapper.Map<TSource, TDestination>(source);
        }

        public static IEnumerable<TDestination> MapToList<TDestination>(this IEnumerable<object> source, IMapper mapper)
        {
            return mapper.Map<IEnumerable<TDestination>>(source);
        }

        public static List<TDestination> MapToList<TSource, TDestination>(this IEnumerable<TSource> source, IMapper mapper)
        {
            return mapper.Map<List<TDestination>>(source);
        }
    }
}