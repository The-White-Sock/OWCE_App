using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using OWCE.Models;
using OWCE.Views;
using Xamarin.Forms;

namespace OWCE.Views
{
    public class BatteryCellsView : ContentView
    {
        /*
        public static readonly BindableProperty CellCountProperty = BindableProperty.Create(nameof(CellCount), typeof(int), typeof(BatteryCellsView), default(int));
        public int CellCount
        {
            get => (int)GetValue(CellCountProperty);
            set => SetValue(CellCountProperty, value);
        }

        public static readonly BindableProperty BatteryCellsProperty = BindableProperty.Create(nameof(BatteryCells), typeof(BatteryCells), typeof(BatteryCellsView), default);       
        public BatteryCells BatteryCells
        {
            get => (BatteryCells)GetValue(BatteryCellsProperty);
            set => SetValue(BatteryCellsProperty, value);
        }
        */

        Dictionary<uint, Label> _cellLables = new Dictionary<uint, Label>();
        BatteryCells _subscribedBatteryCells = null;

        public BatteryCellsView()
        {
            Content = new Grid()
            {
                ColumnSpacing = 0,
                RowSpacing = 0,
            };
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (BindingContextProperty.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            {
                if (_subscribedBatteryCells != null)
                {
                    _subscribedBatteryCells.PropertyChanged -= BatteryCells_PropertyChanged;
                    _subscribedBatteryCells = null;
                }

                if (BindingContext is BatteryCells batteryCells)
                {
                    batteryCells.PropertyChanged += BatteryCells_PropertyChanged;
                    _subscribedBatteryCells = batteryCells;
                    SetupGrid();
                }
            }
        }

        private void SetupGrid()
        {
            if (Content is Grid grid && BindingContext is BatteryCells batteryCells)
            {
                _cellLables.Clear();
                grid.Children.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Clear();

                int columns = 0;
                int rows = 0;
                if (batteryCells.CellCount == 15)
                {
                    rows = 3;
                    columns = 5;
                }
                else if (batteryCells.CellCount == 16)
                {
                    rows = 4;
                    columns = 4;
                }
                else if (batteryCells.CellCount == 18)
                {
                    rows = 6;
                    columns = 3;
                }
                else
                {
                    return;
                }

                // Setup row definitions.
                for (var row = 0; row < rows; ++row)
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(45) });
                }

                // Setup column definitions (not required, its * by default)
                for (var column = 0; column < columns; ++column)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                }

                /*
                var minVoltage = 2.8f;
                var maxVoltage = 4.2f;
                var voltageRange = maxVoltage - minVoltage; //1.4f
                var voltagePerIndex = voltageRange / batteryCells.CellCount;
                */
                var color = Color.Magenta;

                for (var row = 0; row < rows; ++row)
                {
                    for (var column = 0; column < columns; ++column)
                    {
                        var cellIndex = column + (columns * row);

                        //var voltage = minVoltage + (voltagePerIndex * cellIndex);
                        var label = new Label()
                        {
                            TextColor = Color.White,
                            Text = "-",
                            FontSize = 24,
                            FontFamily = "SairaExtraCondensed-Bold",
                            BackgroundColor = Color.Magenta,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        };


                        Grid.SetRow(label, row);
                        Grid.SetColumn(label, column);

                        grid.Children.Add(label);

                        //var cellKey = $"BatteryCell{cellIndex}";
                        _cellLables.Add((uint)cellIndex, label);
                    }
                }

            }
        }



        private void BatteryCells_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("BatteryCell"))
            {
                var labelSpan = e.PropertyName.AsSpan();

                // 11 = "BatteryCell".Length
                var cellIDSpan = labelSpan.Slice(11);

                if (UInt32.TryParse(cellIDSpan, out uint cellID))
                {
                    UpdateCell(cellID);
                }
            }
            else if (e.PropertyName.StartsWith("CellCount"))
            {
                SetupGrid();
            }
        }

        void UpdateCell(uint cellID)
        {
            if (BindingContext is BatteryCells batteryCells && _cellLables.ContainsKey(cellID))
            {
                var value = batteryCells.GetCell(cellID);
                var label = _cellLables[cellID];
                var color = GetColor(value);

                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    label.Text = value.ToString("F2", CultureInfo.CurrentCulture);
                    label.BackgroundColor = color;
                });
            }
        }

        Color GetColor(float voltage)
        {
            var minVoltage = 2.8f;
            var maxVoltage = 4.2f;

            if (voltage < minVoltage)
            {
                voltage = minVoltage;
            }
            else if (voltage > maxVoltage)
            {
                voltage = maxVoltage;
            }

            var voltageRange = maxVoltage - minVoltage; //1.4f
            var voltageOffset = voltage - minVoltage; // 0.46
            var voltagePercent = 1f - (voltageOffset / voltageRange);
            var colorPercent = (voltagePercent * 0.788235294118);

            //System.Diagnostics.Debug.WriteLine($"{voltage:F2} - {colorPercent:F2}");
            return new Color(1f, colorPercent, 1f);


        }
    }
}

