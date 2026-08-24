const CANCELLATION_STATUSES = new Set([
  'Cancelling',
  'CancelRecoveryRequired',
  'Cancelled'
])

function taskStatus(row) {
  if (!row || row.status === undefined || row.status === null) return ''
  return String(row.status).trim()
}

function canResumeCancelledTaskZones(row) {
  return !!row && !!row.id && taskStatus(row) === 'Cancelled' && row.canResumeZones === true
}

function canResumeFaultTaskZones(row) {
  const status = taskStatus(row)
  return !!row &&
    !!row.id &&
    !CANCELLATION_STATUSES.has(status) &&
    row.canResumeFaultZones === true
}

module.exports = {
  canResumeCancelledTaskZones,
  canResumeFaultTaskZones
}
