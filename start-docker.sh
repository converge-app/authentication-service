#!/bin/bash

TAG=$(date +%s)
SERVICE_NAME="authentication-service"
ORGANISATION="converge"
DOCKER_IMAGE="$ORGANISATION/$SERVICE_NAME:$TAG"

docker build . -t "$DOCKER_IMAGE"

docker run --rm --name authentication-service \
-e ELASTICSEARCH_URI="http://localhost:9200" \
-e CollectionName="Authentication" \
-e ConnectionString="mongodb://localhost:27017" \
-e DatabaseName="ApplicationDb" \
-e MONGO_INITDB_ROOT_USERNAME="application" \
-e MONGO_INITDB_ROOT_PASSWORD="password" \
-e MONGO_SERVICE_NAME="localhost" \
-e MONGO_SERVICE_PORT="27017" \
-e JAEGER_AGENT_HOST="localhost" \
-e JAEGER_AGENT_PORT="6831" \
-e JAEGER_SAMPLER_TYPE="const" \
-p 8080:80 \
"$DOCKER_IMAGE"
