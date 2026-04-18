# Docker 部署指南

## 数据持久化

为了在容器升级后保留安装状态和配置，**必须挂载数据目录**：

```bash
docker run -d \
  -v confluence-lite-data:/app/data \
  -p 5000:5000 \
  docker.peos.cn/brucexhe/confluencelite
```

### 配置说明

| 文件 | 位置 | 说明 |
|------|------|------|
| `appsettings.json` | `/app/appsettings.json` | 默认配置，随镜像分发 |
| `appsettings.runtime.json` | `/app/data/appsettings.runtime.json` | 运行时配置（数据库连接等），自动生成 |
| `INSTALLED` | `/app/data/INSTALLED` | 安装标记，自动生成 |

### 为什么需要挂载数据目录？

- **安装状态持久化**：`INSTALLED` 文件记录系统已安装，避免重复进入安装向导
- **配置持久化**：`appsettings.runtime.json` 保存数据库连接等配置
- **升级友好**：升级镜像时，容器内文件系统会重置，但挂载的数据卷会保留

## docker-compose.yml 示例

```yaml
version: '3.8'
services:
  confluence-lite:
    image: docker.peos.cn/brucexhe/confluencelite
    ports:
      - "5000:5000"
    volumes:
      - data:/app/data

volumes:
  data:
```

## 首次部署

1. 启动容器：
   ```bash
   docker run -d -v confluence-lite-data:/app/data -p 5000:5000 docker.peos.cn/brucexhe/confluencelite
   ```

2. 访问 `http://localhost:5000`，进入安装向导

3. 配置数据库并创建管理员账户

4. 安装完成后，数据自动保存到数据卷

## 升级部署

升级时只需拉取新镜像并重新创建容器，**数据不会丢失**：

```bash
# 拉取新镜像
docker pull docker.peos.cn/brucexhe/confluencelite

# 停止并删除旧容器
docker stop confluence-lite
docker rm confluence-lite

# 使用相同的数据卷启动新容器
docker run -d \
  --name confluence-lite \
  -v confluence-lite-data:/app/data \
  -p 5000:5000 \
  docker.peos.cn/brucexhe/confluencelite
```

## 备份建议

定期备份数据卷：

```bash
# 备份数据卷
docker run --rm \
  -v confluence-lite-data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/confluence-lite-backup.tar.gz -C /data .

# 恢复数据卷
docker run --rm \
  -v confluence-lite-data:/data \
  -v $(pwd):/backup \
  alpine tar xzf /backup/confluence-lite-backup.tar.gz -C /data
```
