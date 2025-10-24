namespace Simon.Tickers;

/// <summary>
///   An interface for <see cref="MillisecondTicker" />, so in can be mocked in tests.
/// </summary>
public interface IMillisecondTicker {
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