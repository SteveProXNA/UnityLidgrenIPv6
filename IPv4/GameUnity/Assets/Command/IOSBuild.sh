#!/bin/bash

/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -executeMethod PerformBuild.CommandLineBuildOnCheckinIOS -CustomArgs:GameServer=127.0.0.1;Version=1;ShortBundleVersion=1;BundleID=com.steveproxna.gameunity;SvnRevision=${SVN_REVISION}