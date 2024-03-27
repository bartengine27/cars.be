#!/bin/bash

if [[ ! -d certs ]]
then
    mkdir certs
    cd certs/
    if [[ ! -f localhost.pfx ]]
    then
        dotnet dev-certs https -v -ep localhost.pfx -p c32531bd-9000-4628-87b9-896e7af5c5e2 -t
    fi
    cd ../
fi

docker-compose up -d
