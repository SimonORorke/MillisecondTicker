using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Simon.Tickers;

/// <summary>
///   A .Net wrapper for a precision ticker that ticks at intervals of one or more
///   milliseconds. For accuracy, the ticker is implemented in Rust.
/// </summary>
public partial class MillisecondTicker {
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

  /// <summary>
  ///   Dummy. We currently don't really need this.
  ///   But see the comment in <see cref="Stop" />.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
  private bool IsRunning { get; set; }
  
  private Action OnTick { get; }

  /// <summary>
  ///   Rust function to start the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  /// <param name="callback">Callback to run when the ticker ticks.</param>
  /// <param name="missedTickBehavior">
  ///   The behavior required when a tick is missed.
  /// </param>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  private static partial void start_ticker(
    ulong millisecondsInterval,
    CallbackDelegate callback, byte missedTickBehavior);

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
  /// <param name="missedTickBehavior">
  ///   The behavior required when a tick is missed. The default is
  ///   <see cref="MissedTickBehavior.Burst" />: tick as fast as possible until
  ///   caught up.
  /// </param>
  public void Start(int millisecondsInterval, 
    MissedTickBehavior missedTickBehavior = MissedTickBehavior.Burst) {
    if (millisecondsInterval < 1) {
      throw new ArgumentException(
        $"{nameof(millisecondsInterval)} {millisecondsInterval} is invalid. " +
        $"It must be positive.");
    }
    start_ticker((ulong)millisecondsInterval, _callbackDelegate, 
      (byte)missedTickBehavior);
    IsRunning = true;
  }

  /// <summary>
  ///   Stops the ticker.
  /// </summary>
  public void Stop() {
    stop_ticker();
    // We currently don't really need IsRunning. But accessing it here prevents the IDE
    // from complaining about this method not being static.
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