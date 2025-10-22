use howlong::{Clock, SteadyClock};
use lazy_static::lazy_static;
use spinwait::SpinWait;
use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Duration;
use tokio::runtime::{self, Runtime};

lazy_static! {
    /// Tokio runtime suitable for use in a Foreign Function Interface (FFI) library.
    static ref RUNTIME: Runtime = runtime::Builder::new_multi_thread()
        .build()
        .unwrap();
}

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
        let spinner = SpinWait::new();
        running.store(true, Ordering::SeqCst);
        // I'd rather use std::thread::spawn here. However, if I run the Avalonia C# application
        // in an IDE (JetBrains Rider or Visual Studio), Rust panics when attempting to spawn,
        // with this error message:
        //     failed to spawn thread: Os { code: 5, kind: PermissionDenied, message: "Access is denied." }
        // I can see the error message in Rider but have not been able to find where to see it
        // in Visual Studio.
        RUNTIME.spawn(async move {
            while running.load(Ordering::SeqCst) {
                let interval_start = SteadyClock::now();
                spinner.spin_until(|| (SteadyClock::now() - interval_start) >= interval);
                let callback_clone = callback.clone();
                RUNTIME.spawn(async move { callback_clone() });
            }
        });
    }

    /// Stops the ticker
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}
