using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AlarmMonitoringSystem.Application.Mappers
{
    public static class MappingExtensions
    {
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            // Register AutoMapper with all profiles from current assembly
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            return services;
        }

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