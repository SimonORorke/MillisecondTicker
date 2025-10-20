using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Simon.Tickers.Tests;

/// <summary>
///   Results are variable. So, rather than asserting, results are just written to the
///   test output.
///   The <see cref="SleepWhileTicking" /> and <see cref="CountTicks" /> tests show that
///   the Rust millisecond ticker is very accurate.
///   The <see cref="Delay" /> and <see cref="Sleep" /> tests show that
///   a millisecond ticker made with C# code would be unreliable and often hopelessly
///   slow. It may not be obvious for Delay here, but the same test in an application
///   rather than NUnit is very slow.
/// </summary>
[TestFixture]
public class TickerTests {
  // For more accurate timing, allowing for fixed overheads,
  // run the MillisecondTicker tests for longer.
  private const int RustTestMilliseconds = 1000 * 60; // 1 minute 
  // private const int RustTestMilliseconds = 10;

  private long _tickCount; // Thread safe counter.

  private StringWriter IntervalLog { get; set; } = null!;
  private int IntervalMilliseconds { get; set; }
  private Stopwatch Stopwatch { get; } = new Stopwatch();
  private MillisecondTicker Ticker { get; set; } = null!;


  /// <summary>
  ///   Tests the accuracy of <see cref="MillisecondTicker" /> by counting its ticks.
  /// </summary>
  [Test]
  public async Task CountTicks() {
    await TestContext.Progress.WriteLineAsync(
      $"Test TickerTests.CountTicks: Running test for {RustTestMilliseconds} ticks.");
    Interlocked.Exchange(ref _tickCount, 0);
    var ticker = new MillisecondTicker(OnTickIncrementTickCount);
    Stopwatch.Restart();
    ticker.Start(1);
    while (true) {
      await Task.Delay(1);
      long tickCount = Interlocked.Read(ref _tickCount);
      if (tickCount >= RustTestMilliseconds) {
        break;
      }
    }
    // For the most accurate timing, stop the Stopwatch before calling Stop() on the
    // ticker.
    Stopwatch.Stop();
    ticker.Stop();
    await TestContext.Progress.WriteLineAsync(
      $"    Elapsed milliseconds on Stop = {Stopwatch.ElapsedMilliseconds}.");
  }

  /// <summary>
  ///   Tests the accuracy of <see cref="Task.Delay(int)" />, for comparison with
  ///   <see cref="MillisecondTicker" />.
  /// </summary>
  [Test]
  public async Task Delay() {
    const int expectedMilliseconds = 10;
    await TestContext.Progress.WriteLineAsync(
      $"Testing Delay, expecting {expectedMilliseconds} milliseconds.");
    int count = 0;
    Stopwatch.Restart();
    while (count < expectedMilliseconds) {
      await Task.Delay(1);
      count++;
    }
    Stopwatch.Stop();
    await TestContext.Progress.WriteLineAsync(
      $"Tested Delay, actual was {Stopwatch.ElapsedMilliseconds} milliseconds.");
  }

  [Test]
  public void InvalidInterval() {
    var ticker = new MillisecondTicker(OnTickIncrementTickCount);
    Assert.Throws<ArgumentException>(() => ticker.Start(0));
  }

  /// <summary>
  ///   Measures the intervals between ticks of the <see cref="MillisecondTicker" />,
  ///   to get an idea of how steady they are.
  /// </summary>
  /// <remarks>
  ///   No data is written to the log
  ///   if followed by other tests.  So this must be run individually, hence the
  ///   [Explicit] attribute.
  /// </remarks>
  [Test, Explicit, ExcludeFromCodeCoverage]
  public void MeasureTickIntervals() {
    Ticker = new MillisecondTicker(OnTickMeasureInterval);
    MeasureTickIntervals(1, 100);
    MeasureTickIntervals(10, 100);
    // MeasureTickIntervals(100, 20);
    // MeasureTickIntervals(1000, 20);
  }

  [ExcludeFromCodeCoverage]
  private void MeasureTickIntervals(
    int intervalMilliseconds, int waitFactor) {
    IntervalMilliseconds = intervalMilliseconds;
    // The first tick will happen "immediately", so its interval will be short.
    // However, if we will sleep for 10 intervals plus a millisecond, we will actually
    // get (at least) 11 measurements, so 10 that are relevant,
    // unless MissedTickBehavior is Delay. 
    int waitMilliseconds = IntervalMilliseconds * waitFactor + 1;
    TestContext.Progress.WriteLine(
      $"MeasureTickIntervals: testing {IntervalMilliseconds} millisecond interval " +
      $"for {waitMilliseconds} milliseconds.");
    IntervalLog = new StringWriter();
    // Ticker = new MillisecondTicker(OnTickMeasureInterval);
    Stopwatch.Restart();
    Ticker.Start(IntervalMilliseconds);
    Thread.Sleep(waitMilliseconds);
    Ticker.Stop();
    Stopwatch.Stop();
    TestContext.Progress.WriteLine(IntervalLog.ToString());
  }

  /// <summary>
  ///   Tests the accuracy of <see cref="Thread.Sleep(int)" />, for comparison with
  ///   <see cref="MillisecondTicker" />.
  /// </summary>
  [Test]
  public void Sleep() {
    const int expectedMilliseconds = 10;
    TestContext.Progress.WriteLine(
      $"Testing Sleep, expecting {expectedMilliseconds} milliseconds.");
    int count = 0;
    Stopwatch.Restart();
    while (count < expectedMilliseconds) {
      Thread.Sleep(1);
      count++;
    }
    Stopwatch.Stop();
    TestContext.Progress.WriteLine(
      $"Tested Sleep, actual was {Stopwatch.ElapsedMilliseconds} milliseconds.");
  }

  /// <summary>
  ///   Tests the accuracy of <see cref="MillisecondTicker" /> by sleeping while it ticks.
  /// </summary>
  [Test]
  public void SleepWhileTicking() {
    TestContext.Progress.WriteLine(
      $"Test TickerTests.SleepWhileTicking: Running test for {RustTestMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    var ticker = new MillisecondTicker(OnTickIncrementTickCount);
    // Start a Stopwatch, as that will be more accurate than Thread.Sleep().
    Stopwatch.Restart();
    ticker.Start(1);
    Thread.Sleep(RustTestMilliseconds);
    // For the most accurate timing, stop the Stopwatch before calling Stop() on the
    // ticker.
    Stopwatch.Stop();
    ticker.Stop();
    long tickCount1 = Interlocked.Read(ref _tickCount);
    // Check that Stop works.
    const int millisecondsWaitAfterStop = 5;
    Thread.Sleep(millisecondsWaitAfterStop);
    long tickCount2 = Interlocked.Read(ref _tickCount);
    TestContext.Progress.WriteLine(
      $"    Tick count on Stop = {tickCount1}. " +
      $"Stopwatch: {Stopwatch.ElapsedMilliseconds} milliseconds.");
    // There is usually 1 more tick after calling Stop() on the ticker.
    TestContext.Progress.WriteLine(
      $"    Tick count after {millisecondsWaitAfterStop} milliseconds = {tickCount2}.");
  }

  private void OnTickIncrementTickCount() {
    Interlocked.Increment(ref _tickCount);
  }

  private void OnTickMeasureInterval() {
    Stopwatch.Stop();
    IntervalLog.WriteLine(
      $"Expected {IntervalMilliseconds} milliseconds; " +
      $"actual {Stopwatch.ElapsedMilliseconds} milliseconds.");
    Stopwatch.Restart();
  }
}