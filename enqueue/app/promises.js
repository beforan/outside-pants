// This simply wraps some callback based functions in Promises and exports them

const rsmq = require('./rsmq');
const fs = require('fs');
const mkdirp = require("mkdirp");

const promisify = fn =>
    (...params) =>
        new Promise((resolve, reject) =>
            fn(...params.concat([
                (err, ...args) =>
                    err
                        ? reject(err)
                        : resolve(args.length < 2 ? args[0] : args)
            ])));

module.exports = {
    receiveMessageAsync: promisify(rsmq.receiveMessage),
    listQueuesAsync: promisify(rsmq.listQueues),
    createQueueAsync: promisify(rsmq.createQueue),
    sendMessageAsync: promisify(rsmq.sendMessage),
    deleteMessageAsync: promisify(rsmq.deleteMessage),
    statAsync: promisify(fs.stat),
    renameAsync: promisify(fs.rename),
    mkdirpAsync: promisify(mkdirp),
    readdirAsync: promisify(fs.readdir)
}