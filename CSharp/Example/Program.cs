using System.Diagnostics.CodeAnalysis;

namespace Simon.Tickers.Example;

/// <summary>
///   <see cref="MillisecondTicker" /> usage example."/>
/// </summary>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal class Program {
  private static long _tickCount; // Thread safe counter.

  private static async Task Main() {
    const int maxTickCount = 3;
    const int millisecondsCheck = 300;
    const int millisecondsInterval = 1000;
    // Specify a method that will be called every time the ticker ticks.
    var ticker = new MillisecondTicker(OnTick);
    Console.WriteLine(
      "Starting MillisecondTicker. There will be 3 ticks, 1 second apart.");
    ticker.Start(millisecondsInterval);
    while (true) {
      await Task.Delay(millisecondsCheck);
      // The OnTick method is called in a separate thread.
      // So we use a thread safe counter to increment and check the tick count.
      long tickCount = Interlocked.Read(ref _tickCount);
      if (tickCount >= maxTickCount) {
        break;
      }
    }
    ticker.Stop();
    Console.WriteLine("Stopped MillisecondTicker");
  }

  /// <summary>
  ///   This method will be called every time the ticker ticks.
  ///   It runs in a separate thread.
  /// </summary>
  private static void OnTick() {
    long tickCount = Interlocked.Increment(ref _tickCount);
    Console.WriteLine($"Tick {tickCount}");
  }
}