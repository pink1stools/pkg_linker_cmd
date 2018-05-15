#!/bin/bash
# ------------------------------------------------------------------
# Simple script to build a proper PKG using Python (by CaptainCPS-X)
# ------------------------------------------------------------------
#Change this depending where you installed Python...
export PYTHON=usr/local/lib/Python2.7
# Change these for your application / manual...
export CONTENTID=UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg
export PKG_DIR=./PS3_GAME/
export PKG_NAME=./$CONTENTID.pkg
# This will run the Python PKG script...
python ./pkg.py --contentid $CONTENTID ./PS3_GAME/ $PKG_NAME
