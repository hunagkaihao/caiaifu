# Carrier Site Bind and Unbind Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Proxy RCS carrier/site bind and unbind operations and add an Element UI form that unbinds a manually entered shelf code from a selected numeric machine port.

**Architecture:** Add focused RCS request contracts and reuse `RcsApiClient.PostInternalAsync` for transport, authentication, mock mode, and response parsing. Extend the existing RCS controller with two proxy endpoints. Keep the frontend site mapping in a small testable CommonJS module and integrate a new unbind card into the existing single-file Vue page.

**Tech Stack:** .NET 6, ABP, System.Text.Json, xUnit, Vue 2.6, Element UI 2.15, Node built-in assertions.

## Global Constraints

- `carrierCode` is a manually entered or scanned shelf identifier.
- `siteCode` is the numeric machine port; `O1A` is internal and must never be displayed or submitted by the unbind form.
- The Cascader hierarchy is `O1..O4 -> numeric port`, and `emitPath` is false.
- The frontend exposes only unbind; the backend exposes bind and unbind.
- `carrierDir` is null for bind and absent from unbind.
- Add only required/trim/64-character frontend validation; do not pre-query or validate current binding state.
- Preserve existing code and Chinese comment style; do not restructure unrelated `App.vue` behavior.

---

### Task 1: RCS carrier/site contracts and client transport

**Files:**
- Create: `Abp_Ecs/Ecs.Application.Contracts/Rcs/RcsCarrierSiteRequests.cs`
- Modify: `Abp_Ecs/Ecs.Application.Contracts/Rcs/IRcsApiClient.cs`
- Modify: `Abp_Ecs/Ecs.Application/Rcs/RcsApiClient.cs`
- Create: `Abp_Ecs/Ecs.Application.Tests/Rcs/RcsApiClientCarrierSiteTests.cs`

**Interfaces:**
- Produces: `RcsCarrierBindRequest`, `RcsCarrierUnbindRequest`.
- Produces: `IRcsApiClient.BindCarrierAsync` and `IRcsApiClient.UnbindCarrierAsync`, both returning `Task<RcsApiResponse<object>>`.
- Consumes: existing `RcsApiClient.PostInternalAsync` and `RcsJson.Options`.

- [ ] **Step 1: Write failing client tests**

Create a recording `HttpMessageHandler` and tests that assert the unbind path, numeric `siteCode`, shelf value, and absence of `carrierDir`:

```csharp
[Fact]
public async Task Unbind_sends_numeric_site_code_without_carrier_dir()
{
    var handler = new RecordingHandler();
    var client = CreateClient(handler, agvEnabled: true);

    var result = await client.UnbindCarrierAsync(new RcsCarrierUnbindRequest
    {
        CarrierCode = "SHELF-001",
        SiteCode = "6061"
    });

    Assert.Equal("/rcs/rtas/api/robot/controller/carrier/unbind", handler.RequestUri.AbsolutePath);
    using var json = JsonDocument.Parse(handler.Body);
    Assert.Equal("SHELF-001", json.RootElement.GetProperty("carrierCode").GetString());
    Assert.Equal("6061", json.RootElement.GetProperty("siteCode").GetString());
    Assert.False(json.RootElement.TryGetProperty("carrierDir", out _));
    Assert.Equal("SUCCESS", result.Code);
}
```

Add a bind test that asserts `/carrier/bind` and confirms null `CarrierDir` is omitted. Add a mock-mode test proving no HTTP call occurs and both methods return `SUCCESS` when `AgvEnabled=false`.

Use this recording handler and client factory in the same test class:

```csharp
private sealed class RecordingHandler : HttpMessageHandler
{
    public Uri RequestUri { get; private set; } = default!;
    public string Body { get; private set; } = string.Empty;
    public int CallCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        RequestUri = request.RequestUri!;
        Body = request.Content == null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"code\":\"SUCCESS\",\"message\":\"成功\",\"data\":{\"extra\":null}}",
                Encoding.UTF8,
                "application/json")
        };
    }
}

private static RcsApiClient CreateClient(RecordingHandler handler, bool agvEnabled)
{
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost/rcs/rtas/")
    };
    var options = Options.Create(new RcsOptions
    {
        AgvEnabled = agvEnabled,
        Host = "localhost",
        UseHttps = false
    });
    return new RcsApiClient(httpClient, options, NullLogger<RcsApiClient>.Instance);
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
dotnet test Abp_Ecs/Ecs.Application.Tests/Ecs.Application.Tests.csproj --filter RcsApiClientCarrierSiteTests
```

Expected: compilation fails because the carrier request types and client methods do not exist.

- [ ] **Step 3: Add request contracts**

```csharp
#nullable disable
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

public class RcsCarrierBindRequest
{
    [JsonPropertyName("carrierCode")]
    public string CarrierCode { get; set; }

    [JsonPropertyName("siteCode")]
    public string SiteCode { get; set; }

    [JsonPropertyName("carrierDir")]
    public int? CarrierDir { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}

public class RcsCarrierUnbindRequest
{
    [JsonPropertyName("carrierCode")]
    public string CarrierCode { get; set; }

    [JsonPropertyName("siteCode")]
    public string SiteCode { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}
```

- [ ] **Step 4: Extend the client interface and implementation**

Add:

```csharp
Task<RcsApiResponse<object>> BindCarrierAsync(
    RcsCarrierBindRequest request,
    CancellationToken cancellationToken = default);

Task<RcsApiResponse<object>> UnbindCarrierAsync(
    RcsCarrierUnbindRequest request,
    CancellationToken cancellationToken = default);
```

Implement both through `PostInternalAsync`, and add mock factories returning the existing success response shape.

- [ ] **Step 5: Run focused and full application tests**

```powershell
dotnet test Abp_Ecs/Ecs.Application.Tests/Ecs.Application.Tests.csproj --filter RcsApiClientCarrierSiteTests
dotnet test Abp_Ecs/Ecs.Application.Tests/Ecs.Application.Tests.csproj
```

Expected: all tests pass with zero failures.

### Task 2: ECS proxy controller endpoints

**Files:**
- Modify: `Abp_Ecs/Ecs.HttpApi/Controllers/Rcs/RcsAgvController.cs`

**Interfaces:**
- Consumes: `IRcsApiClient.BindCarrierAsync` and `UnbindCarrierAsync`.
- Produces: `POST /ecs/agv/rcs/carrier/bind` and `POST /ecs/agv/rcs/carrier/unbind`.

- [ ] **Step 1: Add controller methods following existing route style**

```csharp
[HttpPost("carrier/bind")]
[Produces("application/json")]
public async Task<ActionResult<RcsApiResponse<object>>> BindCarrierAsync(
    [FromBody] RcsCarrierBindRequest body,
    CancellationToken cancellationToken)
{
    body.CarrierDir = null;
    var result = await _rcsApiClient.BindCarrierAsync(body, cancellationToken);
    return Ok(result);
}

[HttpPost("carrier/unbind")]
[Produces("application/json")]
public async Task<ActionResult<RcsApiResponse<object>>> UnbindCarrierAsync(
    [FromBody] RcsCarrierUnbindRequest body,
    CancellationToken cancellationToken)
{
    var result = await _rcsApiClient.UnbindCarrierAsync(body, cancellationToken);
    return Ok(result);
}
```

Add XML comments naming the RCS paths and explaining that `siteCode` is the numeric machine port.

- [ ] **Step 2: Build the HTTP API project**

```powershell
dotnet build Abp_Ecs/Ecs.HttpApi/Ecs.HttpApi.csproj -c Debug
```

Expected: build succeeds with zero errors.

### Task 3: Testable frontend mapping and payload builder

**Files:**
- Create: `前端监控程序/src/carrierSiteConfig.js`
- Create: `前端监控程序/tests/carrierSiteConfig.test.js`
- Modify: `前端监控程序/package.json`

**Interfaces:**
- Produces: `CARRIER_SITE_OPTIONS` and `createCarrierUnbindPayload(siteCode, carrierCode)`.
- Consumed by: `App.vue` in Task 4.

- [ ] **Step 1: Write the failing Node test**

```javascript
const assert = require('assert')
const { CARRIER_SITE_OPTIONS, createCarrierUnbindPayload } = require('../src/carrierSiteConfig')

const o1 = CARRIER_SITE_OPTIONS.find(x => x.value === 'O1')
assert.deepStrictEqual(o1.children.map(x => x.label), ['6061', '6060', '3080', '3081'])
assert.deepStrictEqual(o1.children.map(x => x.value), ['6061', '6060', '3080', '3081'])
assert.deepStrictEqual(createCarrierUnbindPayload('6061', '  SHELF-001  '), {
  carrierCode: 'SHELF-001',
  siteCode: '6061',
})
assert.strictEqual(
  Object.prototype.hasOwnProperty.call(createCarrierUnbindPayload('6061', 'S1'), 'carrierDir'),
  false
)
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
npm --prefix 前端监控程序 run test:carrier-site
```

Expected: failure because the script and module do not exist.

- [ ] **Step 3: Implement mapping and payload builder**

```javascript
const PORTS_BY_LINE = {
  O1: ['6061', '6060', '3080', '3081'],
  O2: ['6011', '6010', '1080', '1081'],
  O3: ['6051', '6050', '2050', '2051'],
  O4: ['6031', '6030', '4120', '4121'],
}

const CARRIER_SITE_OPTIONS = Object.keys(PORTS_BY_LINE).map(line => ({
  value: line,
  label: line,
  children: PORTS_BY_LINE[line].map(port => ({ value: port, label: port })),
}))

function createCarrierUnbindPayload(siteCode, carrierCode) {
  return {
    carrierCode: String(carrierCode || '').trim(),
    siteCode: String(siteCode || '').trim(),
  }
}

module.exports = { CARRIER_SITE_OPTIONS, createCarrierUnbindPayload }
```

Add `"test:carrier-site": "node tests/carrierSiteConfig.test.js"` to `package.json`.

- [ ] **Step 4: Run the Node test and verify GREEN**

Run the focused script and expect exit code 0.

### Task 4: Vue carrier unbind form

**Files:**
- Modify: `前端监控程序/src/App.vue`

**Interfaces:**
- Consumes: `CARRIER_SITE_OPTIONS` and `createCarrierUnbindPayload`.
- Calls: `POST /ecs/agv/rcs/carrier/unbind` with `{ carrierCode, siteCode }`.

- [ ] **Step 1: Add constants and form state**

Require the helper module and add the validation function beside existing constants:

```javascript
const { CARRIER_SITE_OPTIONS, createCarrierUnbindPayload } = require('./carrierSiteConfig')
const TAB_CARRIER_UNBIND = 'carrier-unbind'

function validateCarrierCode(rule, value, callback) {
  if (!String(value || '').trim()) {
    callback(new Error('请输入货架编号'))
    return
  }
  callback()
}
```

Add this state to `data()`:

```javascript
TAB_CARRIER_UNBIND,
carrierSiteOptions: CARRIER_SITE_OPTIONS,
carrierSiteCascaderProps: { emitPath: false },
carrierUnbindSubmitting: false,
carrierUnbindForm: { siteCode: '', carrierCode: '' },
carrierUnbindRules: {
  siteCode: [{ required: true, message: '请选择机台号', trigger: 'change' }],
  carrierCode: [{ validator: validateCarrierCode, trigger: ['blur', 'change'] }],
},
```

- [ ] **Step 2: Add tab and card**

Add the tab:

```vue
<el-tab-pane label="载具解绑" :name="TAB_CARRIER_UNBIND" />
```

Add this card in the existing `v-if`/`v-else-if` content chain:

```vue
<el-card v-else-if="activeTab === TAB_CARRIER_UNBIND" class="line-card" shadow="hover">
  <div slot="header" class="line-card-header">
    <span class="line-badge">解绑</span>
    <span class="line-title">载具与机台解绑</span>
  </div>
  <el-form ref="carrierUnbindFormRef" :model="carrierUnbindForm" :rules="carrierUnbindRules"
    label-width="96px" size="small" class="carrier-unbind-form">
    <el-form-item label="机台号" prop="siteCode">
      <el-cascader v-model="carrierUnbindForm.siteCode" :options="carrierSiteOptions"
        :props="carrierSiteCascaderProps" placeholder="请选择机台端口" clearable style="width: 100%" />
    </el-form-item>
    <el-form-item label="货架编号" prop="carrierCode">
      <el-input v-model="carrierUnbindForm.carrierCode" placeholder="请输入或扫描货架编号"
        maxlength="64" clearable @keyup.enter.native="submitCarrierUnbind" />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" :loading="carrierUnbindSubmitting" @click="submitCarrierUnbind">
        确认解绑
      </el-button>
    </el-form-item>
  </el-form>
</el-card>
```

Add a scoped `.carrier-unbind-form` rule with the same card spacing and a maximum width of `520px`.

- [ ] **Step 3: Add confirmation and request handling**

Add these methods:

```javascript
carrierUnbindUrl() {
  const path = '/ecs/agv/rcs/carrier/unbind'
  return this.apiRoot ? `${this.apiRoot}${path}` : path
},
async submitCarrierUnbind() {
  const formRef = this.$refs.carrierUnbindFormRef
  if (!formRef) return
  try {
    await formRef.validate()
  } catch (e) {
    return
  }

  const payload = createCarrierUnbindPayload(
    this.carrierUnbindForm.siteCode,
    this.carrierUnbindForm.carrierCode
  )
  try {
    await this.$confirm(
      `确认将货架「${payload.carrierCode}」与机台「${payload.siteCode}」解绑？`,
      '解绑确认',
      { confirmButtonText: '确认解绑', cancelButtonText: '取消', type: 'warning' }
    )
  } catch (e) {
    return
  }

  this.carrierUnbindSubmitting = true
  try {
    const res = await fetch(this.carrierUnbindUrl(), {
      method: 'POST',
      headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
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
    if (!(body.code === 'SUCCESS' || body.success === true)) {
      const code = body.code ? `[${body.code}] ` : ''
      throw new Error(`${code}${body.message || '解绑失败'}`)
    }
    this.$message.success(body.message || '解绑成功')
    this.carrierUnbindForm.carrierCode = ''
    formRef.clearValidate('carrierCode')
  } catch (e) {
    this.$message.error((e && e.message) || String(e))
  } finally {
    this.carrierUnbindSubmitting = false
  }
},
```

- [ ] **Step 4: Run all frontend checks**

```powershell
npm --prefix 前端监控程序 run test:carrier-site
npm --prefix 前端监控程序 run lint
npm --prefix 前端监控程序 run build
```

Expected: focused test, lint, and production build all exit 0.

### Task 5: Full verification and handoff

**Files:**
- Verify only; no new source files.

**Interfaces:**
- Consumes all previous tasks.
- Produces fresh test/build evidence and a scoped diff summary.

- [ ] **Step 1: Run backend tests and builds**

```powershell
dotnet test Abp_Ecs/Ecs.Application.Tests/Ecs.Application.Tests.csproj
dotnet build Abp_Ecs/Ecs.HttpApi.Host/Ecs.HttpApi.Host.csproj -c Debug
```

- [ ] **Step 2: Run frontend checks**

Run the focused Node test, lint, and production build from Task 4.

- [ ] **Step 3: Inspect scoped diff**

Confirm feature changes are limited to RCS contracts/client/controller/tests, frontend helper/test/package/App.vue, and approved docs. Preserve all pre-existing unrelated changes.

- [ ] **Step 4: Request code review**

Use `superpowers:requesting-code-review`, address findings, rerun affected verification, and report remaining risks without staging or committing unrelated work.
