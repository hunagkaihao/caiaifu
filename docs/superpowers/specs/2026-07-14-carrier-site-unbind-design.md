# Carrier Site Bind and Unbind Design

**Goal:** Add RCS carrier-to-site bind and unbind proxy APIs, and add a Vue + Element UI page for operators to unbind a manually entered shelf code from a selected machine port.

**Scope:** The backend exposes both bind and unbind operations required by the RCS integration. The frontend exposes only the unbind operation. No local binding table, carrier-state pre-query, or site-tree API is added.

## Terminology and Field Contract

- `carrierCode` is the shelf identifier entered manually by the operator or supplied by a scanner.
- `siteCode` is the machine identifier sent to RCS. Its value is the mapped numeric port, such as `6061`, not the internal PLC point name `O1A`.
- `carrierDir` remains empty for bind requests and is not part of the unbind request.
- The unbind request body contains only the required business values:

```json
{
  "carrierCode": "SHELF-001",
  "siteCode": "6061"
}
```

## Site Cascader

The frontend uses a two-level Element UI Cascader. The first level is the production area and the second level is the numeric machine port. Both the displayed leaf label and its value are the port number. The Cascader uses `emitPath: false`, so the form model receives only the selected port string.

```text
O1 -> 6061, 6060, 3080, 3081
O2 -> 6011, 6010, 1080, 1081
O3 -> 6051, 6050, 2050, 2051
O4 -> 6031, 6030, 4120, 4121
```

Internal point codes such as `O1A` remain available to existing monitoring code but are not displayed, selected, or sent by the unbind form.

## Backend Design

Follow the existing RCS request, response, client, and controller conventions.

1. Add carrier-site request DTOs under `Ecs.Application.Contracts/Rcs` with the same `System.Text.Json` annotations and nullable style used by existing RCS contracts.
2. Extend `IRcsApiClient` with bind and unbind methods.
3. Extend `RcsApiClient` through the existing generic `PostInternalAsync` path:
   - bind: `api/robot/controller/carrier/bind`
   - unbind: `api/robot/controller/carrier/unbind`
4. Add mock-success responses consistent with the existing `AgvEnabled=false` behavior.
5. Extend `RcsAgvController` with ECS proxy routes:
   - `POST /ecs/agv/rcs/carrier/bind`
   - `POST /ecs/agv/rcs/carrier/unbind`
6. Return the existing `RcsApiResponse<object>` shape so RCS `code` and `message` remain available to the frontend.

The backend performs only basic input validation. It does not query the current carrier binding or add local business-state validation.

## Frontend Design

Add a new `载具解绑` tab/card to the existing single-page `App.vue`, matching its current Element UI layout, method naming, API helper style, loading state, and Chinese comments.

The form contains:

- `机台号`: two-level Cascader whose leaf labels and values are numeric ports.
- `货架编号`: manual/scanner-compatible text input bound to `carrierCode`.
- `确认解绑`: submits the form after Element UI validation and operator confirmation.

Validation is intentionally limited to:

- a leaf machine port must be selected;
- the trimmed shelf code must not be empty;
- the shelf code is limited to 64 characters, matching the RCS document.

On submission, the frontend sends the selected numeric port as `siteCode`. It must not convert the value back to `O1A` or send `carrierDir`.

## Error Handling

- Treat RCS `code == "SUCCESS"` or `success == true` as success.
- On a business failure, display the returned `code` and `message` with Element UI's error message component.
- On an HTTP or network failure, display the existing frontend request-error text pattern.
- Always clear the loading state in `finally`.
- Do not add carrier/site mismatch pre-validation.

## Testing and Verification

Backend verification covers:

- bind and unbind DTO serialization;
- correct relative RCS paths and request bodies;
- mock-success behavior when `AgvEnabled=false`;
- controller routing and solution compilation.

Frontend verification covers:

- Cascader leaf labels and values are numeric ports;
- choosing `O1 -> 6061` produces `siteCode: "6061"`;
- `carrierCode` is trimmed and required;
- `carrierDir` is absent from the unbind payload;
- success and RCS failure messages are displayed;
- lint and production build pass.

## Non-Goals

- No frontend bind operation.
- No backend site-tree endpoint.
- No local carrier-site binding persistence.
- No carrier binding query before unbind.
- No change to existing PLC point monitoring or transport-task behavior.
