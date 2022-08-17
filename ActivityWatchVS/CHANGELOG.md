# Road map

- [x] A feature that has been completed
- [ ] A feature that has NOT yet been completed

Features that have a checkmark are complete and available for
[download in the CI build](http://vsixgallery.com/extension/ActivityWatchVS.ea6d1160-0387-4c74-9caf-1f9fcabf5ea5/).

## Change log

These are the changes to each version that has been released
on the official Visual Studio extension gallery (for stable)
or on vsixgallery.com (beta/alpha/testing)

## future release

- [ ] TFS / Work Item tracking
- [ ] ... your ideas

## 1.9.0 (testing) Visual Studio 2022 support

- [x] thanks to https://github.com/DarkOoze we have VS2022 Support in testing now.

## 1.1.x (stable) get versioning right

- [x] fix appveyor/vsixmanifest files for correct version numbers everywhere https://github.com/LaggAt/ActivityWatchVS/issues/2

## 1.0.18 (stable) bug fix

- [x] bug fix: ArgumentException while getting active Document https://github.com/LaggAt/ActivityWatchVS/issues/1

## 1.0.x.9 (stable) feedback round 2

- [x] fixed 2019 install issues

## 1.0.x.8 (stable) feedback round

- [x] upload stable build to Visual Studio Marketplace

## 0.9.x.6 - 0.9.x.7 (beta) vsix configuration

- [x] enable it for other VS Versions (Community, Pro, Enterprise for VS 2017/2019)

## 0.9.x.5 (beta) automate deployment of beta/stable branches

- [x] build pipeline for VSIX Packages

## 0.0.0.4 prepairing for automated builds

- [x] testing and configuring deployment
- [x] fix bug not able to set url to a non-default
- [x] as all works great for now. Call it a beta.

## 0.0.0.3 Usage improvement

- [x] autostart aw-qt.exe if found - searching program files and appdata folders 
  (e.g. C:\Program Files\activitywatch\aw-qt.exe)

## 0.0.0.2 UI Alpha

- [x] style VS options page
- [x] custom logo
- [x] package metadata

## 0.0.0.1 First Alpha

- [x] send cumulated events to aw-server
- [x] use server from aw-server.ini (convention over configuration)
- [x] options dialog to enable/disable sending events, overriding URL, links
- [x] logging