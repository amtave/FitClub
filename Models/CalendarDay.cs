using System;
using System.ComponentModel;

namespace FitClub.Models
{
    public class CalendarDay : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool HasSchedules { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public Action SelectDateAction { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}