// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");

const run = require('./run');

// busy flag
let isRunning;

// set a repeating call of our actual work
setInterval(
  () => {
    if (isRunning) return;

    isRunning = true;
    run().then(() => isRunning = false);
  },
  1000); // every 10 seconds
