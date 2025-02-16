#!/bin/bash

export b=$(pwd); 
export a=/tmp/asrootdir$$

rm -rf $a 
mkdir $a 
cd $a

cmake -DCPACK_PACKAGE_VERSION_MAJOR=$1 -DCPACK_PACKAGE_VERSION_MINOR=$2 -DCPACK_PACKAGE_VERSION_PATCH=$3 -GNinja $b && ninja package

cp *.deb $b/../build

cd ..
rm -rf $a
