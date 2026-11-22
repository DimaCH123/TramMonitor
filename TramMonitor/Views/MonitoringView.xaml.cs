using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TramMonitor.Simulation;
using TramMonitor.ViewModels;
using System.Collections.ObjectModel;

namespace TramMonitor.Views
{
    public partial class MonitoringView : UserControl
    {
        private readonly Brush route2Brush =
            new SolidColorBrush(Color.FromRgb(58, 48, 137));

        private readonly Brush route4Brush =
            new SolidColorBrush(Color.FromRgb(0, 145, 75));

        private readonly Brush route4aBrush =
            new SolidColorBrush(Color.FromRgb(166, 99, 77));

        private readonly Brush route8Brush =
            new SolidColorBrush(Color.FromRgb(220, 35, 45));

        private MonitoringViewModel? _viewModel;

        private readonly Dictionary<int, FrameworkElement> _tramMarkers = new();

        public MonitoringView()
        {
            InitializeComponent();

            DrawMap();

            DataContextChanged += MonitoringView_DataContextChanged;
            Loaded += MonitoringView_Loaded;
            Unloaded += MonitoringView_Unloaded;
        }

        private void MonitoringView_DataContextChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel();
        }

        private void MonitoringView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel();
        }

        private void MonitoringView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SimulationUpdated -= UpdateTramMarkers;
                _viewModel = null;
            }
        }

        private void AttachViewModel()
        {
            if (DataContext is not MonitoringViewModel vm)
                return;

            if (_viewModel == vm)
                return;

            if (_viewModel != null)
                _viewModel.SimulationUpdated -= UpdateTramMarkers;

            _viewModel = vm;
            _viewModel.SimulationUpdated += UpdateTramMarkers;

            UpdateTramMarkers();
        }

        private void UpdateTramMarkers()
        {
            var markerWatch = System.Diagnostics.Stopwatch.StartNew();
            if (_viewModel == null)
                return;

            foreach (var tram in _viewModel.Simulation.Trams)
            {
                // Получаем физическое положение трамвая из геометрии маршрута.
                var location = tram.GetCurrentLocation(
                    _viewModel.Simulation.CurrentTime);

                Point position = new Point(location.X, location.Y);

                if (!_tramMarkers.TryGetValue(tram.Id, out var marker))
                {
                    marker = CreateTramMarker(tram);
                    _tramMarkers.Add(tram.Id, marker);
                    MapCanvas.Children.Add(marker);
                }
                else if (marker is Grid markerGrid &&
                         markerGrid.Children.Count > 0 &&
                         markerGrid.Children[0] is Ellipse circle)
                {
                    circle.Fill = GetTramBrush(tram.RouteNumber);
                }

                // Размещаем маркер строго по физическим координатам.
                Canvas.SetLeft(marker, position.X - 12);
                Canvas.SetTop(marker, position.Y - 12);
            }

            var gridWatch = System.Diagnostics.Stopwatch.StartNew();

            TramGrid.Items.Refresh();

            gridWatch.Stop();

            System.Diagnostics.Debug.WriteLine(
                $"[PERFORMANCE] Grid refresh: " +
                $"{gridWatch.Elapsed.TotalMilliseconds:F2} ms");

            markerWatch.Stop();

            System.Diagnostics.Debug.WriteLine(
                $"[PERFORMANCE] Marker update: " +
                $"{markerWatch.Elapsed.TotalMilliseconds:F2} ms");
        }

        private FrameworkElement CreateTramMarker(SimulatedTram tram)
        {
            var marker = new Grid
            {
                Width = 24,
                Height = 24,
                ToolTip = $"Маршрут № {tram.RouteNumber}, трамвай {tram.NumberOnRoute}"
            };

            var circle = new Ellipse
            {
                Fill = GetTramBrush(tram.RouteNumber),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            var number = new TextBlock
            {
                Text = tram.NumberOnRoute.ToString(),
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            marker.Children.Add(circle);
            marker.Children.Add(number);

            return marker;
        }

        private Brush GetTramBrush(string routeNumber)
        {
            return routeNumber switch
            {
                "2" => route2Brush,
                "4" => route4Brush,
                "4а" => route4aBrush,
                "8" => route8Brush,
                _ => Brushes.Gray
            };
        }

        private void DrawMap()
        {
            MapCanvas.Children.Clear();

            double top = 80;
            double bottom = 2240;
            double x = 500;

            // Маршрут № 2
            DrawLine(route2Brush, 504, bottom, 504, 1360);
            DrawLine(route2Brush, 504, 80, 504, 1180);

            // Маршрут № 4
            DrawLine(route4Brush, 498, bottom, 498, top);

            // Маршрут № 4а
            DrawLine(route4aBrush, 492, bottom, 492, 500);

            // Маршрут № 8
            DrawLine(route8Brush, 510, bottom, 510, 1180);

            // Ответвление к Вокзалу (Маршрут № 2)
            DrawLine(route2Brush, 506, 1186, 640, 1186);
            DrawLine(route2Brush, 640, 1186, 640, 1354);
            DrawLine(route2Brush, 640, 1354, 506, 1354);

            // Ответвление к Вокзалу (Маршрут № 8)
            DrawLine(route8Brush, 510, 1360, 646, 1360);
            DrawLine(route8Brush, 646, 1180, 646, 1360);
            DrawLine(route8Brush, 646, 1180, 510, 1180);



            string[] stops =
            {
                "Аглофабрика № 3",
                "Кислородная",
                "Водородная",
                "Доменная",
                "Заводская",
                "Управление ПАО «Северсталь»",
                "Улица Гагарина",
                "Улица Комарова",
                "Университет",
                "Улица Сталеваров",
                "Улица Верещагина",
                "Улица Максима Горького",
                "Красноармейская площадь",
                "Улица Набережная",
                "Улица Первомайская",
                "Фабрика «Красный ткач»",
                "Улица Архангельская",
                "Кинотеатр «Победа»",
                "Улица Юбилейная",
                "23 микрорайон",
                "Улица Олимпийская"
            };

            double[] stopY =
            {
                80,    // Аглофабрика № 3
                280,   // Кислородная
                380,   // Водородная
                500,   // Доменная
                580,   // Заводская
                680,   // Центральное заводоуправление
                840,   // Гагарина
                900,   // Комарова
                1020,  // Университет
                1100,  // Сталеваров
                1180,  // Верещагина
                1240,  // Максима Горького
                1360,  // Красноармейская площадь
                1460,  // Набережная
                1680,  // Первомайская
                1760,  // Красный ткач
                1880,  // Архангельская
                1960,  // Кинотеатр Победа
                2040,  // Юбилейная
                2166,  // 23-й микрорайон
                2240   // Олимпийская
            };

            for (int i = 0; i < stops.Length; i++)
            {
                DrawStop(stops[i], 500, stopY[i]);
            }

            DrawStop("Улица Заря Свободы", 620, 1180, true, true);
            DrawStop("Вокзал", 640, 1240, true, true);
            DrawStop("Военный университет", 640, 1360, true, true);

            DrawDepot();
        }

        private void DrawLine(
            Brush color,
            double x1,
            double y1,
            double x2,
            double y2)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = color,
                StrokeThickness = 3
            };

            MapCanvas.Children.Add(line);
        }


        private void MapScrollViewer_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            // Без Alt колесо полностью передаётся ScrollViewer.
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                return;

            // При зажатом Alt колесо отвечает только за масштаб.
            e.Handled = true;

            double oldScale = MapScaleTransform.ScaleX;
            double delta = e.Delta > 0 ? 0.1 : -0.1;

            double newScale = Math.Clamp(oldScale + delta, 0.5, 3.0);

            if (Math.Abs(newScale - oldScale) < 0.001)
                return;

            // Положение курсора относительно видимой области карты.
            Point mousePosition = e.GetPosition(MapScrollViewer);

            // Запоминаем текущую прокрутку.
            double oldHorizontalOffset = MapScrollViewer.HorizontalOffset;
            double oldVerticalOffset = MapScrollViewer.VerticalOffset;

            // Меняем масштаб.
            MapScaleTransform.ScaleX = newScale;
            MapScaleTransform.ScaleY = newScale;

            // Пересчитываем размеры прокручиваемой области.
            MapScrollViewer.UpdateLayout();

            // Сохраняем точку карты под курсором на прежнем месте.
            double scaleFactor = newScale / oldScale;

            MapScrollViewer.ScrollToHorizontalOffset(
                (oldHorizontalOffset + mousePosition.X) * scaleFactor
                - mousePosition.X);

            MapScrollViewer.ScrollToVerticalOffset(
                (oldVerticalOffset + mousePosition.Y) * scaleFactor
                - mousePosition.Y);
        }



        private void DrawStop(
            string name,
            double x,
            double y,
            bool rightSide = false,
            bool singleCircle = false)
        {
            var stopStroke = new SolidColorBrush(
                Color.FromRgb(45, 45, 45));

            if (singleCircle)
            {
                // Одна остановка для участка с движением
                // только в одном направлении.
                var circle = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = stopStroke,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(circle, x - 9);
                Canvas.SetTop(circle, y - 9);

                MapCanvas.Children.Add(circle);
            }
            else
            {
                // Левый кружок — движение обратно.
                var leftCircle = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = stopStroke,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(leftCircle, x - 21);
                Canvas.SetTop(leftCircle, y - 9);

                MapCanvas.Children.Add(leftCircle);

                // Правый кружок — движение к заводу.
                var rightCircle = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = stopStroke,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(rightCircle, x + 3);
                Canvas.SetTop(rightCircle, y - 9);

                MapCanvas.Children.Add(rightCircle);
            }

            var label = new TextBlock
            {
                Text = name,
                FontSize = 17,
                Foreground = stopStroke,
                TextAlignment = rightSide
                    ? TextAlignment.Left
                    : TextAlignment.Right
            };

            label.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

            if (rightSide)
                Canvas.SetLeft(label, x + 30);
            else
                Canvas.SetLeft(
                    label,
                    x - 35 - label.DesiredSize.Width);

            Canvas.SetTop(label, y - 13);

            MapCanvas.Children.Add(label);
        }

        private void DrawDepot()
        {
            // Путь от Олимпийской к депо.
            DrawLine(
                new SolidColorBrush(Color.FromRgb(100, 110, 120)),
                500, 2240,
                500, 2258);

            // Здание депо.
            var building = new Rectangle
            {
                Width = 90,
                Height = 44,
                Fill = new SolidColorBrush(Color.FromRgb(225, 232, 238)),
                Stroke = new SolidColorBrush(Color.FromRgb(55, 70, 85)),
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };

            Canvas.SetLeft(building, 455);
            Canvas.SetTop(building, 2258);

            MapCanvas.Children.Add(building);

            // Ворота депо.
            var gates = new Rectangle
            {
                Width = 34,
                Height = 23,
                Fill = new SolidColorBrush(Color.FromRgb(180, 195, 207)),
                Stroke = new SolidColorBrush(Color.FromRgb(55, 70, 85)),
                StrokeThickness = 1.5
            };

            Canvas.SetLeft(gates, 483);
            Canvas.SetTop(gates, 2279);

            MapCanvas.Children.Add(gates);

            // Подпись.
            var label = new TextBlock
            {
                Text = "ДЕПО",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 65))
            };

            label.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

            Canvas.SetLeft(label, 500 - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, 2310);

            MapCanvas.Children.Add(label);
        }

    }
}