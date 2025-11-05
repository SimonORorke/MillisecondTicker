using System.Diagnostics.CodeAnalysis;

namespace Simon.Tickers;

/// <summary>
///   A separate class to start the ticker and hold the callback delegate.
/// </summary>
/// <remarks>
///   To prevent the callback delegate from being garbage collected,
///   a new instance of the <see cref="Starter "/> class must created for each call of
///   the <see cref="Start" /> method.
/// </remarks>
[ExcludeFromCodeCoverage]
internal class Starter {
  private RustMethods.CallbackDelegate _callbackDelegate = null!;
  
  private bool HasStarted { get; set; }
  private Action OnTick { get; set; } = null!;

  /// <summary>
  ///   Starts the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  /// <param name="onTick">
  ///   The callback method, which will run in a separate thread.
  /// </param>
  internal void Start(int millisecondsInterval, Action onTick) {
    if (HasStarted) {
      throw new InvalidOperationException(
        "To prevent the callback delegate from being garbage collected, multiple " +
        "calls to Start are not allowed. You must create a new Starter instance and " +
        "start that.");
    }
    OnTick = onTick;
    _callbackDelegate = OnRustCallback;
    RustMethods.start_ticker((ulong)millisecondsInterval, _callbackDelegate);
    HasStarted = true;
  } 

  /// <summary>
  ///   Runs the callback method in a separate thread.
  /// </summary>
  private void OnRustCallback() {
    OnTick();
  }
}