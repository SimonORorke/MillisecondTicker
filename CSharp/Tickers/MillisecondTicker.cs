using System.Diagnostics.CodeAnalysis;
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
  private CallbackDelegate _callbackDelegate;

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
  
  /// <summary>
  ///   Whether the ticker is running.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
#pragma warning disable CA1822
  public bool IsRunning => ticker_is_running() == 1;
#pragma warning restore CA1822
  
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
  ///   Rust function to stop the ticker.
  /// </summary>
  /// <remarks>
  ///   Handling the return as a Rust/C bool looks like a big problem in C#.
  ///   So the Rust function returns 1 for true and 0 for false.
  /// </remarks>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  private static partial byte ticker_is_running();

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
    // Calling ticker_is_running() directly does not help.
    // Maintaining a separate bool, thread-safe or otherwise, does not help either.
    // if (ticker_is_running() == 1) {
    //   // if (IsRunning) {
    //   throw new InvalidOperationException("The ticker is already running.");
    // }
    //
    // If the ticker has previously been stopped, the callback delegate may have
    // been garbage collected. So we need to re-create it each time we start the ticker.
    // THIS DOES NOT WORK. The Avalonia application crashes when the ticker is started.
    _callbackDelegate = OnRustCallback;
    start_ticker((ulong)millisecondsInterval, _callbackDelegate);
  }

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  public void Stop() {
    stop_ticker();
  }

  private void OnRustCallback() {
    OnTick();
    // Keep the delegate alive to prevent garbage collection.
    GC.KeepAlive(_callbackDelegate);
  }

  /// <summary>
  ///   Delegate matching Rust's callback signature.
  ///   In this case, it's as simple as it gets.
  /// </summary>
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void CallbackDelegate();
}