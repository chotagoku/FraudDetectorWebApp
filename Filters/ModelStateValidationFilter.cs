using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Filters
{
    public class ModelStateValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var result = new BadRequestObjectResult(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors,
                    StatusCode = 400,
                    Timestamp = DateTime.UtcNow
                });

                context.Result = result;
            }

            base.OnActionExecuting(context);
        }
    }

    public class ValidationErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]> Errors { get; set; } = new();
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Custom validation result for complex validation scenarios
    /// </summary>
    public class CustomValidationResult : ValidationResult
    {
        public string PropertyName { get; }
        public object? AttemptedValue { get; }

        public CustomValidationResult(string errorMessage, string propertyName, object? attemptedValue) 
            : base(errorMessage, new[] { propertyName })
        {
            PropertyName = propertyName;
            AttemptedValue = attemptedValue;
        }
    }

    /// <summary>
    /// Validation context helper for complex validation scenarios
    /// </summary>
    public static class ValidationHelper
    {
        public static List<ValidationResult> ValidateObject(object obj, ValidationContext? context = null)
        {
            context ??= new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(obj, context, results, true);
            return results;
        }

        public static Dictionary<string, string[]> GetValidationErrors(List<ValidationResult> validationResults)
        {
            return validationResults
                .GroupBy(vr => vr.MemberNames.FirstOrDefault() ?? "")
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray()
                );
        }

        public static bool TryValidateObject(object obj, out Dictionary<string, string[]> errors)
        {
            var validationResults = ValidateObject(obj);
            errors = GetValidationErrors(validationResults);
            return !validationResults.Any();
        }
    }

    /// <summary>
    /// Attribute to enable automatic model validation on controller actions
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public bool IncludeRequestId { get; set; } = true;
        public bool ReturnDetailedErrors { get; set; } = true;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var response = new
                {
                    message = "One or more validation errors occurred.",
                    statusCode = 400,
                    timestamp = DateTime.UtcNow,
                    requestId = IncludeRequestId ? context.HttpContext.TraceIdentifier : null,
                    errors = ReturnDetailedErrors ? GetFormattedErrors(context.ModelState) : null
                };

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            base.OnActionExecuting(context);
        }

        private static Dictionary<string, object> GetFormattedErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            var errors = new Dictionary<string, object>();

            foreach (var kvp in modelState)
            {
                var key = kvp.Key;
                var modelErrors = kvp.Value?.Errors;

                if (modelErrors != null && modelErrors.Count > 0)
                {
                    var errorMessages = modelErrors.Select(e => 
                        !string.IsNullOrEmpty(e.ErrorMessage) 
                            ? e.ErrorMessage 
                            : "Invalid value").ToList();

                    errors[key] = errorMessages.Count == 1 ? errorMessages[0] : errorMessages;
                }
            }

            return errors;
        }
    }

}
