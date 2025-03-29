using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class TemplateField
    {

        public string FieldName { get; set; } = "";
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public int FontSize { get; set; } = 14;
        public bool IsVisible { get; set; } = true;


    }
}
