using System.Collections.Generic;
using System.Linq;
using cmpctircd.Configuration;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace cmpctircd.Validation {
    public class ConfigurationValidator {
        private readonly IConfiguration _config;
        private readonly IOptions<SocketOptions> _socketOptions;

        public ConfigurationValidator(IConfiguration config, IOptions<SocketOptions> socketOptions) {
            _config = config;
            _socketOptions = socketOptions;
        }

        public ValidationResult ValidateConfiguration() {
            var result = new ValidationResult();

            var loggerValidationResult = ValidateLoggerElement();
            result.Errors.AddRange(loggerValidationResult.Errors);

            var modeValidationResult = ValidateModeElement();
            result.Errors.AddRange(modeValidationResult.Errors);

            var operatorValidationResult = ValidateOperatorElement();
            result.Errors.AddRange(operatorValidationResult.Errors);

            var serverValidationResult = ValidateServerElement();
            result.Errors.AddRange(serverValidationResult.Errors);

            var socketValidationResult = ValidateSocketElement();
            result.Errors.AddRange(socketValidationResult.Errors);

            return result;
        }

        private ValidationResult ValidateLoggerElement() {
            var validationResult = new ValidationResult();
            var loggers = _config.GetSection("Logging:Loggers").Get<List<LoggerElement>>();

            var validator = new LoggerElementValidator();

            foreach (var logger in loggers) {
                var result = validator.Validate(logger);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }

        private ValidationResult ValidateModeElement() {
            var validationResult = new ValidationResult();

            var modes = _config.GetSection("Cmodes").Get<List<ModeElement>>();
            modes.AddRange(_config.GetSection("Umodes").Get<List<ModeElement>>());

            var validator = new ModeElementValidator();

            foreach (var mode in modes) {
                var result = validator.Validate(mode);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }

        private ValidationResult ValidateOperatorElement() {
            var validationResult = new ValidationResult();

            var opers = _config.GetSection("Opers").Get<List<OperatorElement>>();


            var validator = new OperatorElementValidator();

            foreach (var oper in opers) {
                var result = validator.Validate(oper);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }

        private ValidationResult ValidateServerElement() {
            var validationResult = new ValidationResult();

            var servers = _config.GetSection("Servers").Get<List<ServerElement>>();


            var validator = new ServerElementValidator();

            foreach (var server in servers) {
                var result = validator.Validate(server);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }

        private ValidationResult ValidateSocketElement() {
            var validationResult = new ValidationResult();

            var sockets = _socketOptions.Value;

            if (!sockets.Sockets.Any()) {
                validationResult.Errors.Add(new ValidationFailure("Socket", "No socket configuration found"));
            }


            var validator = new SocketElementValidator();

            foreach (var socket in sockets.Sockets) {
                var result = validator.Validate(socket);
                validationResult.Errors.AddRange(result.Errors);
            }

            return validationResult;
        }
    }
}