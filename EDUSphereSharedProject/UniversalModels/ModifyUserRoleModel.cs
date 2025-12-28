using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ModifyUserRoleModel
    {
        public string UserId { get; init; } = default!;
        public string Role { get; init; } = default!;
    }
}
