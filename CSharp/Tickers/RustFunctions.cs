using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Simon.Tickers;

internal static partial class RustFunctions {
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
  ///   Rust function to stop the ticker.
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