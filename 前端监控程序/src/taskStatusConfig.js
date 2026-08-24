const TASK_STATUS_OPTIONS = Object.freeze([
  { value: 'Created', label: '创建' },
  { value: 'Submitted', label: '已提交' },
  { value: 'RcsFailed', label: 'RCS下发失败' },
  { value: 'ArrivePreparePosition1', label: '到达取预备货位' },
  { value: 'ArriveDockStation1', label: '到达取货位' },
  { value: 'PickComplete', label: '取货完成' },
  { value: 'RetreatPreparePosition1', label: '退回取货预备位' },
  { value: 'ArrivePreparePosition2', label: '到达放货预备位' },
  { value: 'ArriveDockStation2', label: '到达放货位' },
  { value: 'PlaceComplete', label: '放货完成' },
  { value: 'RetreatPreparePosition2', label: '退回放货预备位' },
  { value: 'Cancelling', label: '取消处理中' },
  { value: 'CancelRecoveryRequired', label: '取消待恢复' },
  { value: 'Cancelled', label: '已取消' },
  { value: 'Completed', label: '完成' },
])

const TASK_STATUS_LABELS = Object.freeze(
  TASK_STATUS_OPTIONS.reduce((labels, option) => {
    labels[option.value] = option.label
    return labels
  }, {})
)

const TASK_STATUS_FILTER_OPTIONS = Object.freeze([
  { value: '未完成', label: '未完成（汇总）' },
  ...TASK_STATUS_OPTIONS,
])

module.exports = {
  TASK_STATUS_FILTER_OPTIONS,
  TASK_STATUS_LABELS,
  TASK_STATUS_OPTIONS,
}
