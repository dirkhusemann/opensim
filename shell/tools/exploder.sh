#!/bin/bash
limit=$1
shift
for x in $(seq 0 $(($limit - 1))) ; do
    eval $*
done