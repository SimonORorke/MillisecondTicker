namespace Simon.Tickers;

/// <summary>
///   The behavior of a <see cref="MillisecondTicker" /> when it misses a tick.
/// </summary>
/// <remarks>
///   This is a Rust enum, used to configure the Rust Interval from which
///   <see cref="MillisecondTicker" /> gets its ticks. See the documentation at
///   https://docs.rs/tokio/latest/tokio/time/enum.MissedTickBehavior.html
///   for details.
/// </remarks>
public enum MissedTickBehavior {
  /// <summary>
  ///   Tick as fast as possible until caught up.
  /// </summary>
  Burst = 0,
  
  /// <summary>
  ///   Tick at multiples of multiples of the specified tick interval from the last
  ///   tick, rather than from when ticking started.
  /// </summary>
  Delay = 1,
  
  /// <summary>
  ///   Skip missed ticks and then tick on the next multiple of the specified tick
  ///   interval from when ticking started.
  /// </summary>
  Skip = 2
}