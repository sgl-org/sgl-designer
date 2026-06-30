using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SglDesigner
{
    public static class LayoutHelper
    {
        public static void Align(List<Border> selected, string type)
        {
            if (selected == null || selected.Count < 2) return;
            double minL = selected.Min(b => Canvas.GetLeft(b));
            double maxR = selected.Max(b => Canvas.GetLeft(b) + b.Width);
            double minT = selected.Min(b => Canvas.GetTop(b));
            double maxB = selected.Max(b => Canvas.GetTop(b) + b.Height);

            foreach (var w in selected)
            {
                if (type == "Left") Canvas.SetLeft(w, minL);
                else if (type == "CenterX") Canvas.SetLeft(w, minL + (maxR - minL) / 2 - w.Width / 2);
                else if (type == "Right") Canvas.SetLeft(w, maxR - w.Width);
                else if (type == "Top") Canvas.SetTop(w, minT);
                else if (type == "CenterY") Canvas.SetTop(w, minT + (maxB - minT) / 2 - w.Height / 2);
                else if (type == "Bottom") Canvas.SetTop(w, maxB - w.Height);
            }

            if (type == "DistH") Distribute(selected, true, minL, maxR);
            if (type == "DistV") Distribute(selected, false, minT, maxB);
        }

        private static void Distribute(List<Border> selected, bool horizontal, double min, double max)
        {
            var list = horizontal ? selected.OrderBy(b => Canvas.GetLeft(b)).ToList() : selected.OrderBy(b => Canvas.GetTop(b)).ToList();
            double totalUsed = list.Sum(x => horizontal ? x.Width : x.Height);
            double gap = (max - min - totalUsed) / (list.Count - 1);
            double current = min;
            foreach (var w in list)
            {
                if (horizontal) { Canvas.SetLeft(w, current); current += w.Width + gap; }
                else { Canvas.SetTop(w, current); current += w.Height + gap; }
            }
        }
    }
}