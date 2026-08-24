const assert = require('assert')
const {
  canResumeCancelledTaskZones,
  canResumeFaultTaskZones
} = require('../src/taskActionState')

const cancelled = {
  id: 'cancelled-task',
  status: 'Cancelled',
  canResumeZones: true,
  canResumeFaultZones: true
}
assert.strictEqual(canResumeCancelledTaskZones(cancelled), true)
assert.strictEqual(canResumeFaultTaskZones(cancelled), false)

const faultPaused = {
  id: 'fault-task',
  status: 'Submitted',
  canResumeZones: false,
  canResumeFaultZones: true
}
assert.strictEqual(canResumeCancelledTaskZones(faultPaused), false)
assert.strictEqual(canResumeFaultTaskZones(faultPaused), true)

for (const status of ['Cancelling', 'CancelRecoveryRequired', 'Cancelled']) {
  assert.strictEqual(
    canResumeFaultTaskZones({ id: status, status, canResumeFaultZones: true }),
    false
  )
}

assert.strictEqual(
  canResumeFaultTaskZones({ id: 'normal-task', status: 'Submitted', canResumeFaultZones: false }),
  false
)

console.log('taskActionState tests passed')
