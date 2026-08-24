const PORTS_BY_LINE = {
  O1: ['6061', '6060', '3080', '3081'],
  O2: ['6011', '6010', '1080', '1081'],
  O3: ['6051', '6050', '2050', '2051'],
  O4: ['6031', '6030', '4045', '4046']
}

const CARRIER_SITE_OPTIONS = Object.entries(PORTS_BY_LINE).map(([line, ports]) => ({
  label: line,
  value: line,
  children: ports.map(port => ({
    label: port,
    value: port
  }))
}))

const CARRIER_ACTIONS = Object.freeze({
  BIND: 'bind',
  UNBIND: 'unbind'
})

const CARRIER_ACTION_CONFIG = Object.freeze({
  [CARRIER_ACTIONS.BIND]: Object.freeze({
    action: CARRIER_ACTIONS.BIND,
    label: '绑定',
    path: '/ecs/agv/rcs/carrier/bind'
  }),
  [CARRIER_ACTIONS.UNBIND]: Object.freeze({
    action: CARRIER_ACTIONS.UNBIND,
    label: '解绑',
    path: '/ecs/agv/rcs/carrier/unbind'
  })
})

function createCarrierPayload(siteCode, carrierCode) {
  return {
    carrierCode: carrierCode.trim(),
    siteCode: siteCode.trim()
  }
}

function getCarrierActionConfig(action) {
  return CARRIER_ACTION_CONFIG[action] || CARRIER_ACTION_CONFIG[CARRIER_ACTIONS.BIND]
}

module.exports = {
  CARRIER_SITE_OPTIONS,
  CARRIER_ACTIONS,
  createCarrierPayload,
  getCarrierActionConfig
}
