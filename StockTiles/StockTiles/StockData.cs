using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StockTiles
{
    public class StockData : INotifyPropertyChanged
    {
        private double _price;
        private string _openDelta;
        private string _tickDelta;
        private string _trend30Sec;
        private string _trend1Min;
        private string _upDownIcon;
        private string _priceString;

        public double OpenPrice { get; set; }

        public string Name { get; set; }
        public double Price
        {
            get { return _price; }
            set
            {
                SetProperty(ref _price, value);
            }
        }

        // hack to get around the fact that WinRT XAML doesn't have a string formatting specifier for data binding
        public string PriceString
        {
            get { return _priceString; }
            set
            {
                SetProperty(ref _priceString, value);
            }
        }

        public string UpDownIcon
        {
            get { return _upDownIcon; }
            set
            {
                SetProperty(ref _upDownIcon, value);
            }
        }

        public string OpenDelta
        {
            get { return _openDelta; }
            set
            {
                SetProperty(ref _openDelta, value);
            }
        }
        public string TickDelta
        {
            get { return _tickDelta; }
            set
            {
                SetProperty(ref _tickDelta, value);
            }
        }

        public string MovingAvg30Sec
        {
            get { return _trend30Sec; }
            set
            {
                SetProperty(ref _trend30Sec, value);
            }
        }

        public string MovingAvg1Min
        {
            get { return _trend1Min; }
            set
            {
                SetProperty(ref _trend1Min, value);
            }
        }
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public double variance;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class CombinedData : INotifyPropertyChanged 
    {
        private string _combinedChange;

        public string CombinedChange
        {
            get { return _combinedChange; }
            set
            {
                SetProperty(ref _combinedChange, value);
            }
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value)) {
                field = value;
                var handler = PropertyChanged;
                if (handler != null) {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
