// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");
const redismq = require("rsmq");

const rsmq = new redismq({ host: "redis", ns: "rsmq" });

const queueName = "folderscan";

let isRunning;

const go = () =>
  rsmq.getQueueAttributes({ qname: queueName }, (err, r) => {
    if (err) {
      console.error(err);
      isRunning = false;
      return;
    }
    console.log("Got queue attributes");

    if (r.msgs === 0) {
      console.log("no messages in queue");
      isRunning = false;
      return;
    }

    //const message = rsmq.popMessage({ qname: queueName });
    isRunning = false;
  });

// set a repeating call of our actual work
setInterval(() => {
  if (!isRunning) {
    console.log("polling 'folderscan' redis queue...")
    isRunning = true;
    go();
  }
}, 10000);
