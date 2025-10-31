# Millisecond Ticker: High Resolution Ticker for .Net using Rust

The `MillisecondTicker` class provides a precision ticker that ticks at intervals of one or more milliseconds. It has been tested on Windows but should also work on macOS and Linux. For accuracy, the ticker is actually implemented as a Rust library, for which `MillisecondTicker` is a .Net wrapper. This is a steady ticker, which means the priority is to tick at equal intervals rather than for the total duration of a sequence of ticks to equal the expected elapsed time. For example, after 600,000 1-millisecond ticks, the elapsed time will probably not be exactly ten minutes.

## Usage

The Rust library (**millisecond_ticker.dll** in Windows) must be copied to executable folders, including for any test projects.  To save .Net developers the need to compile the Rust library, the current **millisecond_ticker.dll** can be found in the [CSharp/RustLibrary](CSharp/RustLibrary) folder.  *I would be happy to include the macOS and Linux libraries there, if provided by contributors.* Copying the Rust library to the executable folder can be added to an application/test project file, for example:

```xml
<ItemGroup>
    <!-- Copy the Rust library, if changed, to the current output folder. -->
    <Content Include="..\..\..\Simon.MillisecondTicker\CSharp\RustLibrary\millisecond_ticker.dll">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

If you amend the Rust library, for Windows the latest  **millisecond_ticker.dll** file may be copied to the RustLibrary folder and to the executable folders in the `Simon.Tickers` C# solution with the [CSharp/CopyRustDll.cmd](CSharp/CopyRustDll.cmd ) command file.

Coding usage of `MillisecondTicker` is straightforward.  A tick callback method, which will be executed in a separate thread, is specified in the constructor.  The ticker is (obviously) started and stopped with the `Start` method, where the tick interval in milliseconds is specified, and the `Stop` method. For usage examples, please refer to the [CSharp/Example](CSharp/Example) and [CSharp/Tests](CSharp/Tests) projects.  `MillisecondTicker` implements an `IMillisecondTicker` interface, which allows the ticker to be mocked in tests.

## Rust Ticker Options Considered

To find the optimum tick source, I tested options available in Rust. The solution needed to be cross-platform, steady and accurate. Demand on CPU resources was also a consideration. `std::thread::sleep` is steady but hopelessly slow.  `SteadyClock` with `SpinWait` is steady and very accurate.  However, it is quite demanding on CPU resources, as `SpinWait` uses the full capacity of of one CPU core. The option finally selected for implementation, `spin_sleep::sleep`, is steady and only very slightly less accurate than `SteadyClock` with `SpinWait` but much less demanding on CPU resources. As its name suggests, `spin_sleep::sleep` achieves its CPU resource economy by a combination of sleeping and spinning.  For details of the options and their pros and cons, see the documentation at the top of [Rust/millisecond_ticker/src/ticker.rs](Rust/millisecond_ticker/src/ticker.rs). Test results for the three options are in the [Benchmark Test Results](Benchmark%20Test%20Results) folder. For the interpretation of the test results, see the documentation in [CSharp/Tests/MillisecondTickerTests.cs](CSharp/Tests/MillisecondTickerTests.cs).

In Windows, the durations of the first two ticks are usually inaccurate. This is true of all the Rust options tested. However, the total duration of the two ticks is accurate, except where the specified interval is 1-millisecond or close to it. This initial inaccuracy may be trivial in an application.
