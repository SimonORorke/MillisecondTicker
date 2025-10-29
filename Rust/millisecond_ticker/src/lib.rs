mod ticker;

use std::sync::Mutex;
use std::time::Duration;
use lazy_static::lazy_static;
use ticker::Ticker;

/// Callback function type that C# will implement
type TickCallback = extern "C" fn();

struct Data {
    ticker: Option<Mutex<Ticker>>,
}

impl Data {
    fn start_ticker(
        &mut self, interval: Duration, callback: TickCallback) {
        self.ticker = Some(Mutex::new(Ticker::new(interval)));
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

    fn ticker_is_running(&self) -> bool {
        self.ticker.is_some() && self.ticker.as_ref().unwrap().lock().unwrap().is_running()
    }
}

lazy_static! {
    static ref DATA: Mutex<Data> = Mutex::new(Data {
        ticker: None,
    });
}

/// Expose this function to C#
#[unsafe(no_mangle)]
pub extern "C" fn start_ticker(
    milliseconds_interval: u64, callback: TickCallback) {
    let mut data = DATA.lock().unwrap();
    data.start_ticker(
        Duration::from_millis(milliseconds_interval),
        callback);
}

/// Expose this function to C#
#[unsafe(no_mangle)]
pub extern "C" fn stop_ticker() {
    let mut data = DATA.lock().unwrap();
    data.stop_ticker();
}

/// Expose this function to C#.
/// Handling the return as a Rust/C bool looks like a big problem in C#.
/// So we'll just return 1 for true and 0 for false.
#[unsafe(no_mangle)]
pub extern "C" fn ticker_is_running() -> u8 {
    let data = DATA.lock().unwrap();
    if data.ticker_is_running() {
        1
    } else {
        0
    }
}
