using System.Diagnostics.CodeAnalysis;

namespace Simon.Tickers;

/// <summary>
///   An interface for <see cref="MillisecondTicker" />, so in can be mocked in tests.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IMillisecondTicker {
  /// <summary>
  ///   Whether the ticker is currently running.
  /// </summary>
  bool IsRunning { get; }
  
  /// <summary>
  ///   Starts the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  void Start(int millisecondsInterval);

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  void Stop();
}