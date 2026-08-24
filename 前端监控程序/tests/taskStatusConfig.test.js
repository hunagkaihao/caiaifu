const assert = require('assert')
const {
  TASK_STATUS_FILTER_OPTIONS,
  TASK_STATUS_LABELS,
  TASK_STATUS_OPTIONS,
} = require('../src/taskStatusConfig')

const expectedStatuses = [
  'Created',
  'Submitted',
  'RcsFailed',
  'ArrivePreparePosition1',
  'ArriveDockStation1',
  'PickComplete',
  'RetreatPreparePosition1',
  'ArrivePreparePosition2',
  'ArriveDockStation2',
  'PlaceComplete',
  'RetreatPreparePosition2',
  'Cancelling',
  'CancelRecoveryRequired',
  'Cancelled',
  'Completed',
]

assert.deepStrictEqual(
  TASK_STATUS_OPTIONS.map(option => option.value),
  expectedStatuses
)
assert.strictEqual(new Set(expectedStatuses).size, TASK_STATUS_OPTIONS.length)
assert.strictEqual(TASK_STATUS_LABELS.Created, '创建')
assert.strictEqual(TASK_STATUS_LABELS.Submitted, '已提交')
assert.strictEqual(TASK_STATUS_LABELS.RcsFailed, 'RCS下发失败')
assert.strictEqual(TASK_STATUS_LABELS.Cancelled, '已取消')
assert.strictEqual(TASK_STATUS_LABELS.Completed, '完成')
assert.deepStrictEqual(TASK_STATUS_FILTER_OPTIONS[0], {
  value: '未完成',
  label: '未完成（汇总）',
})
assert.deepStrictEqual(
  TASK_STATUS_FILTER_OPTIONS.slice(1),
  TASK_STATUS_OPTIONS
)

console.log('taskStatusConfig tests passed')
