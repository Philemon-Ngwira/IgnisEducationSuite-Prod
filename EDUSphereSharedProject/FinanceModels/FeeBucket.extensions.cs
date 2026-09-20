using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels
{
    public partial class FeeBucket
    {
        public FeeBucket Clone() => new FeeBucket
        {
            Id = Id,
            SchoolId = SchoolId,
            Name = Name,
            AccountName = AccountName,
            AccountNumber = AccountNumber,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            BankName = BankName,
            Color = Color,
            IsDefault = IsDefault,
            IsMandatory = IsMandatory,
        };
    }
}
