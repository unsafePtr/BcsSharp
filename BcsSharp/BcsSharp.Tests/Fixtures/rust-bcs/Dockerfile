# syntax=docker/dockerfile:1.7
#
# Builds a container that bakes in Rust-generated BCS fixture bytes for cross-impl
# parity tests. C# Testcontainers fixture builds this image, starts the container,
# and CopyAsync's individual .bcs files out for comparison against C# serializer
# output.
#
# The image build is two stages — only the tiny runtime stage is kept around.

# --- Stage 1: compile Rust binaries and run them to generate fixtures ----------
FROM rust:1.93-alpine AS builder

WORKDIR /app

# Copy manifest + lockfile first so the dependency graph caches independently of
# changes to src/.
COPY Cargo.toml Cargo.lock ./
COPY src/ ./src/

RUN cargo build --release --locked --bin sui-bcs-serialization --bin large_enum

# Run each binary in /fixtures so its .bcs file output lands there.
RUN mkdir /fixtures && \
    cd /fixtures && \
    /app/target/release/sui-bcs-serialization && \
    /app/target/release/large_enum

# --- Stage 2: just the generated fixtures + an idle entrypoint -----------------
FROM alpine:3

COPY --from=builder /fixtures /fixtures

# Keep the container alive so Testcontainers can pull files out via docker cp.
ENTRYPOINT ["sleep", "infinity"]
