use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::{Duration};
use lazy_static::lazy_static;
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
        // let spinner = SpinWait::new();
        running.store(true, Ordering::SeqCst);
        RUNTIME.spawn(async move {
            while running.load(Ordering::SeqCst) {
                // thread::sleep(interval);
                // spin_sleep is the steadiest, which is what we want.
                spin_sleep::sleep(interval);
                // SpinWait keeps closest to elapsed/system time and uses 100% of one CPU core.
                // let interval_start = Instant::now();
                // spinner.spin_until(|| interval_start.elapsed() >= interval);
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
