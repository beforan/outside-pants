module.exports = {
    scanQueue: "folderscan",
    processQueue: "fileprocess",
    paths: {
        base: "/var/work",
        queued: "/queued",
        ignore: ["queued", "badtype", "processed"]
    },
    intervalMs: 10000
}