using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmpctircd.Configuration;
using FluentValidation;

namespace cmpctircd.Validation
{
    public class SocketElementValidator : AbstractValidator<SocketElement>
    {
        public SocketElementValidator() {
            RuleFor(s => s.Tls).NotEmpty();
            RuleFor(s => s.Host).NotEmpty();
            RuleFor(s => s.Port).NotEmpty();
            RuleFor(s => s.Type).NotEmpty();
        }
    }
}
