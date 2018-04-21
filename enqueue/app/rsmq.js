const redismq = require("rsmq");
module.exports = new redismq({ host: "redis", ns: "rsmq" });