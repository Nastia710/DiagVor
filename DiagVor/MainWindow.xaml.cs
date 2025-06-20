using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DiagVor
{
    public class PointWithColor
    {
        public Point Point { get; set; }
        public Color Color { get; set; }
    }

    public enum Metric
    {
        Euclid,
        Manhattan,
        MaxDistance
    }

    public partial class MainWindow : Window
    {
        private const double PointRadius = 3;
        private List<Ellipse> points;
        private List<PointWithColor> coloredPoints;
        private Random random;
        private Metric selectedMetric;
        private WriteableBitmap writeableBitmap;
        private readonly object lockObject = new object();

        public MainWindow()
        {
            InitializeComponent();
            points = new List<Ellipse>();
            coloredPoints = new List<PointWithColor>();
            random = new Random();
        }

        private void InitializeWriteableBitmap()
        {
            if (Canvas.ActualWidth <= 0 || Canvas.ActualHeight <= 0) return;
            writeableBitmap = new WriteableBitmap(
                (int)Canvas.ActualWidth,
                (int)Canvas.ActualHeight,
                96, 96,
                PixelFormats.Bgr32,
                null);
            Canvas.Background = new ImageBrush(writeableBitmap);
        }

        private double CalculateDistance(double[] point1, double[] point2)
        {
            switch (GetSelectedMetric())
            {
                case Metric.Euclid:
                    return Math.Sqrt(Math.Pow(point1[0] - point2[0], 2) + Math.Pow(point1[1] - point2[1], 2));
                case Metric.Manhattan:
                    return Math.Abs(point1[0] - point2[0]) + Math.Abs(point1[1] - point2[1]);
                case Metric.MaxDistance:
                    return Math.Max(Math.Abs(point1[0] - point2[0]), Math.Abs(point1[1] - point2[1]));
                default:
                    throw new NotImplementedException("Метрика не реалізована.");
            }
        }

        private double CalculateEuclideanDistance(double[] point1, double[] point2)
        {
            return Math.Sqrt(Math.Pow(point1[0] - point2[0], 2) + Math.Pow(point1[1] - point2[1], 2));
        }

        private void ConvertPointsToColoredPoints()
        {
            coloredPoints.Clear();
            foreach (var ellipse in points)
            {
                coloredPoints.Add(new PointWithColor
                {
                    Point = new Point(
                        Canvas.GetLeft(ellipse) + PointRadius,
                        Canvas.GetTop(ellipse) + PointRadius
                    ),
                    Color = Color.FromRgb(
                        (byte)random.Next(64, 256),
                        (byte)random.Next(64, 256),
                        (byte)random.Next(64, 256)
                    )
                });
            }
        }

        private void CreateSingleThread()
        {
            if (writeableBitmap == null) InitializeWriteableBitmap();
            if (writeableBitmap == null) return;

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            ConvertPointsToColoredPoints();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double minDistance = double.MaxValue;
                    Color pixelColor = Colors.White;

                    foreach (var point in coloredPoints)
                    {
                        double distance = CalculateDistance(
                            new double[] { x, y },
                            new double[] { point.Point.X, point.Point.Y }
                        );

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            pixelColor = point.Color;
                        }
                    }

                    int index = y * stride + x * 4;
                    pixels[index] = pixelColor.B;
                    pixels[index + 1] = pixelColor.G;
                    pixels[index + 2] = pixelColor.R;
                    pixels[index + 3] = 255;
                }
            }

            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, width, height),
                pixels,
                stride,
                0
            );
        }

        private void CreateMultiThread()
        {
            if (writeableBitmap == null) InitializeWriteableBitmap();
            if (writeableBitmap == null) return;

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            ConvertPointsToColoredPoints();

            int numThreads = Environment.ProcessorCount;
            int rowsPerThread = height / numThreads;

            var tasks = new Task[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                int startY = i * rowsPerThread;
                int endY = (i == numThreads - 1) ? height : (i + 1) * rowsPerThread;

                tasks[i] = Task.Run(() =>
                {
                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double minDistance = double.MaxValue;
                            Color pixelColor = Colors.White;

                            foreach (var point in coloredPoints)
                            {
                                double distance = CalculateEuclideanDistance(
                                    new double[] { x, y },
                                    new double[] { point.Point.X, point.Point.Y }
                                );

                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    pixelColor = point.Color;
                                }
                            }

                            int index = y * stride + x * 4;
                            lock (lockObject)
                            {
                                pixels[index] = pixelColor.B;
                                pixels[index + 1] = pixelColor.G;
                                pixels[index + 2] = pixelColor.R;
                                pixels[index + 3] = 255;
                            }
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, width, height),
                pixels,
                stride,
                0
            );
        }

        private void GenerateSingleThread_Click(object sender, RoutedEventArgs e)
        {
            if (MetricsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, виберіть метрику перед побудовою діаграми", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedMetric = GetSelectedMetric();
            
            var stopwatch = Stopwatch.StartNew();
            var cpuStartTime = Process.GetCurrentProcess().TotalProcessorTime;
            
            CreateSingleThread();
            
            stopwatch.Stop();
            var cpuEndTime = Process.GetCurrentProcess().TotalProcessorTime;
            
            textBlockTime.Text = $"Однопотоково: - Реальний час: {stopwatch.ElapsedMilliseconds} мс; - Процесорний час: {(cpuEndTime - cpuStartTime).TotalMilliseconds:F1} мс";
        }

        private void GenerateMultiThread_Click(object sender, RoutedEventArgs e)
        {
            MetricsComboBox.SelectedIndex = 0;

            var stopwatch = Stopwatch.StartNew();
            var cpuStartTime = Process.GetCurrentProcess().TotalProcessorTime;
            
            CreateMultiThread();
            
            stopwatch.Stop();
            var cpuEndTime = Process.GetCurrentProcess().TotalProcessorTime;
            
            textBlockTime.Text = $"Багатопотоково ({Environment.ProcessorCount} потоків): - Реальний час: {stopwatch.ElapsedMilliseconds} мс; - Процесорний час: {(cpuEndTime - cpuStartTime).TotalMilliseconds:F1} мс";
        }

        private Metric GetSelectedMetric()
        {
            if (MetricsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, виберіть метрику", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                
            }

            string metricText = ((ComboBoxItem)MetricsComboBox.SelectedItem).Content.ToString();
            return metricText switch
            {
                "Евклідова" => Metric.Euclid,
                "Манхеттенська" => Metric.Manhattan,
                "Max відстані" => Metric.MaxDistance
            };
        }

        private void GeneratePoints_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(NumPoints.Text, out int numberOfPoints) || numberOfPoints <= 0)
            {
                MessageBox.Show("Будь ласка, введіть додатне число точок", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Canvas.Children.Clear();
            points.Clear();

            double canvasWidth = Canvas.ActualWidth;
            double canvasHeight = Canvas.ActualHeight;

            for (int i = 0; i < numberOfPoints; i++)
            {
                double x = random.NextDouble() * (canvasWidth - 2 * PointRadius) + PointRadius;
                double y = random.NextDouble() * (canvasHeight - 2 * PointRadius) + PointRadius;

                var point = new Ellipse
                {
                    Width = PointRadius * 2,
                    Height = PointRadius * 2,
                    Fill = Brushes.Black
                };

                Canvas.SetLeft(point, x - PointRadius);
                Canvas.SetTop(point, y - PointRadius);

                Canvas.Children.Add(point);
                points.Add(point);
            }

            NumPoints.Text = points.Count.ToString();
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
                    Fill = Brushes.Black
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
        }
    }
}