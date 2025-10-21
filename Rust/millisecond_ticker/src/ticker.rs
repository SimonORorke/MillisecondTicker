use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread;
use std::time::{Duration};

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
        running.store(true, Ordering::SeqCst);
        thread::spawn(move || {
            while running.load(Ordering::SeqCst) {
                thread::sleep(interval);
                let callback_clone = callback.clone();
                thread::spawn(move || { callback_clone() });
            }
        });
    }

    /// Stops the ticker
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}