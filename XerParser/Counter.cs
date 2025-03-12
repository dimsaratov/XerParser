using System.ComponentModel;

namespace XerParser
{
    /// <summary>
    /// Progress counter
    /// </summary>
    public class Counter : INotifyPropertyChanged
    {
        private decimal _max = 1;
        private decimal _value = 0;
        private string _text = string.Empty;

        /// <summary>
        /// Create new counter
        /// </summary>
        public Counter() { }

        #region Property
        /// <summary>
        /// Message
        /// </summary>
        [DefaultValue("")]
        public string Message
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        /// <summary>
        /// Current value
        /// </summary>
        [DefaultValue(0)]
        public decimal Value
        {
            get => _value;
            set
            {
                if (_value >= 0 && _value <= _max)
                {
                    _value = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        /// <summary>
        /// Maximum value
        /// </summary>
        [DefaultValue(100)]
        public decimal Maximum
        {
            get => _max;
            set
            {
                decimal newValue = value switch
                {
                    < 1 => 1,
                    _ => value,
                };
                if (_max != newValue)
                {
                    _max = newValue;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Maximum)));
                }
            }
        }
        /// <summary>
        /// Percentage of completion
        /// </summary>
        /// <returns></returns>
        public decimal Percent()
        {
            return _value / _max * 100;
        }

        /// <summary>
        /// Reset counter
        /// </summary>
        public void Reset()
        {
            _value = 0;
            _max = 1;
            _text = string.Empty;
        }

        #endregion


        private PropertyChangedEventHandler onPropertyChanged = null;

        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        /// <param name="e">PropertyChangedEventArgs</param>
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            onPropertyChanged?.Invoke(this, e);
        }


        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => onPropertyChanged += value;
            remove => onPropertyChanged -= value;
        }
    }
}
