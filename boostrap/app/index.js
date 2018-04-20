// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");
const http = require("http");
const redismq = require("rsmq");

const rsmq = new redismq({ host: "redis", ns: "rsmq" });

const queueName = "folderscan";
const messageContent = "#NotAllHeroesWearCapes";

const sendMessage = () => {
  let error = false;
  // try and send a message to the queue
  // message content is irrelevant - the presence of a message triggers a scan
  rsmq.sendMessage({ qname: queueName, message: messageContent }, (err, r) => {
    if (err) {
      console.error(err);
      res.writeHead(500, { "Content-Type": "text/plain" });
      res.end(`Error sending message to redis queue 'folderscan': ${err}`);
      error = true;
      return;
    }
    if (r) {
      console.log("Message sent. ID:", r);
    }
  });
  return error;
};

// listen for requests we care about, reject the rest
http
  .createServer(function(req, res) {
    if (req.url == "/initiate" && req.method == "POST") {
      console.log("initiation request received");

      let error = false;
      // check the queue we're working with exists
      // create it if not
      rsmq.listQueues((err, qs) => {
        if (err) {
          console.error(err);
          res.writeHead(500, { "Content-Type": "text/plain" });
          res.end(`Error listing redis queues: ${err}`);
          error = true;
          return;
        }

        if (!qs.includes(queueName)) {
          console.log("creating new 'folderscan' queue");
          rsmq.createQueue({ qname: queueName }, (err, r) => {
            if (err) {
              console.error(err);
              res.writeHead(500, { "Content-Type": "text/plain" });
              res.end(`Error creating redis queue 'folderscan': ${err}`);
              error = true;
              return;
            }
            if (r === 1) {
              console.log("'folderscan' queue created");
              error = sendMessage();
            }
          });
        } else {
          console.log("'folderscan' queue already exists");
          error = sendMessage();
        }
      });

      if (error) return;

      // respond successfully
      res.writeHead(202, { "Content-Type": "text/plain" });
      res.end("accepted");
    } else {
      // reject anything else
      console.log("bad request received");
      res.writeHead(400, { "Content-Type": "text/plain" });
      res.end("bad request - please POST to /initiate");
    }
  })
  .listen(5000);
