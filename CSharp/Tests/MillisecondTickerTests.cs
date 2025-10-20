using System.Diagnostics;
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

  private StringWriter IntervalLog { get; } = new StringWriter();
  private int IntervalMilliseconds { get; set; }
  private MissedTickBehavior MissedTickBehavior { get; set; }
  private Stopwatch Stopwatch { get; } = new Stopwatch();

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
  ///   to get an idea of how steady they are. No data is written to the log
  ///   if followed by other tests.  So this must be run individually, hence the
  ///   [Explicit] attribute.
  /// </summary>
  /// <remarks>
  ///   After measuring the actual intervals between ticks with various specified
  ///   intervals, I conclude that Interval is adequate for what I need.
  ///   I found that the absolute differences between expected and measured intervals
  ///   increased little as I varied the specified interval duration.
  ///   So I think the differences must be mostly due to test artefacts.
  ///   I think I should be able to get more consistent tick durations if I were to
  ///   set MissedTickBehavior to Delay. That would be more like a steady clock.
  ///   However, on due consideration, I've decided that it's best for my musical purpose
  ///   to leave MissedTickBehavior at its default, Burst. That will speed up ticks,
  ///   if necessary, to keep the elapsed time of all ticks as expected while still
  ///   notifying every tick. That would be more like a system clock.
  /// </remarks>
  [Test, Explicit]
  public void MeasureTickIntervals() {
    IntervalMilliseconds = 1;
    MissedTickBehavior = MissedTickBehavior.Burst;
    // MissedTickBehavior = MissedTickBehavior.Delay;
    // MissedTickBehavior = MissedTickBehavior.Skip;
    // The first tick will happen "immediately", so its interval will be short.
    // However, as we will sleep for 10 intervals plus a millisecond, we will actually
    // get (at least) 11 measurements, so 10 that are relevant,
    // unless MissedTickBehavior is Delay. 
    int totalMilliseconds = IntervalMilliseconds * 10;
    // int totalMilliseconds = IntervalMilliseconds * 1000;
    TestContext.Progress.WriteLine(
      $"Testing MeasureTickIntervals for {totalMilliseconds} milliseconds.");
    var ticker = new MillisecondTicker(OnTickMeasureInterval);
    Stopwatch.Restart();
    ticker.Start(IntervalMilliseconds, MissedTickBehavior);
    Thread.Sleep(totalMilliseconds + 1);
    ticker.Stop();
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
      $"    Expected {IntervalMilliseconds} milliseconds; " +
      $"actual {Stopwatch.ElapsedMilliseconds} milliseconds; " +
      $"missed tick behavior {MissedTickBehavior}.");
    Stopwatch.Restart();
  }
}