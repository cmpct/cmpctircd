using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmpctircd.Configuration;
using FluentValidation;

namespace cmpctircd.Validation
{
    public class OperatorElementValidator : AbstractValidator<OperatorElement>
    {
        public OperatorElementValidator() {
            RuleFor(o => o.Algorithm).NotEmpty();
            RuleFor(o => o.Name).NotEmpty();
            RuleFor(o => o.Password).NotEmpty();
            RuleFor(o => o.Hosts).NotEmpty();
        }
    }
}
