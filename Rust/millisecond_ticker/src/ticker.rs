// use howlong::{Clock, SteadyClock};
// use spinwait::SpinWait;
use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Duration;

/// A steady ticker that asynchronously calls a callback on ticking
/// and can be started and stopped.
pub struct Ticker {
    interval: Duration,
    running: Arc<AtomicBool>,
}

impl Ticker {
    pub fn new(interval: Duration) -> Self {
        Self {
            interval,
            running: Arc::new(AtomicBool::new(false)),
        }
    }

    /// Starts the ticker with a callback
    pub fn start<F>(&mut self, callback: F)
    where
        F: Fn() + Send + Clone + 'static,
    {
        let running = self.running.clone();
        let interval = self.interval;
        // let spinner = SpinWait::new();
        running.store(true, Ordering::SeqCst);
        // We cannot use std::thread::spawn here. If the Avalonia C# application is run
        // in an IDE (JetBrains Rider or Visual Studio), Rust panics when attempting to spawn,
        // with this error message:
        //     failed to spawn thread: Os { code: 5, kind: PermissionDenied, message: "Access is denied." }
        // Rayon::spawn does not have this problem, as it uses a thread pool that Rayon has created
        // in advance.
        rayon::spawn(move || {
            while running.load(Ordering::SeqCst) {
                spin_sleep::sleep(interval);
                // let interval_start = SteadyClock::now();
                // spinner.spin_until(|| (SteadyClock::now() - interval_start) >= interval);
                let callback_clone = callback.clone();
                rayon::spawn(move || { callback_clone() });
            }
        });
    }

    /// Stops the ticker
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}
