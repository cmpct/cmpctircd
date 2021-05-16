using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmpctircd.Configuration;
using FluentValidation;

namespace cmpctircd.Validation
{
    public class ModeElementValidator : AbstractValidator<ModeElement>
    {
        public ModeElementValidator() {
            RuleFor(m => m.Name).NotEmpty();
        }
    }
}
