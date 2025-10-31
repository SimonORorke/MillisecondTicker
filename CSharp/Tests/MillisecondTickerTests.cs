using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Simon.Tickers.Tests;

/// <summary>
///   Results are variable. So, rather than asserting, results are just written to the
///   test output.  Summary of test results in Windows.
///   The <see cref="AverageTickInterval" /> test shows that, as the
///   <see cref="MillisecondTicker" /> ticks steadily rather than trying to match elapsed
///   time, the average actual interval is very slightly longer than the specified
///   interval.
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
  
  [Test]
  public void AllowRestart() {
    Ticker = new MillisecondTicker(OnTickMeasureTickInterval);
    Ticker.Start(1);
    Thread.Sleep(10);
    Ticker.Stop();
    Assert.DoesNotThrow(() => Ticker.Start(1));
    Thread.Sleep(10);
    Ticker.Stop();
  }

  [Test]
  public void AlreadyRunning() {
    Ticker = new MillisecondTicker(OnTickMeasureTickInterval);
    Assert.That(!Ticker.IsRunning);
    Ticker.Start(1);
    Assert.That(Ticker.IsRunning);
    Assert.Throws<InvalidOperationException>(() => Ticker.Start(1));
    Ticker.Stop();
  }

  /// <summary>
  ///   Measures the average actual tick interval over 10 minutes for a specified tick
  ///   interval of 1 millisecond. This test also provides an opportunity to monitor the
  ///   ticker's impact on CPU usage.
  /// </summary>
  [Test, Explicit, ExcludeFromCodeCoverage]
  public void AverageTickInterval() {
    const int sleepMilliseconds = 1000 * 600; // 10 minutes.
    // const int sleepMilliseconds = 1000 * 10; // 10 seconds.
    IntervalMilliseconds = 1;
    TestContext.Progress.WriteLine("AverageTickInterval Test");
    TestContext.Progress.WriteLine(
      $"Specified tick interval: {IntervalMilliseconds} millisecond");
    TestContext.Progress.WriteLine(
      $"Sleeping for {sleepMilliseconds} milliseconds = " +
      $"{sleepMilliseconds / 60000} minutes.");
    Interlocked.Exchange(ref _tickCount, 0);
    var totalStopwatch = new Stopwatch();
    TestContext.Progress.WriteLine($"Started at {DateTime.Now:HH:mm:ss}");
    totalStopwatch.Start();
    Ticker = new MillisecondTicker(OnTickIncrementTickCount);
    Ticker.Start(IntervalMilliseconds);
    Thread.Sleep(sleepMilliseconds);
    // For greatest accuracy, stop the stopwatch before calling Stop() on the ticker.
    totalStopwatch.Stop();
    Ticker.Stop();
    TestContext.Progress.WriteLine($"Stopped at {DateTime.Now:HH:mm:ss}");
    long totalTickCount = Interlocked.Read(ref _tickCount);
    float averageIntervalMilliseconds = (float)Math.Round(
      (float)totalStopwatch.ElapsedMilliseconds / totalTickCount, 5);
    TestContext.Progress.WriteLine(
      $"Actual elapsed milliseconds: {totalStopwatch.ElapsedMilliseconds}");
    TestContext.Progress.WriteLine($"Total tick count: {totalTickCount}");
    TestContext.Progress.WriteLine("Average tick interval");
    TestContext.Progress.WriteLine(
      $"    Expected: {IntervalMilliseconds} millisecond");
    TestContext.Progress.WriteLine(
      $"    Actual: {totalStopwatch.ElapsedMilliseconds} / {totalTickCount} = " +
      $"{averageIntervalMilliseconds} milliseconds");
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

  [Test, Explicit, ExcludeFromCodeCoverage]
  public void KeepCallbackDelegateAlive() {
    IntervalMilliseconds = 1000; // 1 second.
    // IntervalMilliseconds = 1000 * 60; // 1 minute.
    int sleepMilliseconds = IntervalMilliseconds * 2 + 100;
    Ticker = new MillisecondTicker(OnTickIncrementTickCount);
    TestContext.Progress.WriteLine(
      $"KeepCallbackDelegateAlive: testing {IntervalMilliseconds}-millisecond tick " +
      $"interval.");
    SleepAndMeasure();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    GC.WaitForFullGCComplete();
    TestContext.Progress.WriteLine("Testing restart after garbage collection.");
    // TestContext.Progress.WriteLine(
    //   $"Sleeping for {IntervalMilliseconds} milliseconds before testing restart.");
    // Thread.Sleep(IntervalMilliseconds);
    // TestContext.Progress.WriteLine("Testing restart.");
    SleepAndMeasure();
    return;

    void SleepAndMeasure() {
      TestContext.Progress.WriteLine($"Sleeping for {sleepMilliseconds} milliseconds.");
      Interlocked.Exchange(ref _tickCount, 0);
      Stopwatch.Restart();
      Ticker.Start(IntervalMilliseconds);
      Thread.Sleep(sleepMilliseconds);
      // For greatest accuracy, stop the stopwatch before calling Stop() on the ticker.
      Stopwatch.Stop();
      Ticker.Stop();
      long tickCount = Interlocked.Read(ref _tickCount);
      TestContext.Progress.WriteLine("************************************************");
      TestContext.Progress.WriteLine(
        $"Elapsed milliseconds measured: {Stopwatch.ElapsedMilliseconds}.");
      TestContext.Progress.WriteLine($"Total tick count: {tickCount}.");
      TestContext.Progress.WriteLine("************************************************");
    }
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
    // The combined results from all of these tests fit within the test output size limit.
    ZMeasureTickIntervals(1, 30);
    ZMeasureTickIntervals(10, 30);
    ZMeasureTickIntervals(100, 30);
    ZMeasureTickIntervals(1000, 30);
    
    // The results from any one of these tests fit within the test output size limit
    // ZMeasureTickIntervals(1, 180);
    // ZMeasureTickIntervals(10, 180);
    // ZMeasureTickIntervals(100, 180);
    // ZMeasureTickIntervals(1000, 180);
  }

  private void ZMeasureTickIntervals(
    int intervalMilliseconds, int waitFactor) {
    IntervalMilliseconds = intervalMilliseconds;
    int sleepMilliseconds = IntervalMilliseconds * waitFactor;
    TestContext.Progress.WriteLine(
      $"ZMeasureTickIntervals: testing {IntervalMilliseconds}-millisecond tick " +
      $"interval.");
    TestContext.Progress.WriteLine($"Sleeping for {sleepMilliseconds} milliseconds.");
    Interlocked.Exchange(ref _tickCount, 0);
    IntervalLog = new StringWriter();
    Ticker = new MillisecondTicker(OnTickMeasureTickInterval);
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
    TestContext.Progress.WriteLine("Elapsed milliseconds");
    TestContext.Progress.WriteLine(
      $"    Expected: total tick count {totalTickCount} " +
      $"* interval {IntervalMilliseconds} = {expectedMilliseconds}");
    TestContext.Progress.WriteLine($"    Measured: {totalStopwatch.ElapsedMilliseconds}.");
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