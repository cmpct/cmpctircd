using cmpctircd.Configuration;
using FluentValidation;

namespace cmpctircd.Validation {
    public class LoggerElementValidator : AbstractValidator<LoggerElement> {
        public LoggerElementValidator() {
            RuleFor(logger => logger.Type).NotEmpty();
            RuleFor(logger => logger.Level).NotEmpty();
        }
    }
}