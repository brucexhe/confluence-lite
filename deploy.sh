
#build api
dotnet publish -r linux-x64 -c Release --self-contained true -p:PublishSingleFile=true -o ./release

rm -rf ./release/ConfluenceLite.Api.dbg
rm -rf ./release/appsettings.Development.json
rm -rf ./release/wwwroot/uploads/*

#build app
#cd confluence-lite-app
#yarn build 
#cp -r dist/* ../release/wwwroot/
#cd ..

#build docker image
docker build -t docker.peos.cn/brucexhe/confluencelite .

docker push docker.peos.cn/brucexhe/confluencelite

#docker run -d docker.peos.cn/brucexhe/water-tracker-api