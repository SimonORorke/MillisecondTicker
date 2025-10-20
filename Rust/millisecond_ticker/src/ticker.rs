use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use tokio::time::{Duration, MissedTickBehavior};
use tokio::time::interval;

/// ============================================
/// Async Interval Ticker with Start/Stop Control
/// ============================================
/// Key Features:
///
/// Start/Stop anytime - fully controllable
/// Thread-safe - uses AtomicBool for the running flag
/// Non-blocking - runs in background Tokio task
/// NOT CURRENTLY REUSABLE - must create a new instance before starting a timer.
///
/// You can run multiple timers concurrently, and each callback runs on the Tokio runtime.
/// Perfect for periodic tasks like heartbeats, polling, or game loops!
pub struct Ticker {
    interval: Duration,
    missed_tick_behavior: MissedTickBehavior,
    running: Arc<AtomicBool>,
    join_handle: Option<tokio::task::JoinHandle<()>>,
}

impl Ticker {
    pub fn new(interval: Duration, missed_tick_behavior: MissedTickBehavior) -> Self {
        Self {
            interval,
            missed_tick_behavior,
            running: Arc::new(AtomicBool::new(false)),
            join_handle: None,
        }
    }

    /// Start the ticker with a callback
    pub fn start<F>(&mut self, callback: F)
    where
        F: Fn() + Send + Clone + 'static,
    {
        let running = self.running.clone();
        let interval_period = self.interval;
        let missed_tick_behavior = self.missed_tick_behavior;
        running.store(true, Ordering::SeqCst);
        self.join_handle = Some(crate::RUNTIME.spawn(async move {
            let mut ticker = interval(interval_period);
            ticker.set_missed_tick_behavior(missed_tick_behavior);
            while running.load(Ordering::SeqCst) {
                ticker.tick().await;
                let callback_clone = callback.clone();
                crate::RUNTIME.spawn(async move { callback_clone() });
            }
        }));
    }

    // /// Check if the ticker is running
    // pub fn is_running(&self) -> bool {
    //     self.running.load(Ordering::SeqCst)
    // }

    /// Stop the ticker
    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }

    // /// Wait for the ticker to complete
    // pub async fn wait(self) {
    //     if self.join_handle.is_some() {
    //         let _ = self.join_handle.unwrap().await;
    //     }
    // }
}