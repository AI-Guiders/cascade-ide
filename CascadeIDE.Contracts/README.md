# CascadeIDE.Contracts

Контракты SDK для внутреннего расширения CascadeIDE (см. `docs/adr/0024-ide-sdk-and-stable-contracts.md`).

## Stability

- По умолчанию новые контракты относятся к `CascadeIDE.Contracts.Experimental.*`.
- Осознанно “опорные” контракты живут в `CascadeIDE.Contracts.Stable.*`.
- Дополнительно используется атрибут `ApiStabilityAttribute` + enum `ApiStability` (internal clarity, не публичное обещание совместимости).

## Capabilities

Минимальные контракты для code-first регистрации возможностей:

- `ICascadeFeatureModule`
- `ICapabilityRegistry`
- `CapabilityMap` + дескрипторы capabilities

