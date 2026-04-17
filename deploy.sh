
#build
dotnet publish -r linux-x64 -c Release --self-contained -o ./release

rm -rf ./release/ConfluenceLite.Api.dbg
rm -rf ./release/appsettings.Development.json
rm -rf ./release/wwwroot/uploads/*

docker build -t docker.peos.cn/brucexhe/confluencelite ./release/

docker push docker.peos.cn/brucexhe/confluencelite

#docker run -d docker.peos.cn/brucexhe/water-tracker-api