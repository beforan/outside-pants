// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");

// modules
const redismq = require("rsmq");
const fs = require("fs");
const mv = require('mv');
const path = require("path");

// the message queue
const rsmq = new redismq({ host: "redis", ns: "rsmq" });

// actual value constants
const queueName = "folderscan";
const processQueue = "fileprocess";
const paths = {
  base: "/var/work",
  queued: "/queued"
};

// busy flag
let isRunning;

// this is our actual main function really, we run it on an interval if we're not busy
// sorry it's a mess
const go = () =>
  
  // try and get a message from the queue
  rsmq.receiveMessage({ qname: queueName }, (err, message) => {
    // if we fail, error out
    // flag us as no longer busy so we can try again next interval
    if (err) {
      console.error(err);
      isRunning = false;
      return;
    }

    // receiveMessage claimed to be successful
    // ensure we got a valid message with an id
    if (message.id) {
      console.log("Message received.", message);

      // do the work the message triggered!

      // try get all the files in the directory
      fs.readdir(paths.base, (err, files) => {
        if (err) {
          console.error("Could not list the base work directory.", err);
          isRunning = false;
          return;
        }

        // prep this as a function so we can use it in callbacks
        // this will one by one queue each filename into redis
        // for workers to process
        const queueFiles = () => {
          files.forEach((file, index) => {
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
                return; // don't care about dirs // TODO actually we do care about sub directories
              }

              // Queue it in redis
              const sendMessage = () => {
                let error = false;
                // try and send a message to the queue
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

                      // move it
                      mv(fromPath, toPath, error => {
                        if (error) {
                          console.error("File moving error.", error);
                        } else {
                          console.log(
                            "Moved file '%s' to '%s'.",
                            fromPath,
                            toPath
                          );
                        }
                      });
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
            });
          });

          // remove the message now we're done with it
          rsmq.deleteMessage({ qname: queueName, id: message.id }, () => {
            if (err) console.error(err);
            else console.log(`Deleted completed message: ${message.id}`);
            isRunning = false;
          });
        };

        // ensure the queued output dir exists before queueing the Files
        fs.access(
          path.join(paths.base, paths.queued),
          fs.constants.R_OK | fs.constants.W_OK,
          err => {
            if (err) {
              console.log("queued directory doesn't exist yet, creating");
              fs.mkdir(path.join(paths.base, paths.queued), queueFiles);
            } else {
              console.log("queued directory already exists");
              queueFiles();
            }
          }
        );
      });
    }
    // else {
    //   console.log("No messages currently in 'folderscan' queue...");
    // }
    isRunning = false;
  });



// set a repeating call of our actual work
setInterval(() => {
  if (!isRunning) {
    isRunning = true;
    go();
  }
}, 10000);
