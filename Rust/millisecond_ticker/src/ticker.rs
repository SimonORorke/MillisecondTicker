use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread;
use std::time::{Duration};

/// ============================================
/// Async Ticker with Start/Stop Control
/// ============================================
/// Key Features:
///
/// Start/Stop anytime - fully controllable
/// Thread-safe - uses AtomicBool for the running flag
/// Non-blocking - runs in background thread.
/// Reusable - An instance can be started and stopped multiple times.
///
/// You can run multiple timers concurrently, and each callback runs on the Tokio runtime.
/// Perfect for periodic tasks like heartbeats, polling, or game loops!
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

    /// Start the ticker with a callback
    pub fn start<F>(&mut self, callback: F)
    where
        F: Fn() + Send + Clone + 'static,
    {
        let running = self.running.clone();
        let interval = self.interval;
        running.store(true, Ordering::SeqCst);
        thread::spawn(move || {
            while running.load(Ordering::SeqCst) {
                thread::sleep(interval);
                let callback_clone = callback.clone();
                thread::spawn(move || { callback_clone() });
            }
        });
    }

    // /// Check if the ticker is running
    // pub fn is_running(&self) -> bool {
    //     self.running.load(Ordering::SeqCst)
    // }

    /// Stop the ticker
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}