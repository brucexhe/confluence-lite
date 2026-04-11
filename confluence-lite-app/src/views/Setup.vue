<template>
  <div class="setup-container">
    <div class="setup-background">
      <div class="bg-shape shape-1"></div>
      <div class="bg-shape shape-2"></div>
    </div>
    <div class="setup-card">
      <!-- Header -->
      <div class="setup-header">
        <div class="brand">
          <span class="brand-icon">C</span>
          <span class="brand-name">Confluence Lite</span>
        </div>
        <h2 class="setup-title">系统安装向导</h2>
        <p class="setup-subtitle">跟随引导完成首次配置</p>
      </div>

      <!-- Step 1: Database Config -->
      <div v-if="currentStep === 0" class="step-content">
        <h3>配置数据库连接</h3>
        <a-form layout="vertical" :model="dbConfig">
          <a-form-item label="数据库类型">
            <a-select v-model:value="dbConfig.dbType">
              <a-select-option value="PostgreSQL">PostgreSQL</a-select-option>
              <a-select-option value="MySQL">MySQL (即将支持)</a-select-option>
            </a-select>
          </a-form-item>
          <div class="form-row">
            <a-form-item label="主机地址" class="form-item-flex">
              <a-input v-model:value="dbConfig.host" placeholder="localhost" />
            </a-form-item>
            <a-form-item label="端口" class="form-item-port">
              <a-input-number v-model:value="dbConfig.port" :min="1" :max="65535" style="width:100%" />
            </a-form-item>
          </div>
          <a-form-item label="数据库名称">
            <a-input v-model:value="dbConfig.database" placeholder="confluencelite" />
          </a-form-item>
          <div class="form-row">
            <a-form-item label="用户名" class="form-item-flex">
              <a-input v-model:value="dbConfig.username" placeholder="postgres" />
            </a-form-item>
            <a-form-item label="密码" class="form-item-flex">
              <a-input-password v-model:value="dbConfig.password" placeholder="数据库密码" />
            </a-form-item>
          </div>
        </a-form>
        <div class="step-actions">
          <a-button @click="testConnection" :loading="testing" type="primary" ghost>
            测试连接
          </a-button>
          <a-button type="primary" @click="nextStep" :disabled="!connectionTested">
            下一步
          </a-button>
        </div>
        <div v-if="testResult" class="test-result" :class="testResult.success ? 'success' : 'error'">
          <span v-if="testResult.success">连接成功 — {{ testResult.version }}</span>
          <span v-else>{{ testResult.error }}</span>
        </div>
      </div>

      <!-- Step 2: Admin Account -->
      <div v-if="currentStep === 1" class="step-content">
        <h3>创建管理员账户</h3>
        <a-form layout="vertical" :model="adminConfig">
          <div class="form-row">
            <a-form-item label="用户名" class="form-item-flex">
              <a-input v-model:value="adminConfig.username" placeholder="admin" />
            </a-form-item>
            <a-form-item label="显示名称" class="form-item-flex">
              <a-input v-model:value="adminConfig.displayName" placeholder="Admin" />
            </a-form-item>
          </div>
          <div class="form-row">
            <a-form-item label="密码" class="form-item-flex">
              <a-input-password v-model:value="adminConfig.password" placeholder="至少6位" />
            </a-form-item>
            <a-form-item label="确认密码" class="form-item-flex">
              <a-input-password v-model:value="adminConfig.confirmPassword" placeholder="再次输入密码" />
            </a-form-item>
          </div>
          <a-form-item label="邮箱 (可选)">
            <a-input v-model:value="adminConfig.email" placeholder="admin@example.com" />
          </a-form-item>
        </a-form>
        <div class="step-actions">
          <a-button @click="prevStep">上一步</a-button>
          <a-button type="primary" @click="nextStep"
                    :disabled="!adminConfig.username || !adminConfig.password || adminConfig.password !== adminConfig.confirmPassword">
            下一步
          </a-button>
        </div>
      </div>

      <!-- Step 3: Default Space -->
      <div v-if="currentStep === 2" class="step-content">
        <h3>配置默认空间</h3>
        <p class="step-desc">系统将自动创建一个空间作为您的知识库起点。</p>
        <a-form layout="vertical" :model="spaceConfig">
          <a-form-item label="空间名称">
            <a-input v-model:value="spaceConfig.name" placeholder="我的知识库" />
          </a-form-item>
          <a-form-item label="空间标识 (英文/数字)">
            <a-input v-model:value="spaceConfig.key" placeholder="HOME"
                     style="text-transform:uppercase" @input="spaceConfig.key = spaceConfig.key.toUpperCase()" />
          </a-form-item>
        </a-form>
        <div class="step-actions">
          <a-button @click="prevStep">上一步</a-button>
          <a-button type="primary" @click="runInstall" :disabled="!spaceConfig.name || !spaceConfig.key"
                    :loading="installing">
            开始安装
          </a-button>
        </div>
      </div>

      <!-- Step 4: Installing / Done -->
      <div v-if="currentStep === 3" class="step-content">
        <div v-if="installing" class="install-progress">
          <a-spin size="large" />
          <p class="install-text">正在安装，请稍候...</p>
          <p class="install-detail">{{ installStatus }}</p>
        </div>
        <div v-else-if="installError" class="install-error">
          <p class="error-text">安装失败</p>
          <p class="error-detail">{{ installError }}</p>
          <a-button type="primary" @click="currentStep = 2">返回重试</a-button>
        </div>
        <div v-else class="install-success">
          <div class="success-icon">✓</div>
          <h3>安装完成！</h3>
          <p>管理员账户和默认空间已创建完成。</p>
          <a-button type="primary" size="large" @click="goToHome">
            进入系统
          </a-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../store/auth'

const API_BASE = ''
const router = useRouter()
const authStore = useAuthStore()

const currentStep = ref(0)

// Step 1: Database
const dbConfig = ref({
  dbType: 'PostgreSQL',
  host: 'localhost',
  port: 5432,
  database: 'confluencelite',
  username: 'postgres',
  password: ''
})
const testing = ref(false)
const connectionTested = ref(false)
const testResult = ref(null)

// Step 2: Admin
const adminConfig = ref({
  username: 'admin',
  password: '',
  confirmPassword: '',
  email: '',
  displayName: 'Admin'
})

// Step 3: Space
const spaceConfig = ref({
  name: '',
  key: ''
})

// 当管理员用户名变化时，自动填充空间名和key
watch(() => adminConfig.value.username, (val) => {
  if (val) {
    spaceConfig.value.name = val
    spaceConfig.value.key = val.toUpperCase()
  }
}, { immediate: true })

// Step 4: Install
const installing = ref(false)
const installError = ref(null)
const installResult = ref(null)
const installStatus = ref('')

async function testConnection() {
  testing.value = true
  testResult.value = null
  try {
    const res = await fetch(`${API_BASE}/api/setup/test-connection`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(dbConfig.value)
    })
    const data = await res.json()
    testResult.value = data.data
    connectionTested.value = data.data?.success === true
  } catch (e) {
    testResult.value = { success: false, error: `请求失败: ${e.message}` }
    connectionTested.value = false
  } finally {
    testing.value = false
  }
}

function nextStep() {
  if (currentStep.value < 2) {
    currentStep.value++
  }
}

function prevStep() {
  if (currentStep.value > 0) {
    currentStep.value--
  }
}

async function runInstall() {
  currentStep.value = 3
  installing.value = true
  installError.value = null
  installStatus.value = '保存配置...'

  const payload = {
    database: { ...dbConfig.value },
    adminUsername: adminConfig.value.username,
    adminPassword: adminConfig.value.password,
    adminEmail: adminConfig.value.email || null,
    adminDisplayName: adminConfig.value.displayName || null,
    spaceName: spaceConfig.value.name,
    spaceKey: spaceConfig.value.key
  }

  try {
    installStatus.value = '正在初始化数据库...'
    const res = await fetch(`${API_BASE}/api/setup/install`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    const data = await res.json()

    if (data.success && data.data) {
      installStatus.value = '安装完成！'
      installResult.value = data.data
      installing.value = false
    } else {
      installError.value = data.message || '安装失败'
      installing.value = false
    }
  } catch (e) {
    installError.value = `请求失败: ${e.message}`
    installing.value = false
  }
}

function goToHome() {
  if (installResult.value) {
    const spaceKey = spaceConfig.value.key.toUpperCase()
    authStore.setFromSetup(
      installResult.value,
      adminConfig.value.displayName || adminConfig.value.username,
      spaceKey
    )
    window.__resetInstallCheck()
    window.location.href = `/${spaceKey}`
  }
}
</script>

<style scoped>
.setup-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d47a1 0%, #1565c0 50%, #1976d2 100%);
  position: relative;
  overflow: hidden;
}

.setup-background .bg-shape {
  position: absolute;
  border-radius: 50%;
  opacity: 0.1;
  background: white;
}
.shape-1 { width: 600px; height: 600px; top: -200px; right: -100px; }
.shape-2 { width: 400px; height: 400px; bottom: -100px; left: -50px; }

.setup-card {
  width: 500px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
  padding: 40px;
  position: relative;
  z-index: 1;
}

.setup-header {
  text-align: center;
  margin-bottom: 32px;
}

.brand {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  margin-bottom: 16px;
}
.brand-icon {
  width: 40px; height: 40px;
  background: linear-gradient(135deg, #0049b0, #1976d2);
  color: white;
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 22px; font-weight: 700;
}
.brand-name {
  font-size: 22px; font-weight: 700; color: #172b4d;
}

.setup-title {
  font-size: 20px; color: #172b4d; margin: 0 0 4px;
}
.setup-subtitle {
  color: #6b778c; font-size: 14px; margin: 0;
}

.step-content h3 {
  font-size: 18px; color: #172b4d; margin: 0 0 16px;
}
.step-desc {
  color: #6b778c; font-size: 14px; margin-bottom: 20px;
}

.form-row {
  display: flex; gap: 16px;
}
.form-item-flex { flex: 1; }
.form-item-port { width: 140px; }

.step-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #ebecf0;
}

.test-result {
  margin-top: 12px;
  padding: 10px 14px;
  border-radius: 6px;
  font-size: 13px;
}
.test-result.success {
  background: #e3fcef; color: #006644; border: 1px solid #abf5d0;
}
.test-result.error {
  background: #ffebe6; color: #bf2600; border: 1px solid #ffbdad;
}

.install-progress {
  text-align: center;
  padding: 40px 0;
}
.install-text {
  font-size: 16px; color: #172b4d; margin-top: 16px;
}
.install-detail {
  font-size: 13px; color: #6b778c; margin-top: 4px;
}

.install-error {
  text-align: center;
  padding: 40px 0;
}
.error-text {
  font-size: 18px; color: #bf2600; font-weight: 600;
}
.error-detail {
  font-size: 13px; color: #6b778c; margin: 8px 0 20px;
}

.install-success {
  text-align: center;
  padding: 30px 0;
}
.success-icon {
  width: 64px; height: 64px;
  background: #36b37e;
  color: white;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 32px;
  margin: 0 auto 16px;
}
.install-success h3 {
  font-size: 20px; color: #172b4d; text-align: center;
}
.install-success p {
  color: #6b778c; margin-bottom: 24px;
}
</style>
