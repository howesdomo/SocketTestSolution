using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class MyMessage : Util.UIComponent.VirtualModel
    {
        public DateTime ReceiveTime { get; set; }

        public string Content { get; set; }

        public int Length { get; set; }
    }
}
