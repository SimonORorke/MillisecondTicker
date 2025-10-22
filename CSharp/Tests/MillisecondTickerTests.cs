using System.Diagnostics;
using NUnit.Framework;

namespace Simon.Tickers.Tests;

/// <summary>
///   Results are variable. So, rather than asserting, results are just written to the
///   test output.  Summary of test results in Windows.
///   The <see cref="ZMeasureTickIntervals" /> test shows that the Rust millisecond
///   ticker is very steady, though the first one or two ticks can be shaky.
///   Elapsed times after many ticks are less than expected for short tick intervals
///   but about right for tick intervals of around 100 milliseconds or more.
///   The <see cref="Delay" /> and <see cref="Sleep" /> tests show that
///   a millisecond ticker made with C# code would be unreliable and often hopelessly
///   slow.
/// </summary>
[TestFixture]
public class TickerTests {
  private long _tickCount; // Thread safe counter.

  private StringWriter IntervalLog { get; set; } = new StringWriter();
  private int IntervalMilliseconds { get; set; }
  private Stopwatch Stopwatch { get; } = new Stopwatch();
  private MillisecondTicker Ticker { get; set; } = null!;

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
    Ticker = new MillisecondTicker(OnTick);
    Assert.Throws<ArgumentException>(() => Ticker.Start(0));
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
  ///   Measures the intervals between ticks of the <see cref="MillisecondTicker" />,
  ///   to get an idea of how steady they are.
  /// </summary>
  /// <remarks>
  ///   Data is written to the log in a separate callback thread.
  ///   So this test must be run last in order to copy the data to the test output.
  /// </remarks>
  [Test]
  public void ZMeasureTickIntervals() {
    Ticker = new MillisecondTicker(OnTick);
    ZMeasureTickIntervals(1, 180);
    // ZMeasureTickIntervals(10, 50);
    // ZMeasureTickIntervals(100, 30);
    // ZMeasureTickIntervals(1000, 30);
  }

  private void ZMeasureTickIntervals(
    int intervalMilliseconds, int waitFactor) {
    IntervalMilliseconds = intervalMilliseconds;
    int sleepMilliseconds = IntervalMilliseconds * waitFactor + 1;
    TestContext.Progress.WriteLine(
      $"MeasureTickIntervals: testing {IntervalMilliseconds}-millisecond tick " +
      $"interval. Sleeping for {sleepMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    IntervalLog = new StringWriter();
    var totalStopwatch = new Stopwatch();
    totalStopwatch.Start();
    Stopwatch.Restart();
    Ticker.Start(IntervalMilliseconds);
    Thread.Sleep(sleepMilliseconds);
    Ticker.Stop();
    Stopwatch.Stop();
    totalStopwatch.Stop();
    long totalTickCount = Interlocked.Read(ref _tickCount);
    long expectedMilliseconds = totalTickCount * IntervalMilliseconds;
    TestContext.Progress.Write(IntervalLog.ToString());
    TestContext.Progress.WriteLine("************************************************");
    TestContext.Progress.WriteLine($"Total tick count: {totalTickCount}.");
    TestContext.Progress.WriteLine(
      $"Elapsed milliseconds: expected total tick count {totalTickCount} " +
      $"* interval {IntervalMilliseconds} = {expectedMilliseconds}; " +
      $"measured {totalStopwatch.ElapsedMilliseconds}.");
    TestContext.Progress.WriteLine("************************************************");
  }

  /// <summary>
  ///   Tick callback running in a separate thread.
  /// </summary>
  private void OnTick() {
    Stopwatch.Stop();
    Interlocked.Increment(ref _tickCount);
    // Because we are in a separate thread, we can't write directly to the test output
    // here. 
    IntervalLog.WriteLine(
      $"Tick interval milliseconds: expected {IntervalMilliseconds}; " +
      $"actual {Stopwatch.ElapsedMilliseconds}.");
    Stopwatch.Restart();
  }
}