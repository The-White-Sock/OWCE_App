#!/bin/sh
# Generates C# straight into the OWCE project, which is what actually gets built and
# was previously never updated by this script - it wrote to a local ./csharp/ folder
# that nothing referenced, so regenerating the proto required manually copying the
# output over, and could silently drift from what's checked in.
OUT_DIR=../OWCE/OWCE/Protobuf
mkdir -p "$OUT_DIR"
protoc -I=. --csharp_out="$OUT_DIR" *.proto
