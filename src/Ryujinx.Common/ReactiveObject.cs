﻿using System;
using System.Threading;
using Ryujinx.Common.Extensions;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new();
        private bool _isInitialized;
        private T _value;

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                using (_readerWriterLock.Read())
                {
                    return _value;
                }
            }
            set
            {
                T oldValue;
                bool oldIsInitialized;

                using (_readerWriterLock.Write())
                {
                    oldValue = _value;
                    oldIsInitialized = _isInitialized;

                    _isInitialized = true;
                    _value = value;
                }

                if (!oldIsInitialized || oldValue == null || !oldValue.Equals(_value))
                {
                    Event?.Invoke(this, new ReactiveEventArgs<T>(oldValue, value));
                }
            }
        }

        public static implicit operator T(ReactiveObject<T> obj)
        {
            return obj.Value;
        }
    }

    public class ReactiveEventArgs<T>
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ReactiveEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
