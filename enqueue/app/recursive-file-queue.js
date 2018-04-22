const fs = require("fs");
const path = require("path");

// Promise Wrappers
const {
    statAsync,
    renameAsync,
    mkdirpAsync,
    sendMessageAsync
} = require('./promises');

const { processQueue, paths } = require('./const');

// queue files in subdirectories forever
const recursiveFileQueue = async dir => {
    const files = fs.readdirSync(dir); // TODO this would be way better async tbh

    // actually queue the files
    files.forEach(async file => {
        const fromPath = path.join(dir, file);
        const toPathDirOnly = path.join(
            paths.base,
            paths.queued,
            dir.replace(paths.base, "")); // get the dir path relative to base
        const toPath = path.join(toPathDirOnly, file); // add the file on the end

        // ensure the target directory exists
        mkdirpAsync(toPathDirOnly);

        const stat = await statAsync(fromPath);
        if (stat.isFile()) {
            console.log("'%s' is a file.", fromPath);

            // move the file first
            await renameAsync(fromPath, toPath);
            console.log(
                "Moved file '%s' to '%s'.",
                fromPath,
                toPath
            );

            // then queue it
            const response = await sendMessageAsync({ qname: processQueue, message: toPath });
            if (response) console.log("Message sent. ID:", response);
        } else if (stat.isDirectory()) {
            console.log("'%s' is a directory.", fromPath);
            if (paths.ignore.includes(file))
                console.log(`'${file}' is on the ignore list, skipping`);
            else await recursiveFileQueue(fromPath);
            // TODO  delete the directory? or just leave it...
        }
    });
}

module.exports = recursiveFileQueue;