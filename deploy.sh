
#build app
cd confluence-lite-app
pnpm i
yarn build
rm -rf ../release/wwwroot/*
cp -r dist/* ../release/wwwroot/
cd ..

#build api
wsl "//root/.dotnet/dotnet" publish -r linux-x64 -c Release --self-contained true -p:PublishSingleFile=true -o ./release

wsl rm -rf ./release/ConfluenceLite.Api.dbg
wsl rm -rf ./release/appsettings.Development.json
wsl rm -rf ./release/wwwroot/uploads/*


#build docker image
wsl service docker start
wsl docker build -t docker.peos.cn/brucexhe/confluencelite .

wsl docker push docker.peos.cn/brucexhe/confluencelite

# 运行示例（请挂载数据目录以保持持久化）：
# docker run -d -v confluence-lite-data:/app/data -p 5000:5000 docker.peos.cn/brucexhe/confluencelite