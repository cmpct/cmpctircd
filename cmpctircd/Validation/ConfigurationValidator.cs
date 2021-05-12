using System.Collections.Generic;
using System.Linq;
using cmpctircd.Configuration;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;

namespace cmpctircd.Validation {
    public class ConfigurationValidator {
        private readonly IConfiguration _config;

        public ConfigurationValidator(IConfiguration config) {
            _config = config;
        }

        public ValidationResult ValidateConfiguration() {
            var result = new ValidationResult();

            var loggerValidationResult = ValidateLoggerElement();
            result.Errors.AddRange(loggerValidationResult.Errors);

            return result;
        }

        private ValidationResult ValidateLoggerElement() {
            var validationResult = new ValidationResult();
            var loggers = _config.GetSection("Logging:Loggers").Get<List<LoggerElement>>();

            if (!loggers.Any()) {
                validationResult.Errors.Add(new ValidationFailure("Log Section",
                    "There were no loggers found in the configuration"));
            }

            var validator = new LoggerElementValidator();

            foreach (var logger in loggers) {
                var result = validator.Validate(logger);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }
    }
}