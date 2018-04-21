const fs = require("fs");
const path = require("path");

// Promise Wrappers
const {
    listQueuesAsync,
    receiveMessageAsync,
    createQueueAsync,
    deleteMessageAsync
} = require('./promises');

const { scanQueue, processQueue, paths } = require('./const');
const recursiveFileQueue = require('./recursive-file-queue');

// The actual main work function
const run = async () => {

    // check the queue we're reading from exists
    // quit if not, and the main loop will retry
    if (!(await listQueuesAsync()).includes(scanQueue)) {
        console.log(`'${scanQueue}' queue not present`);
        return;
    }

    // now continue with our work
    try {
        const message = await receiveMessageAsync({ qname: scanQueue });

        if (message.id) {
            console.log(`Message received: ${message.id}`);

            // ensure the queued output dir exists
            try {
                fs.accessSync(
                    path.join(paths.base, paths.queued),
                    fs.constants.R_OK | fs.constants.W_OK);
                console.log("queued directory already exists");
            } catch (e) {
                console.log("queued directory doesn't exist yet, creating");
                fs.mkdirSync(path.join(paths.base, paths.queued));
            }

            // check the queue we're working with exists
            // create it if not
            if (!(await listQueuesAsync()).includes(processQueue)) {
                await createQueueAsync({ qname: processQueue });
                console.log(`'${processQueue}' queue created`);
            } else {
                console.log(`'${processQueue}' queue already exists`);
            }

            // do the queueing
            await recursiveFileQueue(paths.base);

            // remove the message now we're done with it
            await deleteMessageAsync({ qname: scanQueue, id: message.id });
            console.log(`Deleted completed message: ${message.id}`);
        }
    } catch (err) {
        console.error(err);
    }
}

module.exports = run;