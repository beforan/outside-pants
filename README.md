# Wat?

- `docker-compose up`
- optionally `--scale process=<n>` where `<n>` is a sensible number
- Preferably wait for ElasticSearch and Kibana to be fully initialised (they take the longest)
- POST to `localhost:5000/initiate` to trigger an enumeration of files in the folder you mount to `/var/work` (see the compose file)

That will start everything running.

Redis, ElasticSearch and Kibana are exposed so you can try and see what's going on. Check the compose file for ports.

# Process

- `bootstrap` literally just listens for a POST so it can tell `enqueue` to start things off. This allows you to populate your data drop first. We aren't cool enough for folder watching.
- `enqueue` polls redis every n seconds and when `bootstrap` queues a message it enumerates the work directory, one file at a time, queues the filename in redis and then copies the file to the `queued` subdirectory.
- `process` polls redis every n seconds and when it sees a filename, processes that filename (if it has an accepted extension e.g. `.csv`), posting row by row to Elastic Search.
- `mq-api` is a REST API for the redis message queue library (`rsmq`). the dotnet app hits this to interact with the queue (the node apps use the library natively)
- the rest of the stack is standard images
