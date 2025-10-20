mod ticker;

use std::sync::Mutex;
use tokio::time::{Duration, MissedTickBehavior};
use lazy_static::lazy_static;
use tokio::runtime::{self, Runtime};
use ticker::Ticker;

/// Callback function type that C# will implement
type TickCallback = extern "C" fn();

struct Data {
    ticker: Option<Mutex<Ticker>>,
}

impl Data {
    fn start_ticker(
        &mut self, interval: Duration, callback: TickCallback,
        missed_tick_behavior: MissedTickBehavior) {
        self.ticker = Some(Mutex::new(Ticker::new(interval, missed_tick_behavior)));
        if let Some(ref ticker) = self.ticker {
            if let Ok(mut ticker) = ticker.lock() {
                ticker.start(move || {
                    callback();
                });
            }
        }
    }

    fn stop_ticker(&mut self) {
        if let Some(ref ticker) = self.ticker {
            if let Ok(ticker) = ticker.lock() {
                ticker.stop();
            }
        }
    }
}

lazy_static! {
    static ref DATA: Mutex<Data> = Mutex::new(Data {
        ticker: None,
    });

    /// Tokio runtime suitable for use in a Foreign Function Interface (FFI) library.
    pub static ref RUNTIME: Runtime = runtime::Builder::new_multi_thread()
        .enable_io() // ???
        .enable_time() // Required for Ticker.
        .build()
        .unwrap();
}

/// Expose this function to C#
#[unsafe(no_mangle)]
pub extern "C" fn start_ticker(
    milliseconds_interval: u64, callback: TickCallback, missed_tick_behavior: u8) {
    let missed_behavior_enum_value: MissedTickBehavior = match missed_tick_behavior {
        0 => MissedTickBehavior::Burst,
        1 => MissedTickBehavior::Delay,
        2 => MissedTickBehavior::Skip,
        _ => panic!("Invalid missed tick behavior {}.", missed_tick_behavior),
    };
    let mut data = DATA.lock().unwrap();
    data.start_ticker(
        Duration::from_millis(milliseconds_interval),
        callback, missed_behavior_enum_value);
}

/// Expose this function to C#
#[unsafe(no_mangle)]
pub extern "C" fn stop_ticker() {
    let mut data = DATA.lock().unwrap();
    data.stop_ticker();
}
