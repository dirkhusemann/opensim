#!/bin/bash

MAJOR=0
MINOR=1
BUILD=`date +%s`
REVISION=`svnversion . | sed s/:// | sed s/M//`
REALREVISION=`svnversion`
cat src/VersionInfo.cs.template | sed s/@@VERSION/"$MAJOR.$MINOR, Build $BUILD, Revision $REALREVISION"/g >src/VersionInfo.cs
echo -n $MAJOR.$MINOR.0.$REVISION >VERSION
