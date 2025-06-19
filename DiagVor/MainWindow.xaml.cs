using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiagVor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double PointRadius = 3;
        private List<Ellipse> points;

        public MainWindow()
        {
            InitializeComponent();
            points = new List<Ellipse>();
        }

        private void BuildVoronoi_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(Canvas);

            Ellipse clickedPoint = null;
            foreach (var point in points)
            {
                Point pointCenter = new Point(
                    Canvas.GetLeft(point) + PointRadius,
                    Canvas.GetTop(point) + PointRadius
                );

                double distance = Math.Sqrt(
                    Math.Pow(clickPosition.X - pointCenter.X, 2) +
                    Math.Pow(clickPosition.Y - pointCenter.Y, 2)
                );

                if (distance <= PointRadius)
                {
                    clickedPoint = point;
                    break;
                }
            }

            if (clickedPoint != null)
            {
                Canvas.Children.Remove(clickedPoint);
                points.Remove(clickedPoint);
                NumPoints.Text = points.Count.ToString();
            }
            else
            {
                var point = new Ellipse
                {
                    Width = PointRadius * 2,
                    Height = PointRadius * 2,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(point, clickPosition.X - PointRadius);
                Canvas.SetTop(point, clickPosition.Y - PointRadius);

                Canvas.Children.Add(point);
                points.Add(point);
                NumPoints.Text = points.Count.ToString();
            }
        }

        private void ClearPoints_Click(object sender, RoutedEventArgs e)
        {
            Canvas.Children.Clear();
            points.Clear();
            NumPoints.Text = "0";
        }

        private void GenerateSingleThread_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GenerateMultiThread_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}