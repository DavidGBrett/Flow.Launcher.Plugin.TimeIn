using System;

namespace Flow.Launcher.Plugin.TimeIn
{
    public readonly struct CachedValue<T>{
        public T Value {get;}
        public DateTime ExpiresAt { get; }
        private readonly Func<DateTime> _timeProvider;

        public CachedValue(T value, TimeSpan expirationTime, Func<DateTime>? timeProvider = null){
            _timeProvider = timeProvider ?? (() => DateTime.UtcNow);
            Value = value;
            ExpiresAt = _timeProvider() + expirationTime;
        }

        public bool IsExpired() => _timeProvider() > ExpiresAt;
    }
}