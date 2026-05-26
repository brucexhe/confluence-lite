using SqlSugar;

namespace ConfluenceLite.Api.Data;

/// <summary>
/// 数据库初始化器 - 使用原始SQL建表，绕过AOT限制
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// 初始化所有数据库表
    /// </summary>
    public static void Initialize(ISqlSugarClient db)
    {
        // 先确保基础表存在
        EnsureBaseTables(db);

        // 现有表：添加新字段
        AlterExistingTables(db);

        // 新建表
        CreateUserGroupTables(db);
        CreateWorkspacePermissionTables(db);
        CreatePageRelatedTables(db);
        CreateContentTables(db);
        CreateCollaborationTables(db);
        CreateSearchActivityTables(db);
        CreateSystemManagementTables(db);
        CreateRecentTables(db);

        Console.WriteLine("[Database] All tables initialized successfully");
    }

    private static void EnsureBaseTables(ISqlSugarClient db)
    {
        // 创建基础4张表（如果不存在）
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""users"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""username"" VARCHAR(50) NOT NULL,
                ""email"" VARCHAR(100),
                ""passwordhash"" VARCHAR(255) NOT NULL,
                ""displayname"" VARCHAR(100),
                ""status"" INT NOT NULL DEFAULT 1,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""workspaces"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(100) NOT NULL,
                ""description"" TEXT,
                ""key"" VARCHAR(50) NOT NULL,
                ""ownerid"" BIGINT NOT NULL,
                ""status"" INT NOT NULL DEFAULT 1,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");

        // 添加唯一约束（表已存在时通过 ALTER 添加）
        CreateUniqueConstraintIfNotExists(db, "uq_users_username", "users", "username");
        CreateUniqueConstraintIfNotExists(db, "uq_workspaces_key", "workspaces", "key");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""pages"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""title"" VARCHAR(200) NOT NULL,
                ""content"" TEXT,
                ""status"" INT NOT NULL DEFAULT 0,
                ""workspaceid"" BIGINT NOT NULL,
                ""creatorid"" BIGINT NOT NULL,
                ""parentid"" BIGINT,
                ""sortorder"" INT NOT NULL DEFAULT 0,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""page_comments"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""userid"" BIGINT NOT NULL,
                ""content"" TEXT NOT NULL,
                ""parentid"" BIGINT,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");

        Console.WriteLine("[Database] Base tables ensured");
    }

    private static void AlterExistingTables(ISqlSugarClient db)
    {
        // users 新增字段
        AddColumnIfNotExists(db, "users", "avatarurl", "VARCHAR(500)");
        AddColumnIfNotExists(db, "users", "bio", "TEXT");
        AddColumnIfNotExists(db, "users", "timezone", "VARCHAR(50) NOT NULL DEFAULT 'UTC'");
        AddColumnIfNotExists(db, "users", "locale", "VARCHAR(10) NOT NULL DEFAULT 'zh-CN'");
        AddColumnIfNotExists(db, "users", "lastloginat", "TIMESTAMP");
        AddColumnIfNotExists(db, "users", "isadmin", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "users", "isdeleted", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "users", "deletedat", "TIMESTAMP");

        // workspaces 新增字段
        AddColumnIfNotExists(db, "workspaces", "icon", "VARCHAR(500)");
        AddColumnIfNotExists(db, "workspaces", "color", "VARCHAR(7)");
        AddColumnIfNotExists(db, "workspaces", "homepageid", "BIGINT");
        AddColumnIfNotExists(db, "workspaces", "ispersonal", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "workspaces", "isdefault", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "workspaces", "isdeleted", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "workspaces", "deletedat", "TIMESTAMP");

        // pages 新增字段
        AddColumnIfNotExists(db, "pages", "version", "INT NOT NULL DEFAULT 1");
        AddColumnIfNotExists(db, "pages", "lastmodifierid", "BIGINT");
        AddColumnIfNotExists(db, "pages", "isdeleted", "BOOLEAN NOT NULL DEFAULT FALSE");
        AddColumnIfNotExists(db, "pages", "deletedat", "TIMESTAMP");
        AddColumnIfNotExists(db, "pages", "deletedbyid", "BIGINT");

        // 修改现有列的长度（用于已存在的数据库）
        AlterColumnIfExists(db, "workspaces", "icon", "VARCHAR(500)");
    }

    private static void CreateUserGroupTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""user_groups"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(100) NOT NULL,
                ""description"" VARCHAR(500),
                ""isdefault"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""issystem"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_user_groups_name UNIQUE (""name"")
            )");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""user_group_members"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""groupid"" BIGINT NOT NULL,
                ""userid"" BIGINT NOT NULL,
                ""role"" INT NOT NULL DEFAULT 0,
                ""joinedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_group_user UNIQUE (""groupid"", ""userid"")
            )");
        CreateIndexIfNotExists(db, "ix_ugm_user", "user_group_members", "userid");
    }

    private static void CreateWorkspacePermissionTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""workspace_permissions"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""workspaceid"" BIGINT NOT NULL,
                ""targettype"" INT NOT NULL,
                ""targetid"" BIGINT,
                ""viewspace"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""createpage"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""deletepage"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""deleteownpage"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""editpage"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""exportpage"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""addcomment"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""deletecomment"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""adminspace"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""setpermissions"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_ws_perm_target UNIQUE (""workspaceid"", ""targettype"", ""targetid"")
            )");
        CreateIndexIfNotExists(db, "ix_ws_perm_target", "workspace_permissions", "targettype, targetid");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""workspace_categories"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""workspaceid"" BIGINT NOT NULL,
                ""category"" VARCHAR(100) NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_ws_cat_workspace", "workspace_categories", "workspaceid");
        CreateIndexIfNotExists(db, "ix_ws_cat_category", "workspace_categories", "category");
    }

    private static void CreatePageRelatedTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""page_versions"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""versionnumber"" INT NOT NULL,
                ""title"" VARCHAR(200) NOT NULL,
                ""content"" TEXT,
                ""diffsummary"" VARCHAR(500),
                ""changecomment"" VARCHAR(500),
                ""editorid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_page_version UNIQUE (""pageid"", ""versionnumber"")
            )");
        CreateIndexIfNotExists(db, "ix_pv_page_created", "page_versions", "pageid, createdat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""page_restrictions"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""restrictiontype"" INT NOT NULL,
                ""targettype"" INT NOT NULL,
                ""targetid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_page_restriction UNIQUE (""pageid"", ""restrictiontype"", ""targettype"", ""targetid"")
            )");
        CreateIndexIfNotExists(db, "ix_pr_page", "page_restrictions", "pageid");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""page_labels"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""label"" VARCHAR(100) NOT NULL,
                ""createdbyid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_page_label UNIQUE (""pageid"", ""label"")
            )");
        CreateIndexIfNotExists(db, "ix_pl_label", "page_labels", "label");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""page_templates"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(200) NOT NULL,
                ""description"" VARCHAR(500),
                ""content"" TEXT,
                ""workspaceid"" BIGINT,
                ""creatorid"" BIGINT NOT NULL,
                ""isdefault"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""sortorder"" INT NOT NULL DEFAULT 0,
                ""status"" INT NOT NULL DEFAULT 1,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_pt_workspace", "page_templates", "workspaceid");
        CreateIndexIfNotExists(db, "ix_pt_status", "page_templates", "status");
    }

    private static void CreateContentTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""attachments"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""filename"" VARCHAR(255) NOT NULL,
                ""filesize"" BIGINT NOT NULL DEFAULT 0,
                ""contenttype"" VARCHAR(100) NOT NULL,
                ""storagepath"" VARCHAR(500) NOT NULL,
                ""filehash"" VARCHAR(64),
                ""creatorid"" BIGINT NOT NULL,
                ""comment"" VARCHAR(500),
                ""version"" INT NOT NULL DEFAULT 1,
                ""isdeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_att_page", "attachments", "pageid");
        CreateIndexIfNotExists(db, "ix_att_hash", "attachments", "filehash");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""drafts"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""title"" VARCHAR(200),
                ""content"" TEXT,
                ""pageid"" BIGINT,
                ""workspaceid"" BIGINT,
                ""creatorid"" BIGINT NOT NULL,
                ""parentid"" BIGINT,
                ""drafttype"" INT NOT NULL DEFAULT 0,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_drafts_creator", "drafts", "creatorid");
        CreateIndexIfNotExists(db, "ix_drafts_workspace", "drafts", "workspaceid");
        CreateIndexIfNotExists(db, "ix_drafts_page_creator", "drafts", "pageid, creatorid");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""content_properties"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""entitytype"" INT NOT NULL,
                ""entityid"" BIGINT NOT NULL,
                ""key"" VARCHAR(200) NOT NULL,
                ""value"" TEXT,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_content_prop UNIQUE (""entitytype"", ""entityid"", ""key"")
            )");
        CreateIndexIfNotExists(db, "ix_cp_entity", "content_properties", "entitytype, entityid");
    }

    private static void CreateCollaborationTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""notifications"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""recipientid"" BIGINT NOT NULL,
                ""actorid"" BIGINT NOT NULL,
                ""eventtype"" INT NOT NULL,
                ""entitytype"" INT NOT NULL,
                ""entityid"" BIGINT NOT NULL,
                ""title"" VARCHAR(300),
                ""message"" VARCHAR(500),
                ""isread"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""readat"" TIMESTAMP,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_notif_recipient_unread", "notifications", "recipientid, isread, createdat DESC");
        CreateIndexIfNotExists(db, "ix_notif_recipient_created", "notifications", "recipientid, createdat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""watchers"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""userid"" BIGINT NOT NULL,
                ""entitytype"" INT NOT NULL,
                ""entityid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_watcher UNIQUE (""userid"", ""entitytype"", ""entityid"")
            )");
        CreateIndexIfNotExists(db, "ix_watcher_entity", "watchers", "entitytype, entityid");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""mentions"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""mentioneduserid"" BIGINT NOT NULL,
                ""sourcetype"" INT NOT NULL,
                ""sourceid"" BIGINT NOT NULL,
                ""mentioninguserid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_mentions_target", "mentions", "mentioneduserid, createdat DESC");
        CreateIndexIfNotExists(db, "ix_mentions_source", "mentions", "sourcetype, sourceid");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""shares"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""pageid"" BIGINT NOT NULL,
                ""sharedbyid"" BIGINT NOT NULL,
                ""sharedwithid"" BIGINT NOT NULL,
                ""message"" VARCHAR(500),
                ""isread"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_shares_recipient", "shares", "sharedwithid, isread");
        CreateIndexIfNotExists(db, "ix_shares_page", "shares", "pageid");
    }

    private static void CreateSearchActivityTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""search_history"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""userid"" BIGINT NOT NULL,
                ""query"" VARCHAR(500) NOT NULL,
                ""resultcount"" INT NOT NULL DEFAULT 0,
                ""searchedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_sh_user_time", "search_history", "userid, searchedat DESC");

        // 创建全文索引 (GIN)
        try
        {
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_pages_fts ON pages USING GIN (to_tsvector('simple', title || ' ' || COALESCE(content, '')));
                CREATE INDEX IF NOT EXISTS idx_attachments_fts ON attachments USING GIN (to_tsvector('simple', filename || ' ' || COALESCE(comment, '')));
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Search index creation warning: {ex.Message}");
        }

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""activity_events"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""actorid"" BIGINT NOT NULL,
                ""eventtype"" INT NOT NULL,
                ""entitytype"" INT NOT NULL,
                ""entityid"" BIGINT NOT NULL,
                ""workspaceid"" BIGINT,
                ""title"" VARCHAR(300),
                ""summary"" VARCHAR(500),
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_ae_workspace", "activity_events", "workspaceid, createdat DESC");
        CreateIndexIfNotExists(db, "ix_ae_actor", "activity_events", "actorid, createdat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""audit_log"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""actorid"" BIGINT,
                ""actiontype"" VARCHAR(50) NOT NULL,
                ""entitytype"" VARCHAR(50) NOT NULL,
                ""entityid"" BIGINT,
                ""details"" JSONB,
                ""ipaddress"" VARCHAR(45),
                ""useragent"" VARCHAR(500),
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");
        CreateIndexIfNotExists(db, "ix_al_actor", "audit_log", "actorid, createdat DESC");
        CreateIndexIfNotExists(db, "ix_al_action", "audit_log", "actiontype");
        CreateIndexIfNotExists(db, "ix_al_created", "audit_log", "createdat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""user_favorites"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""userid"" BIGINT NOT NULL,
                ""pageid"" BIGINT NOT NULL,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_user_favorite UNIQUE (""userid"", ""pageid"")
            )");
        CreateIndexIfNotExists(db, "ix_uf_user", "user_favorites", "userid");
    }

    /// <summary>
    /// 安全添加列（如果不存在）
    /// </summary>
    private static void AddColumnIfNotExists(ISqlSugarClient db, string table, string column, string definition)
    {
        var sql = @"
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column";
        var count = db.Ado.GetInt(sql, new List<SugarParameter>
        {
            new("@table", table),
            new("@column", column)
        });
        if (count == 0)
        {
            db.Ado.ExecuteCommand($@"ALTER TABLE ""{table}"" ADD COLUMN ""{column}"" {definition}");
            Console.WriteLine($"[Database] Added column {table}.{column}");
        }
    }

    /// <summary>
    /// 安全修改列类型（如果列存在且类型不同）
    /// </summary>
    private static void AlterColumnIfExists(ISqlSugarClient db, string table, string column, string newDefinition)
    {
        var sql = @"
            SELECT character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column";
        var maxLength = db.Ado.GetInt(sql, new List<SugarParameter>
        {
            new("@table", table),
            new("@column", column)
        });

        // 解析新定义中的长度（例如从 VARCHAR(500) 提取 500）
        var varcharIndex = newDefinition.IndexOf("VARCHAR(");
        if (varcharIndex >= 0)
        {
            var startIndex = varcharIndex + 8; // "VARCHAR(" 的长度
            var endIndex = newDefinition.IndexOf(')', startIndex);
            if (endIndex > startIndex && int.TryParse(newDefinition.AsSpan(startIndex, endIndex - startIndex), out int newLength))
            {
                if (maxLength < newLength)
                {
                    try
                    {
                        db.Ado.ExecuteCommand($@"ALTER TABLE ""{table}"" ALTER COLUMN ""{column}"" TYPE {newDefinition}");
                        Console.WriteLine($"[Database] Altered column {table}.{column} to {newDefinition}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Database] Alter column {table}.{column} warning: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 安全创建索引（如果不存在）
    /// </summary>
    private static void CreateIndexIfNotExists(ISqlSugarClient db, string indexName, string table, string columns)
    {
        try
        {
            db.Ado.ExecuteCommand($@"CREATE INDEX IF NOT EXISTS ""{indexName}"" ON ""{table}"" ({columns})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Index {indexName} warning: {ex.Message}");
        }
    }

    /// <summary>
    /// 安全创建唯一约束（如果不存在）
    /// </summary>
    private static void CreateUniqueConstraintIfNotExists(ISqlSugarClient db, string constraintName, string table, string column)
    {
        try
        {
            var sql = @"
                SELECT COUNT(*) FROM information_schema.table_constraints
                WHERE table_schema = 'public' AND constraint_name = @name AND constraint_type = 'UNIQUE'";
            var count = db.Ado.GetInt(sql, new List<SugarParameter>
            {
                new("@name", constraintName)
            });
            if (count == 0)
            {
                db.Ado.ExecuteCommand($@"ALTER TABLE ""{table}"" ADD CONSTRAINT ""{constraintName}"" UNIQUE (""{column}"")");
                Console.WriteLine($"[Database] Added unique constraint {constraintName} on {table}.{column}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Unique constraint {constraintName} warning: {ex.Message}");
        }
    }

    private static void CreateSystemManagementTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""scheduled_jobs"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(200) NOT NULL,
                ""jobtype"" VARCHAR(50) NOT NULL,
                ""cronexpression"" VARCHAR(100) NOT NULL,
                ""parameters"" JSONB,
                ""isenabled"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""lastrunat"" TIMESTAMP,
                ""nextrunat"" TIMESTAMP,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""updatedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""job_executions"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""jobid"" BIGINT NOT NULL,
                ""startedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""completedat"" TIMESTAMP,
                ""status"" VARCHAR(50) NOT NULL,
                ""errormessage"" TEXT,
                ""output"" JSONB
            )");
        CreateIndexIfNotExists(db, "ix_je_job", "job_executions", "jobid, startedat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""system_backups"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(200) NOT NULL,
                ""description"" VARCHAR(500),
                ""type"" VARCHAR(50) NOT NULL,
                ""options"" JSONB,
                ""filepath"" VARCHAR(500),
                ""filesize"" BIGINT NOT NULL DEFAULT 0,
                ""status"" VARCHAR(50) NOT NULL,
                ""errormessage"" TEXT,
                ""createdbyid"" BIGINT NOT NULL DEFAULT 0,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""completedat"" TIMESTAMP
            )");
        CreateIndexIfNotExists(db, "ix_sb_created", "system_backups", "createdat DESC");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""import_tasks"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(200) NOT NULL,
                ""sourcefile"" VARCHAR(500) NOT NULL,
                ""status"" VARCHAR(50) NOT NULL DEFAULT 'pending',
                ""options"" JSONB,
                ""progress"" JSONB,
                ""errormessage"" TEXT,
                ""createdat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""completedat"" TIMESTAMP,
                ""createdbyid"" BIGINT NOT NULL
            )");
        CreateIndexIfNotExists(db, "ix_it_created", "import_tasks", "createdat DESC");
        CreateIndexIfNotExists(db, "ix_it_status", "import_tasks", "status");
    }

    private static void CreateRecentTables(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ""recents"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""userid"" BIGINT NOT NULL,
                ""pageid"" BIGINT NOT NULL,
                ""visitedat"" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT uq_recent_user_page UNIQUE (""userid"", ""pageid"")
            )");
        CreateIndexIfNotExists(db, "ix_recent_user_date", "recents", "userid, visitedat DESC");
    }
}
