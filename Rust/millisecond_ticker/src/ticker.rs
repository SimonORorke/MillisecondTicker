use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Duration;

/// A steady ticker that asynchronously calls a callback on ticking
/// and can be started and stopped.
/// Timer: spin_sleep::sleep
///     Benchmark test with 1-ms interval specified:
///         Average actual interval: 1.00196 ms
///         CPU usage: 2-3%
/// Other steady timers tested and commented out:
/// SteadyClock with SpinWait
///     Benchmark test with 1-ms interval specified:
///         Average actual interval: 1.00175 ms
///         CPU usage: 4-9% (Uses the whole of one CPU core.)
/// std::thread::sleep
///     Benchmark test with 1-ms interval specified:
///         Average actual interval: 1.51934 ms
///         CPU usage: 0-3%
/// Benchmark tests were run on a Windows PC with
/// an AMD Ryzen 9 5900X 12-Core processor.
pub struct Ticker {
    interval: Duration,
    running: Arc<AtomicBool>,
}

impl Ticker {
    pub fn new(interval: Duration) -> Self {
        if interval.as_millis() < 1 {
            panic!("Interval must be at least 1 millisecond.");
        }
        Self {
            interval,
            running: Arc::new(AtomicBool::new(false)),
        }
    }

    /// Starts the ticker with a callback.
    pub fn start<F>(&mut self, callback: F)
    where
        F: Fn() + Send + Clone + 'static,
    {
        let running = self.running.clone();
        if running.load(Ordering::SeqCst) {
            panic!("The ticker is already running.");
        }
        let interval = self.interval;
        // let spinner = SpinWait::new();
        running.store(true, Ordering::SeqCst);
        // We cannot use std::thread::spawn here. If an Avalonia C# application is run
        // in an IDE (JetBrains Rider or Visual Studio), Rust panics when attempting to spawn,
        // with this error message:
        //     failed to spawn thread: Os { code: 5, kind: PermissionDenied, message: "Access is denied." }
        // Rayon::spawn does not have this problem, as it uses a thread pool that Rayon has created
        // in advance.
        // The type of spawn used has not made any measurable difference to performance.
        rayon::spawn(move || {
            while running.load(Ordering::SeqCst) {
                // std::thread::sleep(interval);
                spin_sleep::sleep(interval);
                // let interval_start = SteadyClock::now();
                // spinner.spin_until(|| (SteadyClock::now() - interval_start) >= interval);
                let callback_clone = callback.clone();
                rayon::spawn(move || { callback_clone() });
            }
        });
    }

    /// Stops the ticker.
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}
