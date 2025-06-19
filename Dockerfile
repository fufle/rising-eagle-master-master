FROM mono:latest

COPY sqlite-netFx-full-source-1.0.108.0.zip /usr/src/

RUN apt_packages=" \
        build-essential \
        unzip \
        wget \
    "; \
    apt-get update && apt-get install -y --no-install-recommends $apt_packages && rm -rf /var/lib/apt/lists/* \
    && mkdir /usr/src/sqlite-interop \
    && unzip /usr/src/sqlite-netFx-full-source-1.0.108.0.zip -d /usr/src/sqlite-interop/ \
    && cd /usr/src/sqlite-interop/Setup/ \
    && chmod +x compile-interop-assembly-debug.sh \
    && ./compile-interop-assembly-debug.sh \
    && cp ../bin/2013/Debug/bin/libSQLite.Interop.so /usr/lib/ \
    && rm -rf /usr/src/sqlite-interop

COPY PRMasterServer.sln /usr/src/gamespy-master/
COPY PRMasterServer/ /usr/src/gamespy-master/PRMasterServer/
COPY .version/ /usr/src/gamespy-master/.version/

RUN cd /usr/src/gamespy-master/ \
    && nuget restore \
    && msbuild \
    && useradd -ms /bin/bash master_server \
    && mkdir /data \
    && chown -R master_server:master_server /data

USER master_server
WORKDIR /usr/src/gamespy-master/bin/Debug/
EXPOSE 27900/udp 28910/tcp 29900/tcp 29901/tcp 29910/udp
VOLUME [ "/data" ]
CMD [ "mono", "PRMasterServer.exe", "+db", "/data/db.sqlite3" ]
