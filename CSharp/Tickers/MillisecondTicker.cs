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
public partial class MillisecondTicker : IMillisecondTicker {
  private readonly CallbackDelegate _callbackDelegate;
  private int _isRunningBackValue;

  /// <summary>
  ///   Instantiates a new ticker, specifying the method to call when the ticker ticks.
  /// </summary>
  /// <param name="onTick">
  ///   The callback method, which will run in a separate thread.
  /// </param>
  public MillisecondTicker(Action onTick) {
    GC.KeepAlive(onTick);
    OnTick = onTick;
    _callbackDelegate = OnRustCallback;
    IsRunning = false;
  }

  /// <summary>
  ///   Thread safe bool.
  /// </summary>
  private bool IsRunning {
    get => Interlocked.CompareExchange(ref _isRunningBackValue, 1, 1) == 1;
    set {
      if (value) {
        Interlocked.CompareExchange(ref _isRunningBackValue, 1, 0);
      } else {
        Interlocked.CompareExchange(ref _isRunningBackValue, 0, 1);
      }
    }
  }
  
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
    // If called from an Avalonia application, the exception is thrown even when
    // IsRunning is false.  So until a fix is found we don't check it.
    // if (IsRunning) {
    //   throw new InvalidOperationException("The ticker is already running.");
    // }
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
    // Keep the delegate alive to prevent garbage collection.
    GC.KeepAlive(_callbackDelegate);
    OnTick();
  }

  /// <summary>
  ///   Delegate matching Rust's callback signature.
  ///   In this case, it's as simple as it gets.
  /// </summary>
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void CallbackDelegate();
}