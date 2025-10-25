using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Simon.Tickers;

/// <summary>
///   A .Net wrapper for a high-resolution ticker that ticks at intervals of one or more
///   milliseconds. For accuracy, the ticker is implemented in Rust.
/// </summary>
/// <remarks>
///   This is a steady ticker, which means the priority is to tick at equal intervals
///   rather than for the total duration of a sequence of ticks to equal the expected
///   elapsed time. For example, after 60,000 1-millisecond ticks, the elapsed time
///   might not be exactly one minute.
///   <para>
///     In Windows, the durations of the first one or two ticks are usually inaccurate.
///   </para>
///   <para>
///     The Rust library must be copied to the executable's output directory.
///   </para>
/// </remarks>
public partial class MillisecondTicker : IMillisecondTicker {
  private readonly CallbackDelegate _callbackDelegate;

  /// <summary>
  ///   Instantiates a new ticker, specifying the method to call when the ticker ticks.
  /// </summary>
  /// <param name="onTick">
  ///   The callback method, which will run in a separate thread.
  /// </param>
  public MillisecondTicker(Action onTick) {
    OnTick = onTick;
    // Keep the delegate alive to prevent garbage collection.
    _callbackDelegate = OnRustCallback;
  }

  private bool IsRunning { get; set; }
  
  private Action OnTick { get; }

  /// <summary>
  ///   Rust function to start the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  /// <param name="callback">Callback to run when the ticker ticks.</param>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  private static partial void start_ticker(
    ulong millisecondsInterval,
    CallbackDelegate callback);

  /// <summary>
  ///   Rust function to stop the ticker.
  /// </summary>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  private static partial void stop_ticker();

  /// <summary>
  ///   Starts the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  public void Start(int millisecondsInterval) {
    if (millisecondsInterval < 1) {
      throw new ArgumentException(
        $"{nameof(millisecondsInterval)} {millisecondsInterval} is invalid. " +
        $"It must be positive.");
    }
    if (IsRunning) {
      throw new InvalidOperationException("The ticker is already running.");
    }
    start_ticker((ulong)millisecondsInterval, _callbackDelegate);
    IsRunning = true;
  }

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  public void Stop() {
    stop_ticker();
    IsRunning = false;
  }

  private void OnRustCallback() {
    OnTick();
  }

  /// <summary>
  ///   Delegate matching Rust's callback signature.
  ///   In this case, it's as simple as it gets.
  /// </summary>
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void CallbackDelegate();
}