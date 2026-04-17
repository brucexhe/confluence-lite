# 1. 基础镜像：只包含运行 .NET 原生程序必须的极简库 (如 zlib, libgcc)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0

# 2. 设置容器内的工作目录
WORKDIR /app

# 3. 拷贝你的独立执行文件 (注意路径要对上你 native 后的实际位置)
# 假设你的项目名是 water-tracker-api
COPY ./release/ConfluenceLite.Api .

# 4. 拷贝你的 HTML 静态文件 (Native AOT 不会自动打包静态资源，必须手动拷)
COPY ./release/wwwroot .
COPY ./appsettings.json .

# 5. 赋予执行权限
RUN chmod +x ./ConfluenceLite.Api

# 6. 暴露 8080 端口
EXPOSE 5000

# 7. 启动！由于是原生二进制，不需要 'dotnet' 命令，直接运行文件名
ENTRYPOINT ["./ConfluenceLite.Api"]