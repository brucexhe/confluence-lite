# 1. 基础镜像：只包含运行 .NET 原生程序必须的极简库 (如 zlib, libgcc)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble

WORKDIR /app

COPY --chmod=755 ./release/ConfluenceLite.Api .
COPY ./release/wwwroot ./wwwroot
COPY ./release/appsettings.json .

# 创建数据目录（用于持久化安装状态和配置）
RUN mkdir -p /app/data

# 声明持久化卷（升级镜像时数据不会丢失）
VOLUME ["/app/data"]

EXPOSE 5000

ENTRYPOINT ["./ConfluenceLite.Api"]