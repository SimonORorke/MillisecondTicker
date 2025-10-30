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
  private readonly RustFunctions.CallbackDelegate _callbackDelegate;

  /// <summary>
  ///   Instantiates a new ticker, specifying the method to call when the ticker ticks.
  /// </summary>
  /// <param name="onTick">
  ///   The callback method, which will run in a separate thread.
  /// </param>
  public MillisecondTicker(Action onTick) {
    OnTick = onTick;
    _callbackDelegate = OnRustCallback;
  }

  private bool HasStarted { get; set; }

  /// <summary>
  ///   Whether the ticker is running.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
#pragma warning disable CA1822
  public bool IsRunning => RustFunctions.ticker_is_running() == 1;
#pragma warning restore CA1822
  
  private Action OnTick { get; }

  /// <summary>
  ///   Starts the ticker.
  /// </summary>
  /// <remarks>
  ///   To avoid callback delegate garbage collection, instantiate
  ///   <see cref="MillisecondTicker" /> before each call of <see cref="Start" />.
  ///   To avoid that requirement, I tried instead keeping the callback delegate alive
  ///   in a worker thread. But then we would have to make
  ///   <see cref="MillisecondTicker" /> disposable so that the worker thread could poll
  ///   for disposal and stop itself.
  ///   I tried that and found that it caused problems in applications.
  /// </remarks>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  public void Start(int millisecondsInterval) {
    if (HasStarted) {
      throw new InvalidOperationException(
        "The ticker has been started before. Multiple calls to Start are not " +
        "allowed. You must create a new MillisecondTicker instance and start that.");
    }
    if (millisecondsInterval < 0) {}
    if (millisecondsInterval < 1) {
      throw new ArgumentException(
        $"{nameof(millisecondsInterval)} {millisecondsInterval} is invalid. " +
        $"It must be positive.");
    }
    if (IsRunning) {
      throw new InvalidOperationException("The ticker is already running.");
    }
    // THIS DOES NOT WORK. IT MAKES THE AVALONIA APPLICATION FREEZES WHEN THE TICKER IS
    // STARTED.
    // If the ticker has previously been stopped, the callback delegate may have
    // been garbage collected. So we need to re-create it each time we start the ticker.
    //_callbackDelegate = OnRustCallback;
    RustFunctions.start_ticker((ulong)millisecondsInterval, _callbackDelegate);
    HasStarted = true;
  }

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  public void Stop() {
    RustFunctions.stop_ticker();
  }

  private void OnRustCallback() {
    OnTick();
    // Keep the delegate alive to prevent garbage collection.
    GC.KeepAlive(_callbackDelegate);
  }
}