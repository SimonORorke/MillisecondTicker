using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Simon.Tickers;

/// <summary>
///   Having the Rust methods in this separate static class rather than in the
///   <see cref="MillisecondTicker" /> class makes the difference between the ticker
///   working and not working in the Avalonia application, even though the problem could
///   not be reproduced in a console application or the unit tests.
///   The reason the fix works is unclear, but may have something to do with 
///   preventing the callback delegate from being garbage collected, which was happening
///   in the Avalonia application at one point.
/// </summary>
internal static partial class RustMethods {
  /// <summary>
  ///   Rust function to start the ticker.
  /// </summary>
  /// <param name="millisecondsInterval">Milliseconds between ticks.</param>
  /// <param name="callback">Callback to run when the ticker ticks.</param>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  internal static partial void start_ticker(
    ulong millisecondsInterval,
    CallbackDelegate callback);

  /// <summary>
  ///   Rust function to stop the ticker.
  /// </summary>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  internal static partial void stop_ticker();

  /// <summary>
  ///   Rust function to return whether the ticker is running.
  /// </summary>
  /// <remarks>
  ///   Handling the return as a Rust/C bool looks like a big problem in C#.
  ///   So the Rust function returns 1 for true and 0 for false.
  /// </remarks>
  [LibraryImport("millisecond_ticker")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  internal static partial byte ticker_is_running();

  /// <summary>
  ///   Delegate matching Rust's callback signature.
  ///   In this case, it's as simple as it gets.
  /// </summary>
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate void CallbackDelegate();
}