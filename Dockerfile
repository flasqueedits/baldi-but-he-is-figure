FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libx11-6 \
    libxext6 \
    libxcursor1 \
    libxinerama1 \
    libxi6 \
    libxrandr2 \
    libgl1 \
    libglu1-mesa \
    libasound2 \
    libudev1 \
    libnss3 \
    libgbm1 \
    libicu70 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY Builds/LinuxServer/ ./server/
# Large Unity data file is stored gzip-compressed to stay under GitHub's 100 MB file limit.
RUN find ./server -name '*.gz' -exec gunzip -f {} \;
RUN chmod +x ./server/FigureServer.x86_64

# Render forwards traffic to the port set in the PORT environment variable (10000).
EXPOSE 10000

CMD ["./server/FigureServer.x86_64", "-batchmode", "-nographics", "-server", "-logFile", "/dev/stdout"]
