const assert = require('assert')
const {
  CARRIER_SITE_OPTIONS,
  CARRIER_ACTIONS,
  createCarrierPayload,
  getCarrierActionConfig
} = require('../src/carrierSiteConfig')

const expectedPortsByLine = {
  O1: ['6061', '6060', '3080', '3081'],
  O2: ['6011', '6010', '1080', '1081'],
  O3: ['6051', '6050', '2050', '2051'],
  O4: ['6031', '6030', '4120', '4121']
}

assert.deepStrictEqual(
  CARRIER_SITE_OPTIONS[0].children.map(({ label, value }) => ({ label, value })),
  expectedPortsByLine.O1.map(port => ({ label: port, value: port }))
)

assert.deepStrictEqual(
  CARRIER_SITE_OPTIONS,
  Object.entries(expectedPortsByLine).map(([line, ports]) => ({
    label: line,
    value: line,
    children: ports.map(port => ({
      label: port,
      value: port
    }))
  }))
)

const payload = createCarrierPayload('6061', '  SHELF-001  ')

assert.deepStrictEqual(payload, {
  carrierCode: 'SHELF-001',
  siteCode: '6061'
})
assert.strictEqual(
  Object.prototype.hasOwnProperty.call(payload, 'carrierDir'),
  false
)

assert.deepStrictEqual(getCarrierActionConfig(CARRIER_ACTIONS.BIND), {
  action: 'bind',
  label: '绑定',
  path: '/ecs/agv/rcs/carrier/bind'
})

assert.deepStrictEqual(getCarrierActionConfig(CARRIER_ACTIONS.UNBIND), {
  action: 'unbind',
  label: '解绑',
  path: '/ecs/agv/rcs/carrier/unbind'
})

console.log('carrierSiteConfig tests passed')
