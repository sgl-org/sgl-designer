using System.Windows;
using System.Windows.Controls;

namespace SglDesigner.PropertyPanels
{
    public partial class PolygonPropertyPanel : UserControl
    {
        public PolygonPropertyPanel()
        {
            InitializeComponent();
        }

        private void AddVertex_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SglPolygonData data)
            {
                // 在最后一个点附近添加一个新点，方便用户看到
                double newX = data.Vertices.Count > 0 ? data.Vertices[data.Vertices.Count - 1].X + 10 : 0;
                double newY = data.Vertices.Count > 0 ? data.Vertices[data.Vertices.Count - 1].Y + 10 : 0;
                data.Vertices.Add(new SglPoint(newX, newY));
            }
        }

        private void RemoveVertex_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SglPoint pt && DataContext is SglPolygonData data)
            {
                // SGL 多边形至少需要 3 个点
                if (data.Vertices.Count > 3)
                {
                    data.Vertices.Remove(pt);
                }
                else
                {
                    MessageBox.Show("SGL Polygon 至少需要 3 个顶点。");
                }
            }
        }
    }
}