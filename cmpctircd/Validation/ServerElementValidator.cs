using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmpctircd.Configuration;
using FluentValidation;

namespace cmpctircd.Validation
{
    public class ServerElementValidator : AbstractValidator<ServerElement>
    {
        public ServerElementValidator() {
            RuleFor(s => s.Host).NotEmpty();
            RuleFor(s => s.Type).NotEmpty();
            RuleFor(s => s.Masks).NotEmpty();
            RuleFor(s => s.Port).NotEmpty();
            RuleFor(s => s.Password).NotEmpty();
        }
    }
}
