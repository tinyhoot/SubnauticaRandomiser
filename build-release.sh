#!/bin/bash

PROJECT_NAME="SubnauticaRandomiser"

PROJECT_DIR=$(pwd)
RELEASE_DIR="./$PROJECT_NAME/bin/Release"

cd $RELEASE_DIR
mkdir tmp
mkdir tmp/plugins
mkdir tmp/plugins/$PROJECT_NAME
mkdir tmp/plugins/$PROJECT_NAME/DataFiles

cp $PROJECT_NAME.dll tmp/plugins/$PROJECT_NAME
cp $PROJECT_DIR/ReadMe-Documentation.txt tmp/plugins/$PROJECT_NAME
cp $PROJECT_DIR/DataFiles/* tmp/plugins/$PROJECT_NAME/DataFiles

cd tmp
zip -r $PROJECT_NAME plugins
mv $PROJECT_NAME.zip ..
cd ..
rm -r tmp