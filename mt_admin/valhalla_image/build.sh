#!/usr/bin/env bash
# Rebuild treealarm/valhalla_moscow:latest by baking prebuilt Moscow tiles
# into the gis-ops valhalla base image.
#
# Usage: ./build.sh [SRC_DIR]
#   SRC_DIR must contain valhalla.json and valhalla_tiles.tar.
#   Default: the leaflet_dock custom_files dir where the tiles were built.
#
# To (re)build the tiles from scratch instead, run the gis-ops image against a
# Moscow .pbf (e.g. moscow-latest.osm.pbf) with serve_tiles/build_tar enabled,
# then point this script at the resulting custom_files dir.
set -euo pipefail

SRC="${1:-/mnt/wdblue/tests/Square/leaflet_dock/leaflet_data/valhalla_custom_files}"
IMAGE="treealarm/valhalla_moscow:latest"
DIR="$(cd "$(dirname "$0")" && pwd)"

for f in valhalla.json valhalla_tiles.tar; do
  [ -f "$SRC/$f" ] || { echo "ERROR: $SRC/$f not found" >&2; exit 1; }
done

CTX="$(mktemp -d)"
trap 'rm -rf "$CTX"' EXIT
cp "$DIR/Dockerfile" "$CTX/"
cp "$SRC/valhalla.json" "$SRC/valhalla_tiles.tar" "$CTX/"

docker build -t "$IMAGE" "$CTX"
echo "Built $IMAGE"
echo "Publish with: docker login -u treealarm && docker push $IMAGE"
