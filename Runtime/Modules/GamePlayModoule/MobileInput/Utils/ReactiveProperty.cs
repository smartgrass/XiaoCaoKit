using System;

namespace MFPC.Utils
{
    public class ReactiveProperty<T>
    {
        private event Action<T> onValueChanged;
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueChanged();
            }
        }

        public ReactiveProperty(T value)
        {
            _value = value;
            ValueChanged();
        }

        public void Subscribe(Action<T> action)
        {
            onValueChanged += action;
        }

        public void Unsubscribe(Action<T> action)
        {
            onValueChanged += action;
        }

        private void ValueChanged()
        {
            onValueChanged?.Invoke(_value);
        }
    }
}