module.exports = {
    scanQueue: "folderscan",
    processQueue: "fileprocess",
    paths: {
        base: "/var/work",
        queued: "/queued",
        ignore: ["queued", "badtype", "complete", "incomplete"]
    },
    intervalMs: 10000
}