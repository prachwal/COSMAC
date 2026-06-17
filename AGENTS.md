# AGENTS.md — RCA CDP1802 Emulator

## Cel projektu
Cycle-accurate emulator procesora RCA CDP1802 (COSMAC) w C# .NET 10.
Docelowo: symulacja sondy kosmicznej z lat 70.–80. (AMSAT, MAGSAT, UoSAT).

## Stack technologiczny
- C# / .NET 10
- xUnit (testy)
- Brak zewnętrznych zależności (poza xUnit)

## Struktura projektu
```
src/Cdp1802.Core/    # Biblioteka - procesor, pamięć, peryferia
src/Cdp1802.Cli/     # CLI do testów ręcznych
tests/Cdp1802.Tests/ # Testy xUnit
```

## Workflow — Test Driven Development (TDD)

### Zasady
1. **RED** — Napisz test opisujący zachowanie (test powinien się wysypać)
2. **GREEN** — Naimplementuj minimalny kod, który przepuszcza test
3. **REFACTOR** — Oczyść kod zachowując wszystkie testy

### Kolejność implementacji
1. Rejestry i flagi (D, DF, P, X, Q, IE, T)
2. MemoryBus (odczyt/zapis)
3. Fetch instrukcji (opcode → I + N)
4. Dekodowanie i wykonanie instrukcji (switch po I)
5. Licznik cykli (TotalCycles)
6. DMA i Interrupt (między instrukcjami)
7. Peryferia (Timer, UART, GPIO)

## Polecenia
```bash
dotnet build Cdp1802.sln        # Build
dotnet test Cdp1802.sln         # Testy
dotnet test --filter "FullyQualifiedName~Xxx"  # Pojedynczy test
dotnet run --project src/Cdp1802.Cli           # CLI
```

## Konwencje kodu
- Namespace: `Cdp1802.Core`, `Cdp1802.Cli`, `Cdp1802.Tests`
- Nazewnictwo: PascalCase dla public, _camelCase dla private fields
- Pliki: jeden typ na plik
- Brak komentarzy chyba że wymagane (algorytmy, decyzje architektoniczne)
- file-scoped namespaces

## Kluczowe typy
- `Cdp1802` — Hauptklasa procesora (16 rejestrów R0-RF, D, DF, P, X, T, Q, IE)
- `MemoryBus` — 64KB pamięci
- `IPeripheral` — interfejs dla urządzeń I/O

## Testy — oczekiwania
- Każdy nowy element powinien mieć test
- Testy spójności (build, bin, struktura) już istnieją
- TDD: najpierw test, potem implementacja

## Ryzyka
- Cycle-accuracy wymaga precyzyjnego modelowania stanów (S0-S3)
- DMA/Interrupt między instrukcjami — kolejność priorytetów
- Instrukcje Long Branch (3 cykle) vs pozostałe (2 cykle)

## Status
- [x] Infrastruktura (solution, projekty, .editorconfig, .gitignore)
- [x] Stub Cdp1802, MemoryBus, IPeripheral
- [x] Testy spójności (13 testów)
- [ ] Implementacja procesora (TDD)
- [ ] Peryferia
