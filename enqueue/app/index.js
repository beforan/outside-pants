// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");

const run = require('./run');
const { intervalMs } = require("./const");

// busy flag
let isRunning;

const main = () => {
  if (isRunning) return;

  isRunning = true;
  run().then(() => isRunning = false);
}

// run at startup
main();

// then set a repeating call of our actual work
setInterval(
  main,
  intervalMs);
