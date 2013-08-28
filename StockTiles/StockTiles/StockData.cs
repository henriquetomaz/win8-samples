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
        private string _trendTick;
        private string _trend30Sec;
        private string _trend1Min;

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
        public string TrendTick
        {
            get { return _trendTick; }
            set
            {
                SetProperty(ref _trendTick, value);
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
