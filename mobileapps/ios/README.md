# Tipitaka iOS App

## Status: Planned

The iOS version will use a WKWebView wrapper pointing to the same PWA at tipitaka.org.

Since iOS does not support TWA, the approach will be:
1. **WKWebView** wrapper with full-screen display
2. Leverage the same PWA (manifest.json + service worker) for offline support
3. Submit to the Apple App Store

## Prerequisites
- macOS with Xcode installed
- Apple Developer account ($99/year)
- The PWA must already be functional at tipitaka.org

## Setup Steps (Future)
1. Create an Xcode project with a WKWebView
2. Point it to https://tipitaka.org
3. Configure App Transport Security for the domain
4. Build and submit to App Store