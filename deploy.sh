#!/bin/bash

rsync -r --delete $TRAVIS_BUILD_DIR/publish/ travis@rancor.mortimer.nu:/var/travis/runit-api/$TRAVIS_BUILD_ID
ssh -t travis@rancor.mortimer.nu "rm /var/travis/runit-api/current && ln -s /var/travis/runit-api/$TRAVIS_BUILD_ID /var/travis/runit-api/current"
ssh -t travis@rancor.mortimer.nu "cp /var/aspnetcore/appsettings.Production.json /var/travis/runit-api/current/"
ssh -t travis@rancor.mortimer.nu "sudo /bin/systemctl restart kestrel-runit-api.service"