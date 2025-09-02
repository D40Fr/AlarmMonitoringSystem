using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Application.DTOs
{
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public static ApiResponseDto<T> SuccessResult(T data, string message = "Operation successful")
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }
        public static ApiResponseDto<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>()
            };
        }
    }

    public class ApiResponseDto : ApiResponseDto<object>
    {
        public static ApiResponseDto SuccessResult(string message = "Operation successful")
        {
            return new ApiResponseDto
            {
                Success = true,
                Message = message,
                Data = null,
                Errors = new List<string>()
            };
        }

        public new static ApiResponseDto ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = message,
                Data = null,
                Errors = errors ?? new List<string>()
            };
        }
    }
}