using System.Diagnostics.CodeAnalysis;

namespace Simon.Tickers;

/// <summary>
///   A .Net wrapper for a high-resolution ticker that ticks at intervals of one or more
///   milliseconds. For accuracy, the ticker is implemented in Rust.
/// </summary>
/// <remarks>
///   This is a steady ticker, which means the priority is to tick at equal intervals
///   rather than for the total duration of a sequence of ticks to equal the expected
///   elapsed time. For example, after 600,000 1-millisecond ticks, the elapsed time
///   will probably not be exactly ten minutes.
///   <para>
///     In Windows, the durations of the first two ticks are usually inaccurate.
///     However, the total duration of the two ticks is accurate, except where the
///     specified interval is 1-millisecond or close to it. This initial inaccuracy may
///     be trivial in an application.
///   </para>
///   <para>
///     The Rust library must be copied to the executable's output directory.
///   </para>
/// </remarks>
public class MillisecondTicker : IMillisecondTicker {
  /// <summary>
  ///   Instantiates a new ticker, specifying the method to call when the ticker ticks.
  /// </summary>
  /// <param name="onTick">
  ///   The callback method, which will run in a separate thread.
  /// </param>
  public MillisecondTicker(Action onTick) {
    OnTick = onTick;
  }

  private Action OnTick { get; }
  private Starter? Starter { get; set; }

  /// <summary>
  ///   Whether the ticker is running.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
#pragma warning disable CA1822
  public bool IsRunning => RustFunctions.ticker_is_running() == 1;
#pragma warning restore CA1822

  /// <summary>
  ///   Starts the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  public void Start(int millisecondsInterval) {
    if (millisecondsInterval < 0) { }
    if (millisecondsInterval < 1) {
      throw new ArgumentException(
        $"{nameof(millisecondsInterval)} {millisecondsInterval} is invalid. " +
        $"It must be positive.");
    }
    if (IsRunning) {
      throw new InvalidOperationException("The ticker is already running.");
    }
    // The Starter contains the Rust callback delegate. So holding the Starter in a 
    // property and instantiating it each time the ticker is started prevents the
    // callback delegate from being garbage collected.
    Starter = new Starter();
    Starter.Start(millisecondsInterval, OnTick);
  }

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  public void Stop() {
    RustFunctions.stop_ticker();
  }
}