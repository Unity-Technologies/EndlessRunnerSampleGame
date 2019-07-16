# Endless Runner Sample game

_Current Used Unity Version : 2019.2_

This repository use tags for versioning. Look in the [Releases](https://github.com/Unity-Technologies/EndlessRunnerSampleGame/releases)
section to download the source for specific other Unity version, or use git
tag to checkout a specific version (e.g. `git checkout 18.2`)

## Cloning note

**This repository use git Large File Support.
To clone it successfully, you will need to install git lfs** :

- Download git lfs here : https://git-lfs.github.com/
- run `git lfs install` in a command line

Now you git clone should get the LFS files properly. For support of LFS in Git
GUI client, please refer to their respective documentation

## Description

This project is a "endless runner" type mobile game made in Unity

You can find [the project on the Unity asset store](https://assetstore.unity.com/packages/essentials/tutorial-projects/endless-runner-sample-game-87901)
(Note that this is the old version not using Lightweight rendering pipeline & addressable, see note at the end of this file.
Assets stor version will be updated when Addressable is out of preview)

A INSTRUCTION.txt text file is inside the Asset folder to highlight diverse
important points of the project

An article is available [on the Unity Learn website](https://unity3d.com/learn/tutorials/topics/mobile-touch/trash-dash-code-walkthrough)
highlighting some part of the code.

You can also go visit the [wiki](https://github.com/Unity-Technologies/EndlessRunnerSampleGame/wiki)
for more in depth details on the projects, how to build it and modify it.

## Note for this Github version

This version include feature not in the released game in the asset store version:

- A basic tutorial when the game is played the first time
- The use of the new Lightweight Rendering pipeline
- The use of the new Addressable System that replace the Assets Bundles.

**Documentation is still not up to date in the wiki. Updating is in progress**
