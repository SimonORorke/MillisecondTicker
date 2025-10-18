using System.Runtime.InteropServices;

namespace Simon.Tickers;

public partial class MillisecondTicker {
  private readonly CallbackDelegate _callbackDelegate;
  
  public MillisecondTicker(Action onTick) {
    OnTick = onTick;
    // Keep delegate alive to prevent GC
    _callbackDelegate = OnRustCallback;
  }
  
  private bool IsRunning { get; set; }
  private Action OnTick { get; }

  // Import Rust functions
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
  private static partial void start_ticker(
    int millisecondsInterval, 
    CallbackDelegate callback);
  
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
  private static partial void stop_ticker();
  
  public void Start(int millisecondsInterval) {
    start_ticker(millisecondsInterval, _callbackDelegate);
    IsRunning = true;
  }
  
  public void Stop() {
    stop_ticker();
    // We currently don't really need IsRunning. But accessing it here prevents the IDE
    // from complaining about this method not being static.
    IsRunning = false;
  }

  private void OnRustCallback() {
    OnTick();
  }

  // Delegate matching Rust's callback signature
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void CallbackDelegate();
}