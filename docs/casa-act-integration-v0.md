# CASA act integration in Cascade IDE (v0)

**Payload:** `casa-ontology-payload`  
**Related:** [CASA-ADR-0008](../../casa-ontology-payload/design/CASA-ADR-0008-layout-parity-bilingual-t9.md)

## Data flow

```text
KeyEvent.code → PhysicalKeyEvent[] (Casa.Act.Core)
             → L0_vk_map.ltm decode (field memory)
             → layout_tick / cide_act_bridge_v0.py
             → preview_chip JSON → CIDE UI
```

## Stdio bridge

```bash
python tools/cide_act_bridge_v0.py serve
```

Request with physical keys:

```json
{"op":"tick","events":[{"key_id":"Slash","action":"down"},{"key_id":"KeyA","action":"down"}]}
```

## Core ltm boot

Profile `bilingual-layout-parity.v0.json` references:

```json
"core_ltm": { "ltm_dir": "lab/agent-core/vk-layouts-v0/ltm" }
```

See [agent-core README](../../casa-ontology-payload/lab/agent-core/README.md).
