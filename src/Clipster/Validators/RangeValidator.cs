using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Clipster.Validators
{
    public class RangeValidator : IOptionValidator
    {
        private readonly int _lower;
        private readonly int _upper;
        public RangeValidator(int lower, int upper)
        {
            _lower = lower;
            _upper = upper;
        }

        public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
        {
            // This validator only runs if there is a value
            if (!option.HasValue()) return ValidationResult.Success;
            var isNumeric = int.TryParse(option.Value(), out int num);

            if (!(isNumeric && Enumerable.Range(_lower, _upper).Contains(num)))
            {
                return new ValidationResult($"The value for --{option.LongName} must a number between {_lower} and {_upper}");
            }

            return ValidationResult.Success;
        }
    }
}
