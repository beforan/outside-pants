package main

import (
	_ "github.com/fsantagostinobietti/logspout-file-module"
	// _ "github.com/looplab/logspout-logstash"
	_ "github.com/gliderlabs/logspout/transports/tcp"
	_ "github.com/gliderlabs/logspout/transports/udp"
)