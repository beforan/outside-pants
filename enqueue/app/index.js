// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");
const redismq = require("rsmq");
const fs = require("fs");
var path = require("path");

const rsmq = new redismq({ host: "redis", ns: "rsmq" });

const queueName = "folderscan";
const processQueue = "fileprocess";
const paths = {
  base: "/var/work",
  queued: "/queued"
};

let isRunning;

const go = () =>
  rsmq.receiveMessage({ qname: queueName }, (err, message) => {
    if (err) {
      console.error(err);
      isRunning = false;
      return;
    }

    if (message.id) {
      console.log("Message received.", message);

      fs.readdir(paths.base, (err, files) => {
        if (err) {
          console.error("Could not list the base work directory.", err);
          isRunning = false;
          return;
        }

        // prep this as a function so we can use it in callbacks
        const queueFiles = () =>
          files.forEach((file, index) => {
            // Make one pass and make the file complete
            var fromPath = path.join(paths.base, file);
            var toPath = path.join(paths.base, paths.queued, file);

            fs.stat(fromPath, (error, stat) => {
              if (error) {
                console.error("Error stating file.", error);
                return;
              }

              if (stat.isFile()) console.log("'%s' is a file.", fromPath);
              else if (stat.isDirectory()) {
                console.log("'%s' is a directory.", fromPath);
                return;
              }

              // Queue it in redis
              const sendMessage = () => {
                let error = false;
                // try and send a message to the queue
                // message content is irrelevant - the presence of a message triggers a scan
                rsmq.sendMessage(
                  { qname: processQueue, message: file },
                  (err, r) => {
                    if (err) {
                      console.error(err);
                      error = true;
                      return;
                    }
                    if (r) {
                      console.log("Message sent. ID:", r);
                    }
                  }
                );
                return error;
              };

              error = false;
              // check the queue we're working with exists
              // create it if not
              rsmq.listQueues((err, qs) => {
                if (err) {
                  console.error(err);
                  error = true;
                  return;
                }

                if (!qs.includes(processQueue)) {
                  console.log("creating new 'fileprocess' queue");
                  rsmq.createQueue({ qname: processQueue }, (err, r) => {
                    if (err) {
                      console.error(err);
                      res.writeHead(500, { "Content-Type": "text/plain" });
                      res.end(
                        `Error creating redis queue 'fileprocess': ${err}`
                      );
                      error = true;
                      return;
                    }
                    if (r === 1) {
                      console.log("'fileprocess' queue created");
                      error = sendMessage();
                    }
                  });
                } else {
                  console.log("'fileprocess' queue already exists");
                  error = sendMessage();
                }
              });

              if (error) {
                console.error("File moving error.", error);
                return;
              }

              // move it
              fs.rename(fromPath, toPath, error => {
                if (error) {
                  console.error("File moving error.", error);
                } else {
                  console.log("Moved file '%s' to '%s'.", fromPath, toPath);
                }
              });
            });
            isRunning = false;
          });

        // ensure the queued output dir exists before queueing the Files
        fs.access(
          path.join(paths.base, paths.queued),
          fs.constants.R_OK | fs.constants.W_OK,
          err => {
            if (err) {
              console.log("queued directory doesn't exist yet, creating");
              fs.mkdirSync(path.join(paths.base, paths.queued), queueFiles);
            } else {
              console.log("queued directory already exists");
              queueFiles();
            }
          }
        );
      });
    } else {
      console.log("No messages currently in 'folderscan' queue...");
    }
    isRunning = false;
  });

// set a repeating call of our actual work
setInterval(() => {
  if (!isRunning) {
    console.log("polling 'folderscan' redis queue...");
    isRunning = true;
    go();
  }
}, 10000);
