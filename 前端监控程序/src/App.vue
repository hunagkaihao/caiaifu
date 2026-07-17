<template>
  <div class="app-container">
    <div class="page-header">
      <h1 class="page-title">ECS AGV-PLC设备点位监控</h1>
      <div class="page-meta">
        <span class="meta-item">{{ lastRefreshText }}</span>
      </div>
    </div>

    <div class="line-tabs-wrap">
      <el-tabs v-model="activeTab" type="card" class="line-tabs">
        <el-tab-pane v-for="line in lines" :key="line" :label="line" :name="line" />
        <el-tab-pane label="任务管理" name="tasks" />
        <el-tab-pane label="点位管理" name="work-positions" />
        <el-tab-pane label="货架管理" :name="TAB_CARRIER_MANAGEMENT" />
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
          <div v-for="(row, rowIndex) in activePointRows" :key="'pr-' + rowIndex" class="point-row">
            <div v-for="p in row" :key="p.pointCode" class="point-block point-block-layout">
              <div class="point-main-col">
                <div class="point-head">
                  <span class="point-code">{{ displayPointCode(p.pointCode) }}</span>
                  <el-tag :type="p.tcpConnected ? 'success' : 'danger'" size="mini">
                    {{ p.tcpConnected ? 'TCP 已连接' : 'TCP 断开' }}
                  </el-tag>
                  <div class="point-run-group">
                    <el-tag size="mini" :type="runStateTagType(p.runState)">{{ runStateLabel(p.runState) }}</el-tag>
                    <el-button type="warning" plain size="mini" :loading="!!resettingPoints[p.pointCode]"
                      @click="resetPointRunState(p)">
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
                  <div v-for="f in seqFields" :key="f.key" class="seq-line">
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
          <el-form-item label="起点端口">
            <el-input v-model="taskFilter.sourcePointCode" placeholder="如 6061" clearable style="width: 120px"
              @keyup.enter.native="onTaskSearch" />
          </el-form-item>
          <el-form-item label="终点端口">
            <el-input v-model="taskFilter.targetPointCode" placeholder="如 3080" clearable style="width: 120px"
              @keyup.enter.native="onTaskSearch" />
          </el-form-item>
          <el-form-item label="任务状态">
            <el-select v-model="taskFilter.taskStatus" placeholder="全部" clearable style="width: 160px">
              <el-option v-for="option in taskStatusOptions" :key="option.value" :label="option.label"
                :value="option.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="创建时间起">
            <el-date-picker v-model="taskFilter.timeStart" type="datetime" placeholder="起始时间"
              value-format="yyyy-MM-dd HH:mm:ss" format="yyyy-MM-dd HH:mm:ss" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item label="创建时间止">
            <el-date-picker v-model="taskFilter.timeEnd" type="datetime" placeholder="结束时间"
              value-format="yyyy-MM-dd HH:mm:ss" format="yyyy-MM-dd HH:mm:ss" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="taskLoading" @click="onTaskSearch">查询</el-button>
            <el-button @click="onTaskFilterReset">重置</el-button>
          </el-form-item>
        </el-form>

        <div class="task-meta">
          共 <strong>{{ taskTotal }}</strong> 条 · 每页 {{ taskPageSize }} 条
        </div>
        <el-table v-loading="taskLoading" :data="taskItems" :row-key="taskRowKey" stripe border size="small"
          class="task-table" empty-text="暂无任务">
          <el-table-column prop="id" label="任务ID" min-width="280" show-overflow-tooltip />
          <el-table-column label="起点" min-width="100" show-overflow-tooltip>
            <template slot-scope="scope">
              {{ displayPointCode(scope.row.sourcePointCode) }}
            </template>
          </el-table-column>
          <el-table-column label="终点" min-width="100" show-overflow-tooltip>
            <template slot-scope="scope">
              {{ displayPointCode(scope.row.targetPointCode) }}
            </template>
          </el-table-column>
          <el-table-column label="边" width="120" align="center" show-overflow-tooltip>
            <template slot-scope="scope">
              {{ displayTaskEdgeCode(scope.row) }}
            </template>
          </el-table-column>
          <el-table-column prop="status" label="状态" min-width="140" align="center">
            <template slot-scope="scope">
              <el-tag size="mini" :type="taskStatusTagType(scope.row.status)">
                {{ taskStatusLabel(scope.row.status) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" min-width="160">
            <template slot-scope="scope">
              {{ formatUtc(scope.row.creationTime) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="270" align="center" fixed="right">
            <template slot-scope="scope">
              <el-button type="text" size="small" class="btn-danger-text" :disabled="!canCancelTask(scope.row)"
                :loading="!!cancellingTasks[scope.row.id]" @click="confirmCancelTransportTask(scope.row)">
                取消任务
              </el-button>
              <el-button v-if="canResumeTaskZones(scope.row)" type="text" size="small"
                :loading="!!resumingTaskZones[scope.row.id]" @click="confirmResumeTaskZones(scope.row)">
                恢复区域
              </el-button>
              <el-button v-if="canResumeFaultTaskZonesRow(scope.row)" type="text" size="small"
                :loading="!!resumingFaultTaskZones[scope.row.id]" @click="confirmResumeFaultTaskZones(scope.row)">
                恢复故障区域
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="task-pager">
          <el-pagination background layout="total, sizes, prev, pager, next, jumper" :current-page.sync="taskPage"
            :page-sizes="[10, 20, 50]" :page-size.sync="taskPageSize" :total="taskTotal"
            @current-change="fetchTransportTasks" @size-change="onTaskPageSizeChange" />
        </div>
      </el-card>

      <el-card v-else-if="activeTab === TAB_CARRIER_MANAGEMENT" class="line-card" shadow="hover">
        <div slot="header" class="line-card-header">
          <span class="line-badge">货架</span>
          <span class="line-title">货架与机台管理</span>
        </div>
        <el-form ref="carrierManagementFormRef" :model="carrierManagementForm" :rules="carrierManagementRules"
          label-width="96px" size="small" class="carrier-management-form">
          <el-form-item label="操作类型">
            <el-radio-group v-model="carrierManagementAction" size="small">
              <el-radio-button :label="CARRIER_ACTIONS.BIND">绑定</el-radio-button>
              <el-radio-button :label="CARRIER_ACTIONS.UNBIND">解绑</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="机台端口" prop="siteCode">
            <el-cascader v-model="carrierManagementForm.siteCode" :options="carrierSiteOptions"
              :props="carrierSiteCascaderProps" placeholder="请选择机台端口" clearable style="width: 100%" />
          </el-form-item>
          <el-form-item label="货架编号" prop="carrierCode">
            <el-input v-model="carrierManagementForm.carrierCode" placeholder="请输入或扫描货架编号" maxlength="64" clearable
              @keyup.enter.native="submitCarrierManagement" />
          </el-form-item>
          <el-form-item>
            <el-button :type="carrierManagementAction === CARRIER_ACTIONS.BIND ? 'primary' : 'danger'"
              :icon="carrierManagementAction === CARRIER_ACTIONS.BIND ? 'el-icon-link' : 'el-icon-unlock'"
              :loading="carrierManagementSubmitting" @click="submitCarrierManagement">
              确认{{ getCarrierActionConfig(carrierManagementAction).label }}
            </el-button>
          </el-form-item>
        </el-form>
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
          <el-table v-loading="workPositionLoading" :data="workPositionItems" row-key="id" stripe border size="small"
            class="task-table" empty-text="暂无点位">
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
                <el-button type="text" size="small" class="btn-danger-text"
                  @click="confirmDeleteWorkPosition(scope.row)">
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-card>
    </div>

    <el-dialog :title="workPositionDialogMode === 'add' ? '新增点位' : '编辑点位'" :visible.sync="workPositionDialogVisible"
      width="480px" :close-on-click-modal="false" @closed="resetWorkPositionForm">
      <el-form ref="workPositionFormRef" :model="workPositionForm" :rules="workPositionFormRules" label-width="96px"
        size="small">
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
  const {
    CARRIER_SITE_OPTIONS,
    CARRIER_ACTIONS,
    createCarrierPayload,
    getCarrierActionConfig,
  } = require('./carrierSiteConfig')
  const {
    canResumeCancelledTaskZones,
    canResumeFaultTaskZones,
  } = require('./taskActionState')
  const {
    TASK_STATUS_FILTER_OPTIONS,
    TASK_STATUS_LABELS,
  } = require('./taskStatusConfig')

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

  // PLC 设备端口映射
  const LINES = ['O1', 'O2', 'O3', 'O4']
  const TAB_TASKS = 'tasks'
  const TAB_WORK_POSITIONS = 'work-positions'
  const TAB_CARRIER_MANAGEMENT = 'carrier-management'
  const POINT_PORT_CODES = {
    O1A: '6061',
    O1B: '6060',
    O1C: '3080',
    O1D: '3081',
    O2A: '6011',
    O2B: '6010',
    O2C: '1080',
    O2D: '1081',
    O3A: '6051',
    O3B: '6050',
    O3C: '2050',
    O3D: '2051',
    O4A: '6031',
    O4B: '6030',
    O4C: '4120',
    O4D: '4121',
  }
  const PORT_POINT_CODES = Object.keys(POINT_PORT_CODES).reduce((acc, pointCode) => {
    acc[POINT_PORT_CODES[pointCode]] = pointCode
    return acc
  }, {})

  const WORK_POSITION_STATUS_OPTIONS = ['可用', '禁用']

  function validateCarrierCode(rule, value, callback) {
    if (!String(value || '').trim()) {
      callback(new Error('请输入货架编号'))
      return
    }
    callback()
  }

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
    { key: 'seq6_Spare', label: '⑥关门完成' },
    { key: 'seq7_ReadyPosition', label: '⑦允许进入' },
    { key: 'seq8_WorkingPosition', label: '⑧允许离开' },
    { key: 'seq9_RequestPickupTask', label: '⑨请求取货' },
    { key: 'seq10_RequestPlaceTask', label: '⑩请求放货' },
  ]

  export default {
    data() {
      return {
        TAB_WORK_POSITIONS,
        TAB_CARRIER_MANAGEMENT,
        CARRIER_ACTIONS,
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
        cancellingTasks: {},
        resumingTaskZones: {},
        resumingFaultTaskZones: {},
        taskFilter: createDefaultTaskFilter(),
        taskStatusOptions: TASK_STATUS_FILTER_OPTIONS,
        carrierSiteOptions: CARRIER_SITE_OPTIONS,
        carrierSiteCascaderProps: { emitPath: false },
        carrierManagementAction: CARRIER_ACTIONS.BIND,
        carrierManagementSubmitting: false,
        carrierManagementForm: {
          siteCode: '',
          carrierCode: '',
        },
        carrierManagementRules: {
          siteCode: [{ required: true, message: '请选择机台端口', trigger: 'change' }],
          carrierCode: [{ validator: validateCarrierCode, trigger: ['blur', 'change'] }],
        },
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
        if (s === '2') return '运行状态：暂停'
        return s ? `运行状态：${s}` : '运行状态：—'
      },
      runStateTagType(val) {
        const s = val !== undefined && val !== null ? String(val).trim() : ''
        if (s === '1') return 'danger'
        return 'info'
      },
      displayPointCode(pointCode) {
        const code = pointCode !== undefined && pointCode !== null ? String(pointCode).trim() : ''
        return POINT_PORT_CODES[code] || code || '—'
      },
      normalizeTaskPointFilter(value) {
        const code = value !== undefined && value !== null ? String(value).trim() : ''
        return PORT_POINT_CODES[code] || code
      },
      displayTaskEdgeCode(row) {
        if (!row) return '—'
        const source = this.displayPointCode(row.sourcePointCode)
        const target = this.displayPointCode(row.targetPointCode)
        if (source !== '—' && target !== '—') {
          return `${source}-${target}`
        }
        return row.edgeCode || '—'
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
          .catch(() => { })
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
      getCarrierActionConfig,
      carrierManagementUrl(action) {
        const path = getCarrierActionConfig(action).path
        return this.apiRoot ? `${this.apiRoot}${path}` : path
      },
      async submitCarrierManagement() {
        const formRef = this.$refs.carrierManagementFormRef
        if (!formRef) return
        try {
          await formRef.validate()
        } catch (e) {
          return
        }

        const actionConfig = getCarrierActionConfig(this.carrierManagementAction)
        const payload = createCarrierPayload(
          this.carrierManagementForm.siteCode,
          this.carrierManagementForm.carrierCode
        )
        try {
          await this.$confirm(
            `确认将货架「${payload.carrierCode}」与机台端口「${payload.siteCode}」${actionConfig.label}？`,
            `${actionConfig.label}确认`,
            {
              confirmButtonText: `确认${actionConfig.label}`,
              cancelButtonText: '取消',
              type: actionConfig.action === CARRIER_ACTIONS.UNBIND ? 'warning' : 'info',
            }
          )
        } catch (e) {
          return
        }

        this.carrierManagementSubmitting = true
        try {
          const res = await fetch(this.carrierManagementUrl(actionConfig.action), {
            method: 'POST',
            headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
          })
          const text = await res.text()
          let body = {}
          try {
            const parsedBody = text ? JSON.parse(text) : {}
            body = parsedBody && typeof parsedBody === 'object' && !Array.isArray(parsedBody)
              ? parsedBody
              : { message: typeof parsedBody === 'string' ? parsedBody : '' }
          } catch (e) {
            body = { message: text }
          }
          if (!res.ok) {
            throw new Error(`HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`)
          }
          if (!(body.code === 'SUCCESS' || body.success === true)) {
            const code = body.code ? `[${body.code}] ` : ''
            throw new Error(`${code}${body.message || `${actionConfig.label}失败`}`)
          }
          this.$message.success(body.message || `${actionConfig.label}成功`)
          this.carrierManagementForm.carrierCode = ''
          formRef.clearValidate('carrierCode')
        } catch (e) {
          this.$message.error((e && e.message) || String(e))
        } finally {
          this.carrierManagementSubmitting = false
        }
      },
      transportTasksUrl() {
        const path = '/ecs/agv-transport-tasks'
        const q = new URLSearchParams({
          page: String(this.taskPage),
          pageSize: String(this.taskPageSize),
        })
        const f = this.taskFilter
        const source = this.normalizeTaskPointFilter(f.sourcePointCode)
        const target = this.normalizeTaskPointFilter(f.targetPointCode)
        const status = f.taskStatus ? String(f.taskStatus).trim() : ''
        if (source) q.set('sourcePointCode', source)
        if (target) q.set('targetPointCode', target)
        if (status) q.set('taskStatus', status)
        if (f.timeStart) q.set('timeStart', f.timeStart)
        if (f.timeEnd) q.set('timeEnd', f.timeEnd)
        const qs = q.toString()
        return this.apiRoot ? `${this.apiRoot}${path}?${qs}` : `${path}?${qs}`
      },
      cancelTransportTaskUrl(id) {
        const path = `/ecs/agv-transport-tasks/${encodeURIComponent(id)}/cancel`
        return this.apiRoot ? `${this.apiRoot}${path}` : path
      },
      resumeTaskZonesUrl(id) {
        const path = `/ecs/agv-transport-tasks/${encodeURIComponent(id)}/resume-zones`
        return this.apiRoot ? `${this.apiRoot}${path}` : path
      },
      resumeFaultTaskZonesUrl(id) {
        const path = `/ecs/agv-transport-tasks/${encodeURIComponent(id)}/resume-fault-zones`
        return this.apiRoot ? `${this.apiRoot}${path}` : path
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
      canCancelTask(row) {
        if (!row || !row.id) return false
        const status = row.status !== undefined && row.status !== null ? String(row.status).trim() : ''
        return status !== '' &&
          status !== 'Created' &&
          status !== 'Completed' &&
          status !== 'Cancelled' &&
          status !== 'RcsFailed'
      },
      canResumeTaskZones(row) {
        return canResumeCancelledTaskZones(row)
      },
      canResumeFaultTaskZonesRow(row) {
        return canResumeFaultTaskZones(row)
      },
      async confirmCancelTransportTask(row) {
        if (!this.canCancelTask(row)) return
        const id = row.id
        const source = this.displayPointCode(row.sourcePointCode)
        const target = this.displayPointCode(row.targetPointCode)
        try {
          await this.$confirm(
            `确认取消任务 ${id}？起点 ${source}，终点 ${target}。`,
            '取消任务确认',
            {
              confirmButtonText: '确认取消',
              cancelButtonText: '返回',
              type: 'warning',
            }
          )
        } catch (e) {
          return
        }

        this.$set(this.cancellingTasks, id, true)
        try {
          const res = await fetch(this.cancelTransportTaskUrl(id), {
            method: 'POST',
            headers: {
              Accept: 'application/json',
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ reason: '前端手动取消任务' }),
          })
          const text = await res.text()
          let body = {}
          try {
            body = text ? JSON.parse(text) : {}
          } catch (e) {
            body = { message: text }
          }
          if (!res.ok) {
            throw new Error(`HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`)
          }
          if (body && body.success === false) {
            throw new Error(body.message || '取消任务失败')
          }
          this.$message.success((body && body.message) || '取消任务成功')
          await this.fetchTransportTasks()
        } catch (e) {
          this.$message.error((e && e.message) || String(e))
        } finally {
          this.$delete(this.cancellingTasks, id)
        }
      },
      async confirmResumeTaskZones(row) {
        if (!this.canResumeTaskZones(row)) return
        const id = row.id
        const sourceZone = row.sourceZoneCode || this.displayPointCode(row.sourcePointCode)
        const targetZone = row.targetZoneCode || this.displayPointCode(row.targetPointCode)
        try {
          await this.$confirm(
            `确认恢复任务 ${id} 的暂停区域？起点区域 ${sourceZone}，终点区域 ${targetZone}。`,
            '恢复区域确认',
            {
              confirmButtonText: '确认恢复',
              cancelButtonText: '返回',
              type: 'warning',
            }
          )
        } catch (e) {
          return
        }

        this.$set(this.resumingTaskZones, id, true)
        try {
          const res = await fetch(this.resumeTaskZonesUrl(id), {
            method: 'POST',
            headers: { Accept: 'application/json' },
          })
          const text = await res.text()
          let body = {}
          try {
            body = text ? JSON.parse(text) : {}
          } catch (e) {
            body = { message: text }
          }
          if (!res.ok) {
            throw new Error(`HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`)
          }
          if (body && body.success === false) {
            throw new Error(body.message || '恢复区域失败')
          }
          this.$message.success((body && body.message) || '恢复区域成功')
          await this.fetchTransportTasks()
        } catch (e) {
          this.$message.error((e && e.message) || String(e))
        } finally {
          this.$delete(this.resumingTaskZones, id)
        }
      },
      async confirmResumeFaultTaskZones(row) {
        if (!this.canResumeFaultTaskZonesRow(row)) return
        const id = row.id
        const sourceZone = row.sourceZoneCode || this.displayPointCode(row.sourcePointCode)
        const targetZone = row.targetZoneCode || this.displayPointCode(row.targetPointCode)
        try {
          await this.$confirm(
            `确认恢复任务 ${id} 的故障暂停区域？起点区域 ${sourceZone}，终点区域 ${targetZone}。`,
            '恢复故障区域确认',
            {
              confirmButtonText: '确认恢复',
              cancelButtonText: '返回',
              type: 'warning',
            }
          )
        } catch (e) {
          return
        }

        this.$set(this.resumingFaultTaskZones, id, true)
        try {
          const res = await fetch(this.resumeFaultTaskZonesUrl(id), {
            method: 'POST',
            headers: { Accept: 'application/json' },
          })
          const text = await res.text()
          let body = {}
          try {
            body = text ? JSON.parse(text) : {}
          } catch (e) {
            body = { message: text }
          }
          if (!res.ok) {
            throw new Error(`HTTP ${res.status} ${res.statusText}：${text.slice(0, 200)}`)
          }
          if (body && body.success === false) {
            throw new Error(body.message || '恢复故障区域失败')
          }
          this.$message.success((body && body.message) || '恢复故障区域成功')
          await this.fetchTransportTasks()
        } catch (e) {
          this.$message.error((e && e.message) || String(e))
        } finally {
          this.$delete(this.resumingFaultTaskZones, id)
        }
      },
      taskStatusLabel(status) {
        if (status === undefined || status === null || status === '') return '—'
        const key = String(status).trim()
        return TASK_STATUS_LABELS[key] || key
      },
      taskStatusTagType(status) {
        const key = status !== undefined && status !== null ? String(status).trim() : ''
        if (key === 'RcsFailed' || key === 'CancelRecoveryRequired') return 'danger'
        if (key === 'Completed') return 'success'
        if (key === 'Cancelled') return 'warning'
        return 'info'
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
    --surface: #ffffff;
    --surface-soft: #f7f9fc;
    --surface-muted: #eef3f8;
    --line: #dfe7f0;
    --line-strong: #c8d6e5;
    --text-main: #223044;
    --text-soft: #64748b;
    --primary: #2f80ed;
    --success: #2f9e44;
    --warning: #d97706;
    --danger: #e5484d;

    padding: 20px 32px 32px;
    background:
      linear-gradient(180deg, #f7f9fc 0%, #edf2f7 100%);
    min-height: 100vh;
    box-sizing: border-box;
    width: 100%;
    max-width: 1920px;
    margin: 0 auto;
    color: var(--text-main);
  }

  .page-header {
    position: relative;
    margin-bottom: 14px;
    padding-bottom: 14px;
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 12px 16px;
    border-bottom: 1px solid rgba(200, 214, 229, 0.86);
  }

  .page-title {
    margin: 0;
    font-size: 22px;
    font-weight: 600;
    line-height: 1.35;
    color: var(--text-main);
    flex: 1;
    min-width: 240px;
  }

  .page-meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: flex-end;
    gap: 12px;
    color: var(--text-soft);
    font-size: 13px;
    flex-shrink: 0;
  }

  .meta-item {
    position: relative;
    display: inline-flex;
    align-items: center;
    min-height: 28px;
    padding: 4px 10px 4px 28px;
    border: 1px solid var(--line);
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.78);
    box-shadow: 0 6px 16px rgba(34, 48, 68, 0.05);
    box-sizing: border-box;
  }

  .meta-item::before {
    content: "";
    position: absolute;
    left: 11px;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--success);
    box-shadow: 0 0 0 4px rgba(47, 158, 68, 0.12);
    animation: statusPulse 2.4s ease-in-out infinite;
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

  .line-tabs>>>.el-tabs__header {
    margin-bottom: 0;
    border-bottom: 0;
  }

  .line-tabs>>>.el-tabs__nav-scroll {
    overflow-x: auto;
    overflow-y: hidden;
  }

  .line-tabs>>>.el-tabs__nav-scroll::-webkit-scrollbar {
    height: 4px;
  }

  .line-tabs>>>.el-tabs__nav-scroll::-webkit-scrollbar-thumb {
    background: #c8d6e5;
    border-radius: 999px;
  }

  .line-tabs>>>.el-tabs__nav-wrap {
    padding: 3px;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.78);
    box-shadow: 0 8px 20px rgba(34, 48, 68, 0.05);
  }

  .line-tabs>>>.el-tabs__nav-wrap::after {
    display: none;
  }

  .line-tabs>>>.el-tabs__nav {
    border: 0;
    white-space: nowrap;
  }

  .line-tabs>>>.el-tabs__item {
    height: 36px;
    line-height: 36px;
    margin-right: 3px;
    border: 0;
    border-radius: 6px;
    color: var(--text-soft);
    transition: color 0.18s ease, background-color 0.18s ease, box-shadow 0.18s ease;
  }

  .line-tabs>>>.el-tabs__item:hover {
    color: var(--primary);
    background: #eef6ff;
  }

  .line-tabs>>>.el-tabs__item.is-active {
    color: var(--primary);
    background: #fff;
    box-shadow: 0 4px 12px rgba(47, 128, 237, 0.16);
  }

  .line-card {
    overflow: hidden;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: var(--surface);
    box-shadow: 0 14px 34px rgba(34, 48, 68, 0.08);
    animation: surfaceIn 0.28s ease both;
  }

  .line-card>>>.el-card__header {
    padding: 10px 16px;
    min-height: 0;
    border-bottom: 1px solid var(--line);
    background: linear-gradient(90deg, #f8fbff 0%, #ffffff 68%);
  }

  .line-card>>>.el-card__body {
    padding: 16px;
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
    min-height: 24px;
    padding: 2px 9px;
    border-radius: 4px;
    background: var(--primary);
    color: #fff;
    font-size: 14px;
    line-height: 1;
    box-shadow: 0 6px 14px rgba(47, 128, 237, 0.22);
  }

  .line-title {
    flex: 1;
    font-size: 16px;
    color: var(--text-main);
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .line-hint-error {
    display: inline-flex;
    align-items: center;
    min-height: 22px;
    padding: 2px 8px;
    border-radius: 4px;
    background: #fff1f2;
    font-size: 12px;
    color: var(--danger);
    animation: hintBlink 1.8s ease-in-out infinite;
  }

  .line-error-msg {
    color: var(--danger);
    padding: 10px 12px;
    background: #fff1f2;
    border: 1px solid #ffd7dc;
    border-radius: 6px;
    font-size: 13px;
    animation: surfaceIn 0.2s ease both;
  }

  .line-empty {
    color: var(--text-soft);
    font-size: 14px;
    padding: 28px 12px;
    text-align: center;
    border: 1px dashed var(--line-strong);
    border-radius: 8px;
    background: var(--surface-soft);
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
    animation: rowRise 0.3s ease both;
  }

  .point-row:nth-child(2) {
    animation-delay: 0.04s;
  }

  .point-row:nth-child(3) {
    animation-delay: 0.08s;
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
    position: relative;
    padding: 14px;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: linear-gradient(180deg, #ffffff 0%, #f9fbfd 100%);
    box-shadow: 0 8px 20px rgba(34, 48, 68, 0.05);
    transition: transform 0.18s ease, border-color 0.18s ease, box-shadow 0.18s ease;
  }

  .point-block::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 3px;
    background: linear-gradient(90deg, var(--primary), #67c23a, #e6a23c);
    opacity: 0.82;
  }

  .point-block:hover {
    transform: translateY(-2px);
    border-color: #bdd7f6;
    box-shadow: 0 14px 28px rgba(34, 48, 68, 0.1);
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
    width: clamp(184px, 18vw, 236px);
    padding-left: 14px;
    border-left: 1px solid var(--line);
  }

  .seq-title-v {
    writing-mode: vertical-rl;
    text-orientation: mixed;
    font-size: 13px;
    font-weight: 600;
    color: var(--text-soft);
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
    width: 100%;
  }

  /* 标签 / 冒号 / 数值分列对齐，避免长短不一 */
  .seq-line {
    display: grid;
    grid-template-columns: 7em 0.65em 2ch;
    align-items: center;
    column-gap: 2px;
    padding: 6px 10px;
    font-size: 13px;
    line-height: 1.35;
    color: #2468b4;
    border: 1px solid #d6e8fb;
    background: #eef7ff;
    border-radius: 4px;
    box-sizing: border-box;
    min-width: 0;
    transition: border-color 0.16s ease, background-color 0.16s ease, transform 0.16s ease;
  }

  .seq-line:hover {
    transform: translateX(2px);
    border-color: #b9d8f8;
    background: #f6fbff;
  }

  .seq-label {
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    text-align: left;
  }

  .seq-sep {
    color: var(--text-soft);
    text-align: center;
  }

  .seq-val {
    text-align: right;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
    color: var(--text-main);
  }

  @media (max-width: 900px) {
    .point-block-layout {
      flex-direction: column;
    }

    .point-seq-col {
      width: 100%;
      border-left: none;
      border-top: 1px solid var(--line);
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
    margin-bottom: 10px;
  }

  .point-run-group {
    display: inline-flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 6px 8px;
  }

  .point-code {
    font-weight: bold;
    font-size: 17px;
    color: var(--text-main);
    margin-right: 8px;
    letter-spacing: 0;
  }

  .point-head>>>.el-tag {
    border-radius: 4px;
    font-weight: 600;
  }

  .point-head>>>.el-button {
    transition: transform 0.16s ease, box-shadow 0.16s ease;
  }

  .point-head>>>.el-button:hover {
    transform: translateY(-1px);
  }

  .summary {
    font-size: 14px;
    color: var(--text-main);
    line-height: 1.5;
    margin-bottom: 12px;
    padding: 9px 10px;
    border-radius: 6px;
    background: #f6f8fb;
    border: 1px solid #edf1f6;
    overflow-wrap: anywhere;
  }

  .desc-main>>>.el-descriptions__label {
    width: 132px;
    color: var(--text-soft);
    background: #f8fafc;
  }

  .desc-main>>>.el-descriptions__content {
    color: var(--text-main);
  }

  .desc-main>>>.el-descriptions__cell {
    transition: background-color 0.16s ease;
  }

  .desc-main>>>.el-descriptions__cell:hover {
    background: #fbfdff;
  }

  .mono {
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-variant-numeric: tabular-nums;
  }

  .raw-frame {
    word-break: break-all;
    overflow-wrap: anywhere;
  }

  .task-badge {
    background: var(--success);
    box-shadow: 0 6px 14px rgba(47, 158, 68, 0.2);
  }

  .task-meta {
    margin-bottom: 12px;
    font-size: 13px;
    color: var(--text-soft);
  }

  .task-meta strong {
    color: var(--text-main);
    font-weight: 700;
  }

  .task-filter-form {
    margin-bottom: 14px;
    padding: 14px 14px 4px;
    background: var(--surface-soft);
    border: 1px solid var(--line);
    border-radius: 8px;
  }

  .task-filter-form>>>.el-form-item {
    margin-bottom: 10px;
  }

  .task-filter-form>>>.el-form-item__label {
    color: var(--text-soft);
  }

  .task-filter-form>>>.el-input__inner,
  .task-filter-form>>>.el-range-input {
    transition: border-color 0.16s ease, box-shadow 0.16s ease;
  }

  .task-filter-form>>>.el-input__inner:focus {
    border-color: var(--primary);
    box-shadow: 0 0 0 3px rgba(47, 128, 237, 0.12);
  }

  .task-table {
    width: 100%;
    border-radius: 8px;
    overflow: hidden;
  }

  .task-table>>>.cell {
    line-height: 1.45;
  }

  .task-table>>>.el-table__body-wrapper {
    overflow-x: auto;
  }

  .task-table>>>.el-table__header th {
    background: #f7fafc;
    color: var(--text-soft);
    font-weight: 600;
  }

  .task-table>>>.el-table__row {
    transition: background-color 0.16s ease;
  }

  .task-table>>>.el-table__body tr:hover>td {
    background: #f3f8ff;
  }

  .task-table>>>.el-table__fixed-right::before,
  .task-table>>>.el-table__fixed::before {
    background: var(--line);
  }

  .task-table>>>.el-tag {
    border-radius: 4px;
    font-weight: 600;
  }

  .task-pager {
    margin-top: 16px;
    display: flex;
    justify-content: flex-end;
    flex-wrap: wrap;
  }

  .task-pager>>>.el-pagination {
    white-space: normal;
  }

  .work-position-badge {
    background: var(--warning);
    box-shadow: 0 6px 14px rgba(217, 119, 6, 0.2);
  }

  .work-position-card .line-card-header {
    flex-wrap: wrap;
  }

  .carrier-management-form {
    max-width: 520px;
    padding: 4px 0;
  }

  .btn-danger-text {
    color: var(--danger);
    transition: color 0.16s ease, transform 0.16s ease;
  }

  .btn-danger-text:hover {
    color: #ff6b6f;
    transform: translateY(-1px);
  }

  .app-container>>>.el-button {
    border-radius: 6px;
    transition: transform 0.16s ease, box-shadow 0.16s ease, border-color 0.16s ease;
  }

  .app-container>>>.el-button:hover {
    transform: translateY(-1px);
  }

  .app-container>>>.el-button--primary {
    background: var(--primary);
    border-color: var(--primary);
    box-shadow: 0 6px 14px rgba(47, 128, 237, 0.18);
  }

  .app-container>>>.el-dialog {
    width: min(480px, calc(100vw - 32px)) !important;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 24px 60px rgba(34, 48, 68, 0.22);
  }

  .app-container>>>.el-dialog__header {
    padding: 16px 20px 12px;
    border-bottom: 1px solid var(--line);
    background: #f8fbff;
  }

  .app-container>>>.el-dialog__body {
    padding: 20px;
  }

  .app-container>>>.el-dialog__footer {
    padding: 12px 20px 16px;
    border-top: 1px solid var(--line);
    background: #fbfdff;
  }

  @keyframes surfaceIn {
    from {
      opacity: 0;
      transform: translateY(8px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  @keyframes rowRise {
    from {
      opacity: 0;
      transform: translateY(6px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  @keyframes statusPulse {

    0%,
    100% {
      box-shadow: 0 0 0 4px rgba(47, 158, 68, 0.12);
    }

    50% {
      box-shadow: 0 0 0 7px rgba(47, 158, 68, 0.04);
    }
  }

  @keyframes hintBlink {

    0%,
    100% {
      opacity: 1;
    }

    50% {
      opacity: 0.72;
    }
  }

  @media (max-width: 1280px) {
    .app-container {
      padding: 18px 22px 28px;
    }

    .point-block-layout {
      flex-direction: column;
    }

    .point-seq-col {
      width: 100%;
      border-left: none;
      border-top: 1px solid var(--line);
      padding-left: 0;
      padding-top: 12px;
    }

    .seq-tags-v {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }

  @media (max-width: 980px) {
    .page-header {
      align-items: flex-start;
    }

    .page-title {
      min-width: 100%;
    }

    .page-meta {
      width: 100%;
      justify-content: flex-start;
    }

    .line-card-header {
      align-items: flex-start;
      flex-wrap: wrap;
    }

    .line-title {
      flex-basis: calc(100% - 62px);
    }

    .line-hint-error {
      margin-left: 50px;
    }

    .task-filter-form {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0 12px;
    }

    .task-filter-form>>>.el-form-item {
      display: flex;
      flex-direction: column;
      margin-right: 0;
    }

    .task-filter-form>>>.el-form-item__label {
      float: none;
      width: auto;
      padding: 0 0 6px;
      line-height: 1.2;
      text-align: left;
    }

    .task-filter-form>>>.el-form-item__content {
      display: block;
      width: 100%;
      line-height: normal;
    }

    .task-filter-form>>>.el-input,
    .task-filter-form>>>.el-select,
    .task-filter-form>>>.el-date-editor.el-input {
      width: 100% !important;
    }

    .task-table>>>.el-table__header,
    .task-table>>>.el-table__body {
      min-width: 760px;
    }
  }

  @media (max-width: 768px) {
    .app-container {
      padding: 14px;
    }

    .page-title {
      font-size: 19px;
    }

    .line-tabs>>>.el-tabs__item {
      padding: 0 12px;
    }

    .line-card>>>.el-card__body {
      padding: 12px;
    }

    .line-badge {
      min-width: 36px;
      min-height: 22px;
      font-size: 13px;
    }

    .line-title {
      flex-basis: 100%;
      order: 2;
      font-size: 15px;
    }

    .line-hint-error {
      order: 3;
      margin-left: 0;
    }

    .point-block {
      padding: 12px;
    }

    .point-head {
      align-items: flex-start;
    }

    .point-code {
      width: 100%;
      margin-right: 0;
      font-size: 16px;
    }

    .point-run-group {
      width: 100%;
    }

    .seq-tags-v {
      grid-template-columns: 1fr;
    }

    .seq-line {
      grid-template-columns: minmax(0, 1fr) 0.65em 2ch;
    }

    .task-filter-form {
      grid-template-columns: 1fr;
      padding: 12px 12px 2px;
    }

    .task-filter-form>>>.el-form-item:last-child .el-form-item__content {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .task-filter-form>>>.el-form-item:last-child .el-button {
      flex: 1 1 112px;
      margin-left: 0;
    }

    .task-pager {
      justify-content: flex-start;
    }

    .task-pager>>>.el-pagination {
      display: flex;
      align-items: center;
      gap: 6px;
      flex-wrap: wrap;
    }

    .app-container>>>.el-dialog__body {
      padding: 16px;
    }
  }

  @media (max-width: 560px) {
    .app-container {
      padding: 10px;
    }

    .page-header {
      gap: 10px;
      margin-bottom: 12px;
      padding-bottom: 12px;
    }

    .page-title {
      font-size: 18px;
      line-height: 1.35;
    }

    .meta-item {
      width: 100%;
      justify-content: flex-start;
      font-size: 12px;
    }

    .line-tabs {
      margin-bottom: 12px;
    }

    .line-tabs>>>.el-tabs__item {
      height: 32px;
      line-height: 32px;
      padding: 0 10px;
      font-size: 13px;
    }

    .line-card>>>.el-card__header {
      padding: 10px 12px;
    }

    .line-card>>>.el-card__body {
      padding: 10px;
    }

    .points-stack,
    .point-row {
      gap: 10px;
    }

    .summary {
      font-size: 13px;
    }

    .desc-main>>>.el-descriptions__body,
    .desc-main>>>.el-descriptions__table,
    .desc-main>>>tbody,
    .desc-main>>>tr,
    .desc-main>>>td {
      display: block;
      width: 100% !important;
      box-sizing: border-box;
    }

    .desc-main>>>.el-descriptions__label {
      display: block;
      width: 100%;
      padding: 8px 10px 4px;
      border-right: 0;
    }

    .desc-main>>>.el-descriptions__content {
      display: block;
      width: 100%;
      padding: 4px 10px 8px;
    }

    .task-table>>>.el-table__header,
    .task-table>>>.el-table__body {
      min-width: 680px;
    }

    .task-pager>>>.el-pagination .el-pagination__jump {
      margin-left: 0;
    }

    .app-container>>>.el-dialog {
      width: calc(100vw - 20px) !important;
      margin-top: 8vh !important;
    }

    .app-container>>>.el-dialog__header,
    .app-container>>>.el-dialog__body,
    .app-container>>>.el-dialog__footer {
      padding-left: 14px;
      padding-right: 14px;
    }
  }

  @media (prefers-reduced-motion: reduce) {

    .line-card,
    .point-row,
    .line-hint-error,
    .meta-item::before {
      animation: none;
    }

    .point-block,
    .seq-line,
    .app-container>>>.el-button,
    .btn-danger-text {
      transition: none;
    }
  }
</style>