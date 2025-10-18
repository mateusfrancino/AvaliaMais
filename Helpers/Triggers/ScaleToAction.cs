using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalia_.Helpers.Triggers
{
    public class ScaleToAction : TriggerAction<VisualElement>
    {
        public double Scale { get; set; } = 1.0;
        public uint Length { get; set; } = 300; // ms
        public Easing Easing { get; set; } = Easing.CubicOut;

        protected override async void Invoke(VisualElement sender)
        {
            // cancela animações em andamento e faz a nova
            sender.AbortAnimation("ScaleTo");
            await sender.ScaleTo(Scale, Length, Easing);
        }
    }
}
