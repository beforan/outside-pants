// add timestamps in front of log messages
require("console-stamp")(console, "HH:MM:ss.l");
const http = require("http");

http
  .createServer(function(req, res) {
    if (req.url == "/initiate" && req.method == "POST") {
      console.log("initiation request received");
      res.writeHead(202, { "Content-Type": "text/plain" });
      res.end("accepted");
    } else {
      console.log("bad request received")
      res.writeHead(400, { "Content-Type": "text/plain" });
      res.end("bad request - please POST to /initiate");
    }
  })
  .listen(5000);
