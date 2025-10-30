using System.Diagnostics.CodeAnalysis;

namespace Simon.Tickers;

[ExcludeFromCodeCoverage]
internal class Starter {
  private RustFunctions.CallbackDelegate _callbackDelegate = null!;
  
  private bool HasStarted { get; set; }
  private Action OnTick { get; set; } = null!;

  internal void Start(int millisecondsInterval, Action onTick) {
    if (HasStarted) {
      throw new InvalidOperationException(
        "The ticker has been started before. Multiple calls to Start are not " +
        "allowed. You must create a new Starter instance and start that.");
    }
    OnTick = onTick;
    _callbackDelegate = OnRustCallback;
    RustFunctions.start_ticker((ulong)millisecondsInterval, _callbackDelegate);
    HasStarted = true;
  } 

  private void OnRustCallback() {
    OnTick();
    // Keep the delegate alive to prevent garbage collection.
    GC.KeepAlive(_callbackDelegate);
  }
}