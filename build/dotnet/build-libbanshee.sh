#!/bin/sh
# Builds libbanshee.so (the native GStreamer backend) into bin/ without autotools.
# Requires the GStreamer 1.0 and GTK+ 3 development packages.
set -e

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
SRC="$ROOT/src/Backends/Banshee.GStreamer/libbanshee"
OUT="$ROOT/bin/libbanshee.so"

PKGS="gtk+-3.0 gstreamer-1.0 gstreamer-base-1.0 gstreamer-controller-1.0
      gstreamer-plugins-base-1.0 gstreamer-audio-1.0 gstreamer-fft-1.0
      gstreamer-pbutils-1.0 gstreamer-tag-1.0 gstreamer-video-1.0"

SOURCES="banshee-bpmdetector.c banshee-gst.c banshee-player.c banshee-player-cdda.c
         banshee-player-dvd.c banshee-player-equalizer.c banshee-player-missing-elements.c
         banshee-player-pipeline.c banshee-player-replaygain.c banshee-player-video.c
         banshee-player-vis.c banshee-ripper.c banshee-tagger.c banshee-transcoder.c"

# VERSION used to come from the autotools config.h
VERSION=$(sed -n 's/^m4_define(\[banshee_version_\(major\|minor\|micro\)\], \[\([0-9]*\)\])/\2/p' "$ROOT/configure.ac" | paste -sd.)

mkdir -p "$ROOT/bin"
cd "$SRC"
# shellcheck disable=SC2086
${CC:-cc} -shared -fPIC -O2 -g -Wall -D_FORTIFY_SOURCE=2 -DVERSION="\"$VERSION\"" -I"$ROOT" \
    $(pkg-config --cflags $PKGS) \
    $SOURCES \
    -o "$OUT" \
    $(pkg-config --libs $PKGS)
echo "Built $OUT"
