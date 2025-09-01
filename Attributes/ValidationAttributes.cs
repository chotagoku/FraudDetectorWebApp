using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FraudDetectorWebApp.Attributes
{
    /// <summary>
    /// Validates that a string is valid JSON
    /// </summary>
    public class ValidJsonAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Allow null/empty if not required

            try
            {
                JsonDocument.Parse(value.ToString()!);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must contain valid JSON.";
        }
    }

    /// <summary>
    /// Validates that a URL is properly formatted and accessible
    /// </summary>
    public class ValidUrlAttribute : ValidationAttribute
    {
        public bool CheckAccessibility { get; set; } = false;

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            var urlString = value.ToString()!;

            if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must contain a valid HTTP or HTTPS URL.";
        }
    }

    /// <summary>
    /// Validates that a password meets complexity requirements
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Allow null/empty if not required

            var password = value.ToString()!;

            if (password.Length < MinLength)
                return false;

            if (RequireUppercase && !password.Any(char.IsUpper))
                return false;

            if (RequireLowercase && !password.Any(char.IsLower))
                return false;

            if (RequireDigit && !password.Any(char.IsDigit))
                return false;

            if (RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            var requirements = new List<string>();
            
            requirements.Add($"at least {MinLength} characters");
            if (RequireUppercase) requirements.Add("an uppercase letter");
            if (RequireLowercase) requirements.Add("a lowercase letter");
            if (RequireDigit) requirements.Add("a digit");
            if (RequireSpecialCharacter) requirements.Add("a special character");

            return $"The {name} field must contain {string.Join(", ", requirements)}.";
        }
    }

    /// <summary>
    /// Validates that a value is within a specified numeric range with custom bounds
    /// </summary>
    public class NumericRangeAttribute : ValidationAttribute
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public bool IncludeMinimum { get; set; } = true;
        public bool IncludeMaximum { get; set; } = true;

        public NumericRangeAttribute(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            if (!double.TryParse(value.ToString(), out double numericValue))
                return false;

            if (IncludeMinimum && numericValue < Minimum)
                return false;

            if (!IncludeMinimum && numericValue <= Minimum)
                return false;

            if (IncludeMaximum && numericValue > Maximum)
                return false;

            if (!IncludeMaximum && numericValue >= Maximum)
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            var minOperator = IncludeMinimum ? ">=" : ">";
            var maxOperator = IncludeMaximum ? "<=" : "<";
            
            return $"The {name} field must be {minOperator} {Minimum} and {maxOperator} {Maximum}.";
        }
    }

    /// <summary>
    /// Validates file extensions
    /// </summary>
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        public string[] Extensions { get; set; }

        public AllowedExtensionsAttribute(params string[] extensions)
        {
            Extensions = extensions.Select(ext => ext.StartsWith('.') ? ext : $".{ext}").ToArray();
        }

        public override bool IsValid(object? value)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName);
                return Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            }

            if (value is string fileName && !string.IsNullOrWhiteSpace(fileName))
            {
                var extension = Path.GetExtension(fileName);
                return Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            }

            return true; // Allow null if not required
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must have one of the following extensions: {string.Join(", ", Extensions)}.";
        }
    }

    /// <summary>
    /// Validates file size
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        public long MaxSizeInBytes { get; set; }

        public MaxFileSizeAttribute(long maxSizeInBytes)
        {
            MaxSizeInBytes = maxSizeInBytes;
        }

        public override bool IsValid(object? value)
        {
            if (value is IFormFile file)
            {
                return file.Length <= MaxSizeInBytes;
            }

            return true; // Allow null if not required
        }

        public override string FormatErrorMessage(string name)
        {
            var maxSizeMB = MaxSizeInBytes / (1024.0 * 1024.0);
            return $"The {name} field must be smaller than {maxSizeMB:F2} MB.";
        }
    }

    /// <summary>
    /// Validates CNIC format (Pakistani National Identity Card)
    /// </summary>
    public class ValidCnicAttribute : ValidationAttribute
    {
        private static readonly Regex CnicRegex = new(@"^\d{5}-\d{7}-\d{1}$", RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            return CnicRegex.IsMatch(value.ToString()!);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be in the format XXXXX-XXXXXXX-X.";
        }
    }

    /// <summary>
    /// Validates phone number format (flexible for international formats)
    /// </summary>
    public class ValidPhoneNumberAttribute : ValidationAttribute
    {
        private static readonly Regex PhoneRegex = new(@"^[\+]?[1-9][\d]{0,15}$", RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            var phone = value.ToString()!.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            return PhoneRegex.IsMatch(phone);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must contain a valid phone number.";
        }
    }

    /// <summary>
    /// Validates that a date is not in the future
    /// </summary>
    public class NotInFutureAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
            {
                return dateValue <= DateTime.UtcNow;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field cannot be a future date.";
        }
    }

    /// <summary>
    /// Validates that a collection has items within a specified range
    /// </summary>
    public class CollectionCountAttribute : ValidationAttribute
    {
        public int MinCount { get; set; } = 0;
        public int MaxCount { get; set; } = int.MaxValue;

        public override bool IsValid(object? value)
        {
            if (value == null)
                return MinCount == 0;

            if (value is System.Collections.ICollection collection)
            {
                return collection.Count >= MinCount && collection.Count <= MaxCount;
            }

            if (value is System.Collections.IEnumerable enumerable)
            {
                var count = enumerable.Cast<object>().Count();
                return count >= MinCount && count <= MaxCount;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            if (MaxCount == int.MaxValue)
                return $"The {name} field must contain at least {MinCount} item(s).";
            
            if (MinCount == 0)
                return $"The {name} field must contain at most {MaxCount} item(s).";

            return $"The {name} field must contain between {MinCount} and {MaxCount} item(s).";
        }
    }
}
