# Self-contained Valhalla image with prebuilt Moscow tiles baked in.
# The gis-ops base serves tiles from /custom_files; its default env has
# use_tiles_ignore_pbf=True + serve_tiles=True, so it serves the baked
# valhalla_tiles.tar without needing the source .pbf or a rebuild.
FROM ghcr.io/gis-ops/docker-valhalla/valhalla:latest
COPY valhalla.json /custom_files/valhalla.json
COPY valhalla_tiles.tar /custom_files/valhalla_tiles.tar
