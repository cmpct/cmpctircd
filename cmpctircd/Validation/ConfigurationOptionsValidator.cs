using cmpctircd.Configuration.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace cmpctircd.Validation {
    public class ConfigurationOptionsValidator : AbstractValidator<IOptions<ConfigurationOptions>> {
        public ConfigurationOptionsValidator() {
            RuleFor(c => c.Value.Host).NotEmpty();
            RuleFor(c => c.Value.Sid).NotEmpty();
            RuleFor(c => c.Value.OperChan).NotEmpty();
            RuleFor(c => c.Value.Advanced).SetValidator(new AdvancedOptionsValidator());
            RuleForEach(c => c.Value.Opers).NotEmpty().SetValidator(new OperatorElementValidator());
            RuleForEach(c => c.Value.Sockets).NotEmpty().SetValidator(new SocketElementValidator());
            RuleForEach(c => c.Value.Loggers).SetValidator(new LoggerElementValidator());
            RuleForEach(c => c.Value.UModes).SetValidator(new ModeElementValidator());
            RuleForEach(c => c.Value.CModes).SetValidator(new ModeElementValidator());
            RuleForEach(c => c.Value.Servers).NotEmpty().SetValidator(new ServerElementValidator());
            RuleFor(c => c.Value.Tls).SetValidator(new TlsOptionsValidator());
        }
    }

    public class TlsOptionsValidator : AbstractValidator<TlsOptions> {
        public TlsOptionsValidator() {
            RuleFor(t => t.File).NotEmpty();
        }
    }

    public class AdvancedOptionsValidator : AbstractValidator<AdvancedOptions> {
        public AdvancedOptionsValidator() {
            RuleFor(a => a.MaxTargets).NotEmpty();
            RuleFor(a => a.PingTimeout).NotEmpty();
            RuleFor(a => a.RequirePongCookie).NotNull();
            RuleFor(a => a.ResolveHostnames).NotNull();
        }
    }
}