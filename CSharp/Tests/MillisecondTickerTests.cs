using System.Diagnostics;
using NUnit.Framework;

namespace Simon.Tickers.Tests;

/// <summary>
///   Results are variable. So, rather than asserting, results are just written to the
///   test outputs.
///   The <see cref="SleepWhileTicking" /> and <see cref="CountTicks" /> tests show that
///   the Rust millisecond ticker is very accurate.
///   The <see cref="Delay" /> and <see cref="Sleep" /> tests show that
///   a millisecond ticker made with C# code would be unreliable and often hopelessly
///   slow. It may not be obvious for Delay here, but the same test in an application
///   rather than NUnit is very slow.
/// </summary>
[TestFixture]
public class TickerTests {
  private const int RustTestMilliseconds = 10;
  // private const int RustTestMilliseconds = 1000 * 60; // 1 minute 
  private long _tickCount; // Thread safe counter.

  [Test]
  public async Task CountTicks() {
    await TestContext.Progress.WriteLineAsync(
      $"Test TickerTests.CountTicks: Running test for {RustTestMilliseconds} ticks.");
    Interlocked.Exchange(ref _tickCount, 0);
    var ticker = new MillisecondTicker(OnTick);
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    ticker.Start(1);
    while (true) {
      await Task.Delay(1);
      long tickCount = Interlocked.Read(ref _tickCount);
      if (tickCount >= RustTestMilliseconds) {
        break;
      }
    }
    // For the most accurate timing, stop the stopwatch before calling Stop() on the
    // ticker.
    stopwatch.Stop(); 
    ticker.Stop();
    await TestContext.Progress.WriteLineAsync(
      $"    Elapsed milliseconds on Stop = {stopwatch.ElapsedMilliseconds}.");
  }

  [Test]
  public async Task Delay() {
    const int expectedMilliseconds = 10;
    Console.WriteLine(
      $"Testing Delay, expecting {expectedMilliseconds} milliseconds.");
    int count = 0;
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    while (count < expectedMilliseconds) {
      await Task.Delay(1);
      count++;
    }
    stopwatch.Stop();
    Console.WriteLine(
      $"Tested Delay, actual was {stopwatch.ElapsedMilliseconds} milliseconds.");
  }

  [Test]
  public void Sleep() {
    const int expectedMilliseconds = 10;
    Console.WriteLine(
      $"Testing Sleep, expecting {expectedMilliseconds} milliseconds.");
    int count = 0;
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    while (count < expectedMilliseconds) {
      Thread.Sleep(1);
      count++;
    }
    stopwatch.Stop();
    Console.WriteLine(
      $"Tested Sleep, actual was {stopwatch.ElapsedMilliseconds} milliseconds.");
  }

  [Test]
  public void SleepWhileTicking() {
    TestContext.Progress.WriteLine(
      $"Test TickerTests.SleepWhileTicking: Running test for {RustTestMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    var ticker = new MillisecondTicker(OnTick);
    // Start a stopwatch, as that will be more accurate than Thread.Sleep().
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    ticker.Start(1);
    Thread.Sleep(RustTestMilliseconds);
    // For the most accurate timing, stop the stopwatch before calling Stop() on the
    // ticker.
    stopwatch.Stop();
    ticker.Stop();
    long tickCount1 = Interlocked.Read(ref _tickCount);
    // Check that Stop works.
    const int millisecondsWaitAfterStop = 5;
    Thread.Sleep(millisecondsWaitAfterStop); 
    long tickCount2 = Interlocked.Read(ref _tickCount);
    TestContext.Progress.WriteLine(
      $"    Tick count on Stop = {tickCount1}. " +
      $"Stopwatch: {stopwatch.ElapsedMilliseconds} milliseconds.");
    // There is usually 1 more tick after calling Stop() on the ticker.
    TestContext.Progress.WriteLine(
      $"    Tick count after {millisecondsWaitAfterStop} milliseconds = {tickCount2}.");
  }
  
  private void OnTick() {
    Interlocked.Increment(ref _tickCount);
  }
}