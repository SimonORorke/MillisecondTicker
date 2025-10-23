using System.Diagnostics;
using NUnit.Framework;

namespace Simon.Tickers.Tests;

/// <summary>
///   Results are variable. So, rather than asserting, results are just written to the
///   test output.  Summary of test results in Windows.
///   The <see cref="ZMeasureTickIntervals" /> test shows that the Rust millisecond
///   ticker is very steady, though the first one or two ticks can be shaky.  Also,
///   see the remarks for <see cref="ZMeasureTickIntervals" /> method.
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
  ///   Sleep while the ticker callback is counting ticks.
  ///   This test will give the most accurate
  ///   measurement of the total elapsed time after the ticker has stopped. It also
  ///   provides an opportunity to monitor the ticker's impact on CPU usage.
  ///   CPU usage.
  ///   Spin: 4-19%, typical 4-9%.
  ///   Total elapsed after 10 minutes sleep.
  ///   Spin:
  ///   Elapsed milliseconds
  ///     Expected: total tick count 598875 * interval 1 = 598875
  ///     Measured: 600012.
  ///   So the 598875 ticks were a total of 1.137 seconds fast after 10 minutes. 
  /// </summary>
  [Test]
  public void CountTicks() {
    const int sleepMilliseconds = 1000 * 600 + 1; // 10 minutes and 1 millisecond.
    IntervalMilliseconds = 1;
    TestContext.Progress.WriteLine(
      $"SleepWhileTicking: testing {IntervalMilliseconds}-millisecond tick " +
      $"interval. Sleeping for {sleepMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    var totalStopwatch = new Stopwatch();
    totalStopwatch.Start();
    Ticker = new MillisecondTicker(OnTickIncrementTickCount);
    Ticker.Start(IntervalMilliseconds);
    Thread.Sleep(sleepMilliseconds);
    // For greatest accuracy, stop the stopwatch before calling Stop() on the ticker.
    totalStopwatch.Stop();
    Ticker.Stop();
    long totalTickCount = Interlocked.Read(ref _tickCount);
    long expectedMilliseconds = totalTickCount * IntervalMilliseconds;
    decimal fastSeconds = 
      (decimal)(totalStopwatch.ElapsedMilliseconds - totalTickCount) / 1000;
    TestContext.Progress.WriteLine($"Total tick count: {totalTickCount}.");
    TestContext.Progress.WriteLine("Elapsed milliseconds");
    TestContext.Progress.WriteLine(
      $"    Expected: total tick count {totalTickCount} " +
      $"* interval {IntervalMilliseconds} = {expectedMilliseconds}");
    TestContext.Progress.WriteLine($"    Measured: {totalStopwatch.ElapsedMilliseconds}.");
    TestContext.Progress.WriteLine(
      $"So the {totalTickCount} ticks were a total of {fastSeconds} seconds fast " +
      $"after 10 minutes sleep.");
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
    Ticker = new MillisecondTicker(OnTickMeasureTickInterval);
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
  ///   <para>
  ///     How to interpret the results. The smallest duration that can be measured in
  ///     Windows is 1 millisecond and <see cref="Stopwatch" /> does not round up
  ///     milliseconds.  So, if the ticker is performing as expected, the measured
  ///     duration of each tick should be equal to or one millisecond less than the
  ///     specified interval.  For example, if the specified interval is 1 millisecond,
  ///     tick durations are expected to be measured at 0 or 1 milliseconds. That does
  ///     not mean that those ticks whose duration was measured at 0 milliseconds actually
  ///     occured almost immediately after the previous tick. 
  ///   </para>
  /// </remarks>
  [Test]
  public void ZMeasureTickIntervals() {
    Ticker = new MillisecondTicker(OnTickMeasureTickInterval);
    // The results from any one of these tests fit within the test output size limit
    // ZMeasureTickIntervals(1, 180);
    // ZMeasureTickIntervals(10, 180);
    // ZMeasureTickIntervals(100, 180);
    // ZMeasureTickIntervals(1000, 180);

    // The combined results from all of these tests fit within the test output size limit.
    ZMeasureTickIntervals(1, 40);
    ZMeasureTickIntervals(10, 40);
    ZMeasureTickIntervals(100, 40);
    ZMeasureTickIntervals(1000, 40);
  }

  private void ZMeasureTickIntervals(
    int intervalMilliseconds, int waitFactor) {
    IntervalMilliseconds = intervalMilliseconds;
    int sleepMilliseconds = IntervalMilliseconds * waitFactor + 1;
    TestContext.Progress.WriteLine(
      $"ZMeasureTickIntervals: testing {IntervalMilliseconds}-millisecond tick " +
      $"interval. Sleeping for {sleepMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    IntervalLog = new StringWriter();
    var totalStopwatch = new Stopwatch();
    totalStopwatch.Start();
    Stopwatch.Restart();
    Ticker.Start(IntervalMilliseconds);
    Thread.Sleep(sleepMilliseconds);
    // For greatest accuracy, stop the stopwatches before calling Stop() on the ticker.
    totalStopwatch.Stop();
    Stopwatch.Stop(); 
    Ticker.Stop();
    long totalTickCount = Interlocked.Read(ref _tickCount);
    long expectedMilliseconds = totalTickCount * IntervalMilliseconds;
    TestContext.Progress.Write(IntervalLog.ToString());
    TestContext.Progress.WriteLine("************************************************");
    TestContext.Progress.WriteLine($"Slept for {sleepMilliseconds} milliseconds.");
    TestContext.Progress.WriteLine($"Total tick count: {totalTickCount}.");
    TestContext.Progress.WriteLine("Elapsed milliseconds:");
    TestContext.Progress.WriteLine(
      $"    Expected total tick count {totalTickCount} " +
      $"* interval {IntervalMilliseconds} = {expectedMilliseconds}");
    TestContext.Progress.WriteLine($"    Measured {totalStopwatch.ElapsedMilliseconds}.");
    TestContext.Progress.WriteLine("************************************************");
  }

  /// <summary>
  ///   Tick callback running in a separate thread.
  /// </summary>
  private void OnTickIncrementTickCount() {
    Interlocked.Increment(ref _tickCount);
  }

  /// <summary>
  ///   Tick callback running in a separate thread.
  /// </summary>
  private void OnTickMeasureTickInterval() {
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