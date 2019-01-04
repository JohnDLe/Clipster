using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Clipster.Validators
{
    public class OutputFileValidator : IOptionValidator
    {
        private readonly string _source;
        public OutputFileValidator(string source)
        {
            _source = source;
        }

        public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
        {
            // This validator only runs if there is a value
            if (!option.HasValue()) return ValidationResult.Success;
            var sourcePath = Path.GetFullPath(_source);
            var outputPath = Path.GetFullPath(option.Value());


            if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult($"The value for --{option.LongName} must be match the value of --source");
            }

            return ValidationResult.Success;
        }
    }
}
