<template>
  <div class="app-container">
    <div class="page-header">
      <h1 class="page-title">AGV-PLC设备点位监控</h1>
      <div class="page-meta">
        <span class="meta-item">{{ lastRefreshText }}</span>
      </div>
    </div>

    <div class="line-tabs-wrap">
      <el-tabs v-model="activeTab" type="card" class="line-tabs">
        <el-tab-pane v-for="line in lines" :key="line" :label="line" :name="line" />
        <el-tab-pane label="任务管理" name="tasks" />
        <el-tab-pane label="点位管理" name="work-positions" />
      </el-tabs>

      <el-card v-if="isPlcTab" class="line-card" shadow="hover">
        <div slot="header" class="line-card-header">
          <span class="line-badge">{{ activeTab }}</span>
          <span class="line-title">模组监控</span>
          <span v-if="lineErrors[activeTab]" class="line-hint-error">本轮请求失败</span>
        </div>

        <div v-if="lineErrors[activeTab]" class="line-error-msg">
          {{ lineErrors[activeTab] }}
        </div>

        <div v-else-if="!(pointsByLine[activeTab] || []).length" class="line-empty">
          暂无点位数据
        </div>

        <div v-else class="points-stack">
          <div
            v-for="(row, rowIndex) in activePointRows"
            :key="'pr-' + rowIndex"
            class="point-row"
          >
            <div v-for="p in row" :key="p.pointCode" class="point-block point-block-layout">
              <div class="point-main-col">
                <div class="point-head">
                  <span class="point-code">{{ p.pointCode }}</span>
                  <el-tag :type="p.tcpConnected ? 'success' : 'danger'" size="mini">
                    {{ p.tcpConnected ? 'TCP 已连接' : 'TCP 断开' }}
                  </el-tag>
                  <div class="point-run-group">
                    <el-tag size="mini" :type="runStateTagType(p.runState)">{{ runStateLabel(p.runState) }}</el-tag>
                    <el-button
                      type="warning"
                      plain
                      size="mini"
                      :loading="!!resettingPoints[p.pointCode]"
                      @click="resetPointRunState(p)"
                    >
                      复位
                    </el-button>
                  </div>
                </div>

                <div class="summary">{{ p.statusActiveSummary || '—' }}</div>

                <el-descriptions :column="2" size="small" border class="desc-main">
                  <el-descriptions-item label="首字节(Hex)">
                    <span class="mono">{{ p.firstByteHex }}</span>
                  </el-descriptions-item>
                  <el-descriptions-item label="首字节(二进制)">
                    <span class="mono">{{ p.firstByteBinary }}</span>
                  </el-descriptions-item>
                  <el-descriptions-item label="原始帧" :span="2">
                    <span class="mono raw-frame">{{ p.rawFrameHex }}</span>
                  </el-descriptions-item>
                  <el-descriptions-item label="最近错误" :span="2">
                    <span class="mono">{{ p.lastError || '无' }}</span>
                  </el-descriptions-item>
                  <el-descriptions-item label="更新时间(UTC)" :span="2">
                    {{ formatUtc(p.lastUpdateUtc) }}
                  </el-descriptions-item>
                </el-descriptions>
              </div>

              <aside class="point-seq-col">
                
                <div class="seq-tags-v">
                  <div
                    v-for="f in seqFields"
                    :key="f.key"
                    class="seq-line"
                  >
                    <span class="seq-label">{{ f.label }}</span>
                    <span class="seq-sep" aria-hidden="true">：</span>
                    <span class="seq-val mono">{{ bitText(p[f.key]) }}</span>
                  </div>
                </div>
              </aside>
            </div>
          </div>
        </div>
      </el-card>

      <el-card v-else-if="activeTab === 'tasks'" class="line-card task-card" shadow="hover">
        <div slot="header" class="line-card-header">
          <span class="line-badge task-badge">任务</span>
          <span class="line-title">搬运任务列表（AgvTransportTasks）</span>
          <span v-if="taskListError" class="line-hint-error">加载失败</span>
        </div>

        <div v-if="taskListError" class="line-error-msg">
          {{ taskListError }}
        </div>

        <el-form class="task-filter-form" :inline="true" size="small" @submit.native.prevent="onTaskSearch">
          <el-form-item label="起点">
            <el-input
              v-model="taskFilter.sourcePointCode"
              placeholder="如 O1A"
              clearable
              style="width: 120px"
              @keyup.enter.native="onTaskSearch"
            />
          </el-form-item>
          <el-form-item label="终点">
            <el-input
              v-model="taskFilter.targetPointCode"
              placeholder="如 O1C"
              clearable
              style="width: 120px"
              @keyup.enter.native="onTaskSearch"
            />
          </el-form-item>
          <el-form-item label="任务状态">
            <el-select v-model="taskFilter.taskStatus" placeholder="全部" clearable style="width: 120px">
              <el-option label="完成" value="完成" />
              <el-option label="未完成" value="未完成" />
            </el-select>
          </el-form-item>
          <el-form-item label="创建时间起">
            <el-date-picker
              v-model="taskFilter.timeStart"
              type="datetime"
              placeholder="起始时间"
              value-format="yyyy-MM-dd HH:mm:ss"
              format="yyyy-MM-dd HH:mm:ss"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="创建时间止">
            <el-date-picker
              v-model="taskFilter.timeEnd"
              type="datetime"
              placeholder="结束时间"
              value-format="yyyy-MM-dd HH:mm:ss"
              format="yyyy-MM-dd HH:mm:ss"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="taskLoading" @click="onTaskSearch">查询</el-button>
            <el-button @click="onTaskFilterReset">重置</el-button>
          </el-form-item>
        </el-form>

        <div class="task-meta">
          共 <strong>{{ taskTotal }}</strong> 条 · 每页 {{ taskPageSize }} 条
        </div>
        <el-table
          v-loading="taskLoading"
          :data="taskItems"
          :row-key="taskRowKey"
          stripe
          border
          size="small"
          class="task-table"
          empty-text="暂无任务"
        >
          <el-table-column prop="id" label="任务ID" min-width="280" show-overflow-tooltip />
          <el-table-column prop="sourcePointCode" label="起点" min-width="100" show-overflow-tooltip />
          <el-table-column prop="targetPointCode" label="终点" min-width="100" show-overflow-tooltip />
          <el-table-column prop="edgeCode" label="边" width="88" align="center" />
          <el-table-column prop="status" label="状态" min-width="140" align="center">
            <template slot-scope="scope">
              <el-tag size="mini" type="info">{{ taskStatusLabel(scope.row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" min-width="160">
            <template slot-scope="scope">
              {{ formatUtc(scope.row.creationTime) }}
            </template>
          </el-table-column>
        </el-table>

        <div class="task-pager">
          <el-pagination
            background
            layout="total, sizes, prev, pager, next, jumper"
            :current-page.sync="taskPage"
            :page-sizes="[10, 20, 50]"
            :page-size.sync="taskPageSize"
            :total="taskTotal"
            @current-change="fetchTransportTasks"
            @size-change="onTaskPageSizeChange"
          />
        </div>
      </el-card>

      <el-card v-else-if="activeTab === TAB_WORK_POSITIONS" class="line-card work-position-card" shadow="hover">
        <div slot="header" class="line-card-header">
          <span class="line-badge work-position-badge">点位</span>
          <span class="line-title">工位点位管理（WorkPositions）</span>
          <el-button type="primary" size="mini" icon="el-icon-plus" @click="openWorkPositionAdd">
            新增
          </el-button>
          <span v-if="workPositionListError" class="line-hint-error">加载失败</span>
        </div>

        <div v-if="workPositionListError" class="line-error-msg">
          {{ workPositionListError }}
        </div>

        <div v-else>
          <div class="task-meta">
            共 <strong>{{ workPositionItems.length }}</strong> 条
          </div>
          <el-table
            v-loading="workPositionLoading"
            :data="workPositionItems"
            row-key="id"
            stripe
            border
            size="small"
            class="task-table"
            empty-text="暂无点位"
          >
            <el-table-column prop="id" label="ID" width="72" align="center" />
            <el-table-column prop="deviceName" label="设备名称" min-width="120" show-overflow-tooltip />
            <el-table-column prop="siteName" label="站点名称" min-width="120" show-overflow-tooltip />
            <el-table-column prop="status" label="状态" width="100" align="center">
              <template slot-scope="scope">
                <el-tag size="mini" :type="workPositionStatusTagType(scope.row.status)">
                  {{ scope.row.status }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" align="center" fixed="right">
              <template slot-scope="scope">
                <el-button type="text" size="small" @click="openWorkPositionEdit(scope.row)">编辑</el-button>
                <el-button type="text" size="small" class="btn-danger-text" @click="confirmDeleteWorkPosition(scope.row)">
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-card>
    </div>

    <el-dialog
      :title="workPositionDialogMode === 'add' ? '新增点位' : '编辑点位'"
      :visible.sync="workPositionDialogVisible"
      width="480px"
      :close-on-click-modal="false"
      @closed="resetWorkPositionForm"
    >
      <el-form ref="workPositionFormRef" :model="workPositionForm" :rules="workPositionFormRules" label-width="96px" size="small">
        <el-form-item label="设备名称" prop="deviceName">
          <el-input v-model="workPositionForm.deviceName" placeholder="如 O1A、AGV-01" maxlength="64" clearable />
        </el-form-item>
        <el-form-item label="站点名称" prop="siteName">
          <el-input v-model="workPositionForm.siteName" placeholder="如 MDO1A、站点A" maxlength="128" clearable />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="workPositionForm.status" placeholder="请选择状态" style="width: 100%">
            <el-option label="可用" value="可用" />
            <el-option label="禁用" value="禁用" />
          </el-select>
        </el-form-item>
      </el-form>
      <div slot="footer">
        <el-button size="small" @click="workPositionDialogVisible = false">取消</el-button>
        <el-button type="primary" size="small" :loading="workPositionSaving" @click="submitWorkPosition">
          确定
        </el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script>
/** 本地开发默认直连 ECS HTTP 端口（与后端监听一致）；生产请在 .env.production 设置 VUE_APP_ECS_BASE 或网关同源反代 */
const ECS_DEFAULT_DEV = 'http://localhost:3270'

function ecsApiRoot() {
  const raw = process.env.VUE_APP_ECS_BASE
  if (raw !== undefined && raw !== null && /^proxy$/i.test(String(raw).trim())) {
    return ''
  }
  const s = raw === undefined || raw === null ? '' : String(raw).trim()
  if (s !== '') {
    return s.replace(/\/$/, '')
  }
  return process.env.NODE_ENV === 'development' ? ECS_DEFAULT_DEV : ''
}

const LINES = ['O1', 'O2', 'O3', 'O4']
const TAB_TASKS = 'tasks'
const TAB_WORK_POSITIONS = 'work-positions'

/** 搬运任务 Status 英文 → 中文展示 */
const TASK_STATUS_LABELS = {
  Submitted: '创建',
  Completed: '完成',
  ArrivePreparePosition1: '到达取预备货位',
  ArriveDockStation1: '到达取货位',
  PickComplete: '取货完成',
  RetreatPreparePosition1: '退回取货预备位',
  ArrivePreparePosition2: '到达放货预备位',
  ArriveDockStation2: '到达放货位',
  PlaceComplete: '放货完成',
  RetreatPreparePosition2: '退回放货预备位',
}

const WORK_POSITION_STATUS_OPTIONS = ['可用', '禁用']

function padDatePart(n) {
  return String(n).padStart(2, '0')
}

function formatTaskFilterDateTime(d) {
  return (
    `${d.getFullYear()}-${padDatePart(d.getMonth() + 1)}-${padDatePart(d.getDate())} ` +
    `${padDatePart(d.getHours())}:${padDatePart(d.getMinutes())}:${padDatePart(d.getSeconds())}`
  )
}

/** 任务筛选默认时间：6 天前 00:00:00 ～ 明天 23:59:59 */
function createDefaultTaskFilter() {
  const now = new Date()
  const start = new Date(now)
  start.setDate(start.getDate() - 6)
  start.setHours(0, 0, 0, 0)

  const end = new Date(now)
  end.setDate(end.getDate() + 1)
  end.setHours(23, 59, 59, 0)

  return {
    sourcePointCode: '',
    targetPointCode: '',
    taskStatus: '',
    timeStart: formatTaskFilterDateTime(start),
    timeEnd: formatTaskFilterDateTime(end),
  }
}

const SEQ_FIELDS = [
  { key: 'seq1_DeviceStatus', label: '①设备状态' },
  { key: 'seq2_AllowPickup', label: '②允许取货' },
  { key: 'seq3_AllowPlace', label: '③允许放货' },
  { key: 'seq4_CommDiagnosis', label: '④通讯诊断' },
  { key: 'seq5_EmergencyStop', label: '⑤急停' },
  { key: 'seq6_Spare', label: '⑥预留' },
  { key: 'seq7_ReadyPosition', label: '⑦就位' },
  { key: 'seq8_WorkingPosition', label: '⑧工作位' },
  { key: 'seq9_RequestPickupTask', label: '⑨请求取货' },
  { key: 'seq10_RequestPlaceTask', label: '⑩请求放货' },
]

export default {
  data() {
    return {
      TAB_WORK_POSITIONS,
      lines: LINES,
      activeTab: 'O1',
      seqFields: SEQ_FIELDS,
      apiRoot: ecsApiRoot(),
      pointsByLine: { O1: [], O2: [], O3: [], O4: [] },
      lineErrors: { O1: '', O2: '', O3: '', O4: '' },
      lastRefreshAt: null,
      pollTimer: null,
      resettingPoints: {},
      taskItems: [],
      taskTotal: 0,
      taskPage: 1,
      taskPageSize: 10,
      taskListError: '',
      taskLoading: false,
      taskFilter: createDefaultTaskFilter(),
      workPositionItems: [],
      workPositionListError: '',
      workPositionLoading: false,
      workPositionDialogVisible: false,
      workPositionDialogMode: 'add',
      workPositionSaving: false,
      workPositionForm: {
        id: null,
        deviceName: '',
        siteName: '',
        status: '可用',
      },
      workPositionFormRules: {
        deviceName: [{ required: true, message: '请输入设备名称', trigger: 'blur' }],
        siteName: [{ required: true, message: '请输入站点名称', trigger: 'blur' }],
        status: [{ required: true, message: '请选择状态', trigger: 'change' }],
      },
    }
  },
  computed: {
    isPlcTab() {
      return this.lines.indexOf(this.activeTab) >= 0
    },
    lastRefreshText() {
      return this.lastRefreshAt ? this.formatUtc(this.lastRefreshAt) : '尚未刷新'
    },
    /** 每行两个点位：第一行 A+B，第二行 C+D（按 pointCode 排序后两两分组） */
    activePointRows() {
      if (!this.isPlcTab) return []
      const raw = this.pointsByLine[this.activeTab] || []
      const list = [...raw].sort((a, b) => String(a.pointCode).localeCompare(String(b.pointCode)))
      const rows = []
      for (let i = 0; i < list.length; i += 2) {
        rows.push(list.slice(i, i + 2))
      }
      return rows
    },
  },
  watch: {
    activeTab(val) {
      if (this.lines.indexOf(val) >= 0) {
        this.refreshActiveLine()
      } else if (val === TAB_TASKS) {
        this.fetchTransportTasks()
      } else if (val === TAB_WORK_POSITIONS) {
        this.fetchWorkPositions()
      }
    },
  },
  mounted() {
    this.scheduleInitialFetch()
    this.pollTimer = setInterval(() => {
      this.pollCurrentTab()
    }, 3000)
  },
  beforeDestroy() {
    if (this.pollTimer) clearInterval(this.pollTimer)
  },
  methods: {
    pointsUrl(line) {
      const path = `/ecs/agv-plc/points/${encodeURIComponent(line)}`
      return this.apiRoot ? `${this.apiRoot}${path}` : path
    },
    bitText(val) {
      if (val === undefined || val === null) return '—'
      return String(val)
    },
    /** 运行状态：0 空箱，1 锁定（锁定用红色标签） */
    runStateLabel(val) {
      const s = val !== undefined && val !== null ? String(val).trim() : ''
      if (s === '0') return '运行状态：空闲'
      if (s === '1') return '运行状态：锁定'
      return s ? `运行状态：${s}` : '运行状态：—'
    },
    runStateTagType(val) {
      const s = val !== undefined && val !== null ? String(val).trim() : ''
      if (s === '1') return 'danger'
      return 'info'
    },
    resetRunStateUrl(pointCode) {
      const path = `/ecs/agv-plc/points/${encodeURIComponent(pointCode)}/reset-run-state`
      return this.apiRoot ? `${this.apiRoot}${path}` : path
    },
    async resetPointRunState(point) {
      const code = point && point.pointCode ? String(point.pointCode) : ''
      if (!code) return
      this.$set(this.resettingPoints, code, true)
      const url = this.resetRunStateUrl(code)
      try {
        const res = await fetch(url, {
          method: 'POST',
          headers: { Accept: 'application/json' },
        })
        const text = await res.text()
        let body = {}
        try {
          body = text ? JSON.parse(text) : {}
        } catch (_) {
          body = {}
        }
        if (!res.ok) {
          this.$message.error(`复位失败（${code}）：HTTP ${res.status} ${text.slice(0, 160)}`)
          return
        }
        if (body.success === false) {
          this.$message.warning(body.message || `复位未成功（${code}）`)
          return
        }
        this.$message.success(body.message || `${code} 运行状态已复位`)
        await this.fetchLine(this.activeTab)
      } catch (e) {
        this.$message.error((e && e.message) || String(e))
      } finally {
        this.$set(this.resettingPoints, code, false)
      }
    },
    formatUtc(iso) {
      if (!iso) return '—'
      try {
        const d = new Date(iso)
        if (Number.isNaN(d.getTime())) return String(iso)
        return (
          new Intl.DateTimeFormat('zh-CN', {
            timeZone: 'Asia/Shanghai',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false,
          }).format(d) 
        )
      } catch {
        return String(iso)
      }
    },
    async refreshActiveLine() {
      if (!this.isPlcTab) return
      await this.fetchLine(this.activeTab)
      this.lastRefreshAt = new Date().toISOString()
    },
    scheduleInitialFetch() {
      if (this.activeTab === TAB_TASKS) {
        this.fetchTransportTasks()
      } else if (this.activeTab === TAB_WORK_POSITIONS) {
        this.fetchWorkPositions()
      } else {
        this.refreshActiveLine()
      }
    },
    pollCurrentTab() {
      if (this.isPlcTab) {
        this.refreshActiveLine()
      }
    },
    workPositionUrl(path) {
      const p = path.startsWith('/') ? path : `/${path}`
      return this.apiRoot ? `${this.apiRoot}${p}` : p
    },
    workPositionStatusTagType(status) {
      return status === '可用' ? 'success' : 'info'
    },
    async fetchWorkPositions() {
      this.workPositionLoading = true
      this.workPositionListError = ''
      const url = this.workPositionUrl('/ecs/work-positions/all')
      try {
        const res = await fetch(url)
        const text = await res.text()
        if (!res.ok) {
          this.workPositionListError = `HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`
          this.workPositionItems = []
          return
        }
        const data = text ? JSON.parse(text) : []
        if (!Array.isArray(data)) {
          this.workPositionListError = '返回格式异常：应为数组'
          this.workPositionItems = []
          return
        }
        this.workPositionItems = data
        this.lastRefreshAt = new Date().toISOString()
      } catch (e) {
        this.workPositionListError = e && e.message ? e.message : String(e)
        this.workPositionItems = []
      } finally {
        this.workPositionLoading = false
      }
    },
    resetWorkPositionForm() {
      this.workPositionForm = {
        id: null,
        deviceName: '',
        siteName: '',
        status: '可用',
      }
      if (this.$refs.workPositionFormRef) {
        this.$refs.workPositionFormRef.clearValidate()
      }
    },
    openWorkPositionAdd() {
      this.workPositionDialogMode = 'add'
      this.resetWorkPositionForm()
      this.workPositionDialogVisible = true
    },
    openWorkPositionEdit(row) {
      this.workPositionDialogMode = 'edit'
      this.workPositionForm = {
        id: row.id,
        deviceName: row.deviceName || '',
        siteName: row.siteName || '',
        status: WORK_POSITION_STATUS_OPTIONS.indexOf(row.status) >= 0 ? row.status : '可用',
      }
      this.workPositionDialogVisible = true
      this.$nextTick(() => {
        if (this.$refs.workPositionFormRef) {
          this.$refs.workPositionFormRef.clearValidate()
        }
      })
    },
    async submitWorkPosition() {
      const formRef = this.$refs.workPositionFormRef
      if (!formRef) return
      try {
        await formRef.validate()
      } catch {
        return
      }
      const isAdd = this.workPositionDialogMode === 'add'
      const path = isAdd ? '/ecs/work-positions/add' : '/ecs/work-positions/update'
      const payload = {
        deviceName: String(this.workPositionForm.deviceName).trim(),
        siteName: String(this.workPositionForm.siteName).trim(),
        status: this.workPositionForm.status,
      }
      if (!isAdd) {
        payload.id = this.workPositionForm.id
      }
      this.workPositionSaving = true
      try {
        const res = await fetch(this.workPositionUrl(path), {
          method: 'POST',
          headers: {
            Accept: 'application/json',
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(payload),
        })
        const text = await res.text()
        let body = {}
        try {
          body = text ? JSON.parse(text) : {}
        } catch (_) {
          body = {}
        }
        if (!res.ok) {
          this.$message.error(`保存失败：HTTP ${res.status} ${text.slice(0, 160)}`)
          return
        }
        if (body.success === false) {
          this.$message.warning(body.message || '保存未成功')
          return
        }
        this.$message.success(body.message || (isAdd ? '添加成功' : '修改成功'))
        this.workPositionDialogVisible = false
        await this.fetchWorkPositions()
      } catch (e) {
        this.$message.error((e && e.message) || String(e))
      } finally {
        this.workPositionSaving = false
      }
    },
    confirmDeleteWorkPosition(row) {
      const id = row && row.id
      if (id === undefined || id === null) return
      const label = row.deviceName || row.siteName || id
      this.$confirm(`确定删除点位「${label}」（ID ${id}）？`, '删除确认', {
        type: 'warning',
        confirmButtonText: '删除',
        cancelButtonText: '取消',
      })
        .then(() => this.deleteWorkPosition(id))
        .catch(() => {})
    },
    async deleteWorkPosition(id) {
      const url = this.workPositionUrl(`/ecs/work-positions/delete?id=${encodeURIComponent(id)}`)
      try {
        const res = await fetch(url, {
          method: 'POST',
          headers: { Accept: 'application/json' },
        })
        const text = await res.text()
        let body = {}
        try {
          body = text ? JSON.parse(text) : {}
        } catch (_) {
          body = {}
        }
        if (!res.ok) {
          this.$message.error(`删除失败：HTTP ${res.status} ${text.slice(0, 160)}`)
          return
        }
        if (body.success === false) {
          this.$message.warning(body.message || '删除未成功')
          return
        }
        this.$message.success(body.message || '删除成功')
        await this.fetchWorkPositions()
      } catch (e) {
        this.$message.error((e && e.message) || String(e))
      }
    },
    transportTasksUrl() {
      const path = '/ecs/agv-transport-tasks'
      const q = new URLSearchParams({
        page: String(this.taskPage),
        pageSize: String(this.taskPageSize),
      })
      const f = this.taskFilter
      const source = f.sourcePointCode ? String(f.sourcePointCode).trim() : ''
      const target = f.targetPointCode ? String(f.targetPointCode).trim() : ''
      const status = f.taskStatus ? String(f.taskStatus).trim() : ''
      if (source) q.set('sourcePointCode', source)
      if (target) q.set('targetPointCode', target)
      if (status) q.set('taskStatus', status)
      if (f.timeStart) q.set('timeStart', f.timeStart)
      if (f.timeEnd) q.set('timeEnd', f.timeEnd)
      const qs = q.toString()
      return this.apiRoot ? `${this.apiRoot}${path}?${qs}` : `${path}?${qs}`
    },
    async fetchTransportTasks() {
      this.taskLoading = true
      this.taskListError = ''
      const url = this.transportTasksUrl()
      try {
        const res = await fetch(url)
        const text = await res.text()
        if (!res.ok) {
          this.taskListError = `HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`
          this.taskItems = []
          this.taskTotal = 0
          return
        }
        const data = text ? JSON.parse(text) : {}
        const items = data.items
        if (!Array.isArray(items)) {
          this.taskListError = '返回格式异常：缺少 items 数组'
          this.taskItems = []
          this.taskTotal = 0
          return
        }
        this.taskItems = items
        const tc = data.totalCount
        this.taskTotal = typeof tc === 'number' ? tc : parseInt(tc, 10) || 0
        this.lastRefreshAt = new Date().toISOString()
      } catch (e) {
        this.taskListError = e && e.message ? e.message : String(e)
        this.taskItems = []
        this.taskTotal = 0
      } finally {
        this.taskLoading = false
      }
    },
    onTaskPageSizeChange() {
      this.taskPage = 1
      this.fetchTransportTasks()
    },
    onTaskSearch() {
      this.taskPage = 1
      this.fetchTransportTasks()
    },
    onTaskFilterReset() {
      this.taskFilter = createDefaultTaskFilter()
      this.taskPage = 1
      this.fetchTransportTasks()
    },
    taskRowKey(row) {
      return row.id || `${row.sourcePointCode}-${row.targetPointCode}-${row.creationTime}`
    },
    taskStatusLabel(status) {
      if (status === undefined || status === null || status === '') return '—'
      const key = String(status).trim()
      return TASK_STATUS_LABELS[key] || key
    },
    async fetchLine(line) {
      const url = this.pointsUrl(line)
      try {
        const res = await fetch(url)
        const text = await res.text()
        if (!res.ok) {
          this.$set(this.lineErrors, line, `HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`)
          this.$set(this.pointsByLine, line, [])
          return
        }
        let data
        try {
          data = text ? JSON.parse(text) : []
        } catch (e) {
          this.$set(this.lineErrors, line, `返回非 JSON：${text.slice(0, 200)}`)
          this.$set(this.pointsByLine, line, [])
          return
        }
        if (!Array.isArray(data)) {
          this.$set(this.lineErrors, line, '返回格式应为数组')
          this.$set(this.pointsByLine, line, [])
          return
        }
        this.$set(this.lineErrors, line, '')
        data.sort((a, b) => String(a.pointCode).localeCompare(String(b.pointCode)))
        this.$set(this.pointsByLine, line, data)
      } catch (e) {
        this.$set(this.lineErrors, line, e && e.message ? e.message : String(e))
        this.$set(this.pointsByLine, line, [])
      }
    },
  },
}
</script>

<style scoped>
.app-container {
  padding: 16px 32px 28px;
  background-color: #f0f2f5;
  min-height: 100vh;
  box-sizing: border-box;
  width: 100%;
  max-width: 1920px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 12px;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  line-height: 1.35;
  color: #303133;
  flex: 1;
  min-width: 0;
}

.page-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  color: #606266;
  font-size: 13px;
  flex-shrink: 0;
}

.meta-item code {
  background: #f4f4f5;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
}

.line-tabs-wrap {
  width: 100%;
}

.line-tabs {
  margin-bottom: 16px;
}

.line-tabs >>> .el-tabs__header {
  margin-bottom: 0;
}

.line-card {
  border-radius: 8px;
}

.line-card >>> .el-card__header {
  padding: 8px 16px;
  min-height: 0;
}

.line-card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
}

.line-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 40px;
  padding: 2px 8px;
  border-radius: 4px;
  background: #409eff;
  color: #fff;
  font-size: 14px;
}

.line-title {
  flex: 1;
  font-size: 16px;
  color: #303133;
}

.line-hint-error {
  font-size: 12px;
  color: #f56c6c;
}

.line-error-msg {
  color: #f56c6c;
  padding: 8px;
  background: #fef0f0;
  border-radius: 4px;
  font-size: 13px;
}

.line-empty {
  color: #909399;
  font-size: 14px;
  padding: 8px;
}

.points-stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.point-row {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 16px;
}

.point-row .point-block {
  flex: 1;
  min-width: 0;
}

@media (max-width: 1100px) {
  .point-row {
    flex-direction: column;
  }
}

.point-block {
  padding: 12px;
  border: 1px solid #ebeef5;
  border-radius: 6px;
  background: #fafafa;
}

.point-block-layout {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 14px;
}

.point-main-col {
  flex: 1;
  min-width: 0;
}

.point-seq-col {
  flex-shrink: 0;
  display: flex;
  flex-direction: row;
  align-items: flex-start;
  gap: 10px;
  padding-left: 12px;
  border-left: 1px solid #e4e7ed;
}

.seq-title-v {
  writing-mode: vertical-rl;
  text-orientation: mixed;
  font-size: 13px;
  font-weight: 600;
  color: #606266;
  line-height: 1.4;
  white-space: nowrap;
  letter-spacing: 0.08em;
  padding: 4px 0;
}

.seq-tags-v {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 6px;
  min-width: 0;
  flex: 1;
}

/* 标签 / 冒号 / 数值分列对齐，避免长短不一 */
.seq-line {
  display: grid;
  grid-template-columns: 7em 0.65em 2ch;
  align-items: center;
  column-gap: 2px;
  padding: 5px 10px;
  font-size: 13px;
  line-height: 1.35;
  color: #409eff;
  border: 1px solid #d9ecff;
  background: #ecf5ff;
  border-radius: 4px;
  box-sizing: border-box;
  min-width: 0;
}

.seq-label {
  min-width: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: left;
}

.seq-sep {
  color: #909399;
  text-align: center;
}

.seq-val {
  text-align: right;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

@media (max-width: 900px) {
  .point-block-layout {
    flex-direction: column;
  }

  .point-seq-col {
    width: 100%;
    border-left: none;
    border-top: 1px solid #e4e7ed;
    padding-left: 0;
    padding-top: 12px;
    flex-direction: column;
    align-items: flex-start;
  }

  .seq-title-v {
    writing-mode: horizontal-tb;
    white-space: normal;
  }
}

.point-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.point-run-group {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 8px;
}

.point-code {
  font-weight: bold;
  font-size: 16px;
  color: #303133;
  margin-right: 8px;
}

.summary {
  font-size: 14px;
  color: #303133;
  line-height: 1.5;
  margin-bottom: 10px;
}

.desc-main >>> .el-descriptions__label {
  width: 132px;
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.raw-frame {
  word-break: break-all;
}

.task-badge {
  background: #67c23a;
}

.task-meta {
  margin-bottom: 12px;
  font-size: 13px;
  color: #606266;
}

.task-filter-form {
  margin-bottom: 12px;
  padding: 12px 12px 2px;
  background: #fafafa;
  border: 1px solid #ebeef5;
  border-radius: 6px;
}

.task-filter-form >>> .el-form-item {
  margin-bottom: 10px;
}

.task-table {
  width: 100%;
}

.task-pager {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
  flex-wrap: wrap;
}

.work-position-badge {
  background: #e6a23c;
}

.work-position-card .line-card-header {
  flex-wrap: wrap;
}

.btn-danger-text {
  color: #f56c6c;
}

.btn-danger-text:hover {
  color: #f78989;
}
</style>
