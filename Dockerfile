# 1. 基础镜像：只包含运行 .NET 原生程序必须的极简库 (如 zlib, libgcc)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble
 
WORKDIR /app

COPY --chmod=755 ./release/ConfluenceLite.Api .
COPY ./release/wwwroot ./wwwroot
COPY ./release/appsettings.json .
 
EXPOSE 5000
 
ENTRYPOINT ["./ConfluenceLite.Api"]