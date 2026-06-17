# RCA CDP1802 (COSMAC) — Cycle-Accurate Emulator
## Implementation Documentation (C# .NET)

**Cel dokumentu:**  
Przygotowanie kompletnej dokumentacji technicznej do implementacji **cycle-accurate** emulatora procesora RCA CDP1802 w języku C# (zgodnego ze stylem skillu `emulator-8bit-csharp`).

---

## Cel projektu

Głównym celem jest stworzenie **cycle-accurate emulatora procesora RCA CDP1802**, a w dalszej perspektywie — lekkiej symulacji sondy kosmicznej lub satelity z lat 70.–80., który wykorzystywał ten procesor (np. serie AMSAT/OSCAR, MAGSAT, UoSAT i podobne misje).

Dlatego emulator powinien spełniać następujące wymagania:

- **Cycle-accurate** — wierne odwzorowanie liczby cykli maszynowych (kluczowe dla realistycznego zachowania timingów)
- Dobra obsługa **DMA** i **przerwań** (jedna z najmocniejszych stron CDP1802 w zastosowaniach kosmicznych)
- Modularna architektura pozwalająca na łatwe dodawanie peryferiów (pamięć, I/O, telemetria, sensory, aktuatory)
- Przygotowanie pod symulację wyższego poziomu (model sondy kosmicznej, generowanie telemetrii, reakcja na komendy)

---

## 1. Architektura procesora

## 1. Architektura procesora

### 1.1 Ogólny opis
- 8-bitowy mikroprocesor CMOS
- 16-bitowa szyna adresowa (multipleksowana)
- Unikalna architektura oparta na **16 rejestrach 16-bitowych**
- Bardzo popularny w zastosowaniach kosmicznych (lata 70.–90.) dzięki odporności na promieniowanie i niskiemu poborowi mocy

### 1.2 Rejestry

| Nazwa | Szerokość | Opis | Uwagi |
|-------|-----------|------|-------|
| **R0 – RF** | 16-bit | 16 rejestrów ogólnego przeznaczenia | R(0) często używany jako wskaźnik DMA |
| **D** | 8-bit | Rejestr danych (akumulator) | Główny rejestr operacji arytmetyczno-logicznych |
| **DF** | 1-bit | Data Flag (Carry / Borrow) | Używany w operacjach ADD/ADC/SUB |
| **P** | 4-bit | Program Counter selector | Wskazuje który z R0–RF jest licznikiem programu |
| **X** | 4-bit | Data Pointer selector | Wskazuje który rejestr jest wskaźnikiem danych |
| **N** | 4-bit | Nibble operandu | Część kodu rozkazu (używany w wielu instrukcjach) |
| **I** | 4-bit | Major opcode | Starsza połówka pierwszego bajtu rozkazu |
| **T** | 8-bit | Temporary register | Zapisywany przy przerwaniu (X i P) |
| **Q** | 1-bit | Output flip-flop | Steruje wyjściem Q |
| **IE** | 1-bit | Interrupt Enable | Włączanie/wyłączanie przerwań |

**Uwaga:** Rejestry R0–RF są używane bardzo elastycznie — jeden z nich pełni rolę PC, inny rolę wskaźnika danych.

### 1.3 Model pamięci
- 64 KB przestrzeni adresowej (16-bit)
- Szyna adresowa multipleksowana (TPA = Address High, TPB = Address Low + Data)
- Obsługa **DMA In / DMA Out** (bardzo ważna w systemach satelitarnych)

---

## 2. Model taktowania (Cycle Accurate)

### 2.1 Podstawowe jednostki czasu
- **Machine Cycle** = **8 taktów zegara**
- Każdy cykl zawiera impulsy **TPA** i **TPB**

### 2.2 Czas trwania instrukcji

| Typ instrukcji                    | Liczba cykli maszynowych | Liczba taktów zegara | Uwagi |
|-----------------------------------|---------------------------|----------------------|-------|
| Większość instrukcji              | 2                         | 16                   | Fetch + Execute |
| Long Branch, Long Skip            | 3                         | 24                   | Fetch + 2× Execute |
| Interrupt / DMA response          | Specjalne                 | —                    | Obsługiwane między instrukcjami |

**Implementacja w emulatorze:**
- Licznik `Cycles` lub `TotalCycles` (ulong)
- Po każdej instrukcji dodawać odpowiednią liczbę cykli
- Opcjonalnie: dokładna emulacja stanów maszyny (S0, S1, S2, S3)

---

## 3. Pełny zestaw instrukcji z cyklami

### 3.1 Grupy instrukcji

#### A. Memory Reference Instructions

| Opcode | Mnemonic     | Opis                                      | Cykle | Flagi | Uwagi |
|--------|--------------|-------------------------------------------|-------|-------|-------|
| 0N     | LDN          | D ← M(R(N))                               | 2     | —     | N ≠ 0 |
| 4N     | LDA          | D ← M(R(N)); R(N)++                       | 2     | —     | — |
| F0     | LDX          | D ← M(R(X))                               | 2     | —     | — |
| 72     | LDXA         | D ← M(R(X)); R(X)++                       | 2     | —     | — |
| F8     | LDI          | D ← M(R(P)); R(P)++                       | 2     | —     | Immediate |
| 5N     | STR          | M(R(N)) ← D                               | 2     | —     | — |
| 73     | STXD         | M(R(X)) ← D; R(X)--                       | 2     | —     | — |

#### B. Register Operations

| Opcode | Mnemonic | Opis                        | Cykle | Flagi | Uwagi |
|--------|----------|-----------------------------|-------|-------|-------|
| 1N     | INC      | R(N)++                      | 2     | —     | — |
| 2N     | DEC      | R(N)--                      | 2     | —     | — |
| 8N     | GLO      | D ← R(N).low                | 2     | —     | — |
| 9N     | GHI      | D ← R(N).high               | 2     | —     | — |
| AN     | PLO      | R(N).low ← D                | 2     | —     | — |
| BN     | PHI      | R(N).high ← D               | 2     | —     | — |

#### C. Logic and Arithmetic

| Opcode | Mnemonic | Opis                              | Cykle | Flagi     | Uwagi |
|--------|----------|-----------------------------------|-------|-----------|-------|
| F1     | OR       | D ← D \| M(R(X))                  | 2     | —         | — |
| F2     | AND      | D ← D & M(R(X))                   | 2     | —         | — |
| F3     | XOR      | D ← D ⊕ M(R(X))                   | 2     | —         | — |
| F4     | ADD      | D ← D + M(R(X))                   | 2     | DF        | — |
| 74     | ADC      | D ← D + M(R(X)) + DF              | 2     | DF        | z przeniesieniem |
| F5     | SUB      | D ← M(R(X)) - D                   | 2     | DF        | — |
| 75     | SDB      | D ← M(R(X)) - D - ~DF             | 2     | DF        | — |
| F7     | SM       | D ← D - M(R(X))                   | 2     | DF        | — |
| 77     | SMB      | D ← D - M(R(X)) - ~DF             | 2     | DF        | — |

#### D. Branch Instructions (Short)

| Opcode | Mnemonic | Warunek                  | Cykle | Uwagi |
|--------|----------|--------------------------|-------|-------|
| 30     | BR       | Bezwarunkowy             | 2     | — |
| 32     | BZ       | jeśli D == 0             | 2     | — |
| 3A     | BNZ      | jeśli D != 0             | 2     | — |
| 33     | BDF      | jeśli DF == 1            | 2     | — |
| 3B     | BNF      | jeśli DF == 0            | 2     | — |
| 34     | BQ       | jeśli Q == 1             | 2     | — |
| 35     | BNQ      | jeśli Q == 0             | 2     | — |

#### E. Long Branch / Long Skip (3 cykle)

| Opcode | Mnemonic     | Opis                     | Cykle |
|--------|--------------|--------------------------|-------|
| C0     | LBR          | Long Branch              | 3     |
| C2     | LBZ          | Long Branch if Zero      | 3     |
| CA     | LBNZ         | Long Branch if Not Zero  | 3     |
| C3     | LBDF         | Long Branch if DF        | 3     |
| CB     | LBNF         | Long Branch if Not DF    | 3     |
| C4     | NOP          | No Operation (Long)      | 3     |
| C8     | LSKP         | Long Skip                | 3     |
| C9–CF  | LSxx         | Long Skip if condition   | 3     |

#### F. Control and I/O

| Opcode | Mnemonic | Opis                              | Cykle | Uwagi |
|--------|----------|-----------------------------------|-------|-------|
| D0–DF  | SEP      | P ← N                             | 2     | Zmiana PC |
| E0–EF  | SEX      | X ← N                             | 2     | Zmiana wskaźnika danych |
| 68     | —        | (zarezerwowane / rozszerzenia)    | —     | — |
| 60     | IRX      | R(X)++                            | 2     | — |
| 7X     | OUT / INP| Wyjście / Wejście                 | 2     | N = port |

**Uwaga:** Pełna lista wszystkich wariantów (szczególnie z N) znajduje się w oficjalnym datasheetcie CDP1802.

---

## 4. Proponowana struktura w C#

Ze względu na docelową symulację sondy kosmicznej, zalecana jest **modularna architektura**:

- `Cdp1802` — czysty procesor (cycle-accurate)
- `MemoryBus` / `MemoryMap` — elastyczne mapowanie pamięci
- `Peripheral` (interfejs) — baza dla urządzeń I/O i DMA
- `Spacecraft` / `ProbeSimulation` — warstwa wyższa (później)

Taka struktura pozwoli później łatwo dodać np. prosty model telemetrii, timerów pokładowych czy symulację sensorów.

### 4.1 Główne klasy (zgodne ze stylem `emulator-8bit-csharp`)

```csharp
public class Cdp1802
{
    // Rejestry
    public ushort[] R = new ushort[16];   // R0–RF
    public byte D;
    public bool DF;
    public byte P;                        // 0–15
    public byte X;                        // 0–15
    public byte T;
    public bool Q;
    public bool IE = true;

    // Pamięć
    public byte[] Memory = new byte[65536];

    // Licznik cykli
    public ulong TotalCycles { get; private set; }

    // Metody główne
    public void Reset();
    public void Step();                    // jedna instrukcja + cykle
    public void ExecuteCycleAccurate();    // dokładna emulacja stanów
}

// =====================================================
// SEKCJA: Obsługa DMA i Interrupt (Cycle Accurate)
// =====================================================

## 5. Obsługa DMA i Interrupt (Cycle-Accurate)

W procesorze **RCA CDP1802** obsługa DMA i przerwań odbywa się **między instrukcjami** (lub w specjalnych stanach maszyny). Jest to kluczowe w systemach satelitarnych i komputerach COSMAC (Elf, VIP, FRED itp.).

### 5.1 Stany maszyny (Machine States)

Procesor CDP1802 używa następujących stanów:

| Stan | Nazwa              | Opis                                      | Kiedy występuje |
|------|--------------------|-------------------------------------------|-----------------|
| S0   | Fetch              | Pobieranie kodu rozkazu                   | Zwykła praca |
| S1   | Execute            | Wykonywanie rozkazu                       | Zwykła praca |
| S2   | DMA                | Transfer DMA (In lub Out)                 | Na żądanie DMA |
| S3   | Interrupt          | Obsługa przerwania                        | Na żądanie INT (gdy IE=1) |

**Kolejność priorytetów (gdy żądania występują jednocześnie):**
1. **DMA-IN** (najwyższy)
2. **DMA-OUT**
3. **Interrupt** (najniższy spośród tych trzech)

### 5.2 DMA (Direct Memory Access)

**Dwa rodzaje:**
- **DMA-IN**: Zewnętrzne urządzenie zapisuje dane do pamięci (np. z taśmy, dysku, kamery)
- **DMA-OUT**: Procesor odczytuje dane z pamięci i wysyła na zewnątrz (np. do wyświetlacza)

**Kluczowe zasady:**
- Używa **R(0)** jako wskaźnika adresu
- Po każdym transferze **R(0)** jest automatycznie inkrementowany
- Transfer odbywa się **między instrukcjami** (lub w stanie S2)
- Nie wpływa na rejestry P, X, D, DF itp.

#### Proponowana implementacja w C#

```csharp
public class Cdp1802
{
    // ... istniejące pola ...

    // Linie wejściowe (symulowane z zewnątrz)
    public bool DmaInRequest { get; set; }
    public bool DmaOutRequest { get; set; }
    public bool InterruptRequest { get; set; }

    public byte DmaDataIn { get; set; }   // Dane do zapisania przy DMA-IN
    public byte DmaDataOut { get; private set; } // Dane odczytane przy DMA-OUT

    /// <summary>
    /// Sprawdza żądania DMA/Interrupt między instrukcjami.
    /// Wywoływane na końcu metody Step() lub ExecuteInstruction().
    /// </summary>
    private void CheckDmaAndInterrupts()
    {
        if (DmaInRequest)
        {
            // DMA-IN: Zapis danych z urządzenia do pamięci
            Memory[R[0]] = DmaDataIn;
            R[0]++;                    // Automatyczna inkrementacja
            DmaInRequest = false;      // Wyczyść żądanie (w realnym sprzęcie byłoby asynchroniczne)
            TotalCycles += 8;          // 1 machine cycle dla DMA
            // Stan S2
        }
        else if (DmaOutRequest)
        {
            // DMA-OUT: Odczyt danych z pamięci do urządzenia
            DmaDataOut = Memory[R[0]];
            R[0]++;
            DmaOutRequest = false;
            TotalCycles += 8;
        }
        else if (InterruptRequest && IE)
        {
            HandleInterrupt();
        }
    }

    private void HandleInterrupt()
    {
        // 1. Zapisz aktualne X i P do rejestru T
        T = (byte)((X << 4) | P);

        // 2. Ustaw P = 1 (wskaźnik na R(1) jako nowy PC)
        P = 1;

        // 3. Ustaw X = 2
        X = 2;

        // 4. Wyłącz przerwania
        IE = false;

        // 5. Wyczyść żądanie
        InterruptRequest = false;

        // 6. Dodaj cykle za obsługę przerwania (zazwyczaj 8 cykli / 1 machine cycle w stanie S3)
        TotalCycles += 8;

        // Uwaga: W rzeczywistym sprzęcie skok następuje do adresu wskazywanego przez R(1)
        // po ustawieniu P=1. Tutaj symulujemy tylko zmianę stanu.
    }
}
```

### 5.3 Interrupt – Szczegóły

**Sekwencja przy przerwaniu:**

1. Aktualna instrukcja kończy się normalnie.
2. Procesor przechodzi w stan **S3** (Interrupt Response).
3. Zapisuje `(X << 4) | P` do rejestru **T**.
4. Ustawia:
   - `P = 1`
   - `X = 2`
   - `IE = false`
5. Kontynuuje wykonanie od nowej wartości PC (R[1]).

**Powrót z przerwania** – programista musi ręcznie:
- Przywrócić `X` i `P` z rejestru `T` (instrukcja `RET` lub ręcznie)
- Ustawić `IE = true`

### 5.4 Rekomendacje implementacyjne

- Sprawdzaj żądania DMA/Interrupt **po każdej instrukcji** (w metodzie `Step()`).
- DMA ma wyższy priorytet niż Interrupt.
- Używaj `R[0]` wyłącznie do DMA (nie nadpisuj go w normalnym kodzie, chyba że świadomie).
- W wersjach cycle-accurate możesz emulować pełne stany maszyny (`CurrentState = State.S2` itp.).
- W systemach satelitarnych DMA było często używane do szybkiego transferu telemetrii lub danych z sensorów.

---

**Chcesz teraz:**

- Pełny szkielet klasy `Cdp1802` z już zaimplementowanymi DMA + Interrupt?
- Rozszerzenie dokumentacji o przykładowy prosty device (np. UART lub Timer używający DMA)?
- Przejście do implementacji głównej pętli `Step()` + dekodera instrukcji?

Daj znać, co robimy dalej.
```

### 4.2 Proponowana organizacja projektu

```
Cdp1802Emulator/
├── Core/
│   ├── Cdp1802.cs              // Główna klasa CPU
│   ├── Registers.cs            // Struktura rejestrów
│   ├── InstructionSet.cs       // Tabela rozkazów + wykonanie
│   └── Timing.cs               // Cycle counting
├── Memory/
│   └── MemoryBus.cs
├── Devices/
│   └── (opcjonalnie: UART, Timer, DMA controller)
├── Tests/
│   └── InstructionTests.cs
└── Program.cs                  // Test / headless runner
```

### 4.3 Strategia implementacji (krok po kroku)

1. **Podstawowa pętla** (`Step()`)
2. **Fetch** – odczyt kodu rozkazu + dekodowanie I + N
3. **Execute** – switch po `I` (z podswitchami po `N`)
4. **Aktualizacja cykli** po każdej instrukcji
5. **Obsługa DMA i Interrupt** (między instrukcjami)
6. **Testy** – użycie znanych testów 1802 (np. z projektów COSMAC Elf)

---

## 5. Rekomendacje implementacyjne

- Używaj `switch` na `opcode` lub tabeli delegate'ów dla szybkości
- Przechowuj `P` i `X` jako `byte` (0–15)
- Do szybkiego dostępu do rejestrów: `R[P]` jako aktualny PC
- Zaimplementuj osobną metodę `ExecuteInstruction(byte opcode)`
- Dodaj licznik cykli jako `ulong` (ważne przy symulacji satelity)
- Na początek zaimplementuj wersję **funkcjonalną**, potem dodaj pełną dokładność timingową

---

**Chcesz, żebym teraz przygotował:**

1. Pełny szkielet kodu C# (klasa `Cdp1802` + podstawowe instrukcje)?
2. Rozszerzoną tabelę wszystkich instrukcji w formacie gotowym do kopiowania?
3. Przykład obsługi DMA i przerwań?

Daj znać, od czego zaczynamy implementację.

---

## 7. Podstawowe peryferia najprostszej sondy kosmicznej

Poniżej znajduje się propozycja **minimalnego zestawu peryferiów**, które warto zaimplementować, aby móc zasymulować prostą sondę kosmiczną / satelitę z lat 70.–80. opartą na procesorze RCA CDP1802.

### 7.1 Zalecany zestaw peryferiów (Minimal Spacecraft)

| Peryferium                    | Przeznaczenie w sondzie                  | Sposób podłączenia          | Priorytet | Uwagi |
|-------------------------------|------------------------------------------|-----------------------------|---------|-------|
| **Programmable Timer**        | Planowanie zadań, heartbeat, timeouty    | Memory-mapped               | Wysoki  | Kluczowy |
| **UART / Serial Port**        | Telemetria i komendy z Ziemi             | Memory-mapped lub I/O       | Wysoki  | Podstawowa komunikacja |
| **GPIO (Digital I/O)**        | Sterowanie przekaźnikami, sensorami      | Memory-mapped               | Wysoki  | Proste wejścia/wyjścia |
| **DMA Controller**            | Szybki transfer danych (telemetria)      | Natywny mechanizm 1802      | Wysoki  | Wykorzystuje R(0) |
| **Watchdog Timer**            | Ochrona przed zawieszeniem               | Memory-mapped               | Średni  | Bezpieczeństwo |
| **Status/Control Register**   | Flagi stanu sondy, reset, tryby pracy    | Memory-mapped               | Średni  | Centralny rejestr sterujący |
| **ADC (opcjonalnie)**         | Odczyt sensorów analogowych              | Memory-mapped               | Niski   | Na późniejszym etapie |

### 7.2 Szczegółowe wyjaśnienie działania peryferiów

Poniżej znajdziesz praktyczne wyjaśnienie, **jak każde z peryferiów działa** z perspektywy procesora CDP1802 i oprogramowania pokładowego sondy.

#### 1. Programmable Interval Timer (PIT / Timer)

**Do czego służy w sondzie?**
- Zapewnia precyzyjny czas w systemie („taktowanie” sondy).
- Umożliwia wykonywanie zadań cyklicznych (np. odczyt sensorów co 100 ms, wysyłanie telemetrii co 1 sekundę).
- Służy jako podstawa prostego schedulera zadań pokładowych.

**Jak działa z punktu widzenia CPU?**
- Procesor zapisuje do rejestru wartość początkową licznika.
- Timer zlicza w dół (lub w górę) z określoną częstotliwością.
- Gdy licznik osiągnie zero → generuje **przerwanie** (IRQ).
- Oprogramowanie może przeładować timer lub zmienić jego wartość „w locie”.

**Typowe rejestry (przykład):**
- `Timer_Load` – wartość, od której zaczyna się odliczanie
- `Timer_Control` – włącz/wyłącz, tryb (jednorazowy / cykliczny), preskaler
- `Timer_Status` – flaga przerwania

**Przykład użycia w kodzie sondy:**
```c
// Co 100 ms generuj przerwanie
WriteRegister(Timer_Load, 10000);     // przy 100 kHz
WriteRegister(Timer_Control, 0x03);   // włącz + tryb cykliczny
```

---

#### 2. UART (Serial Port) – Telemetria i Telecommand

**Do czego służy?**
- Główny kanał komunikacji z Ziemią.
- Wysyłanie pakietów telemetrii (stan sondy, dane z sensorów).
- Odbieranie komend z naziemnej stacji kontroli.

**Jak działa?**
- Dwa rejestry:
  - `UART_TX` – zapisanie bajtu powoduje jego wysłanie
  - `UART_RX` – odczytanie odebranego bajtu
- Dodatkowy rejestr statusu (`UART_Status`) zawiera flagi:
  - `TX_Ready` – można wysłać kolejny bajt
  - `RX_Available` – jest odebrany bajt do przeczytania
  - Błędy (parity, overrun, framing)

**Ważne cechy w misjach kosmicznych:**
- Często używany z protokołem ramek (np. z CRC).
- Może pracować z niską prędkością (np. 1200–9600 baud) ze względu na ograniczenia mocy i odległość.

**Typowe rozmiary pakietów telemetrii (lata 70.–80.):**
- Proste pakiety statusu: **4–16 bajtów**
- Średnie pakiety z danymi sensorów: **16–64 bajty**
- Większe ramki telemetryczne (np. Landsat, niektóre sondy naukowe): **64–256 bajtów**
- Nagłówek + dane + CRC: zazwyczaj dodawano 4–8 bajtów narzutu

W tamtych czasach pakiety były **znacznie mniejsze** niż dzisiejsze (obecnie często 1–4 KB). Mały rozmiar wynikał z ograniczonej mocy nadajnika, niskiej prędkości łącza i potrzeby częstego wysyłania danych.

**Metody korekcji i wykrywania błędów (czy były używane?):**

**Tak – były używane**, choć w prostszej formie niż dziś:

| Metoda                    | Czy używana w latach 70.–80.? | Typowe zastosowanie                  | Siła ochrony          | Uwagi |
|---------------------------|-------------------------------|--------------------------------------|-----------------------|-------|
| **Prosty Checksum**       | Tak                           | Proste satelity, wczesne misje       | Słaba                 | Łatwy do implementacji |
| **Parity bit**            | Tak                           | Niektóre systemy                     | Bardzo słaba          | Często stosowany |
| **CRC-16**                | **Tak (bardzo często)**       | Większość misji z lat 70.–80.        | Dobra                 | Najpopularniejsza metoda |
| **Hamming Code**          | Tak                           | Krytyczne dane, pamięć pokładowa     | Dobra (korekcja)      | Używany też w łączach |
| **Convolutional + Viterbi** | Tak (głównie deep space)    | Voyager, Pioneer, niektóre sondy     | Bardzo dobra          | Wymagał więcej mocy obliczeniowej |
| **Reed-Solomon**          | Od późnych lat 70.            | Voyager (od 1977)                    | Bardzo dobra          | Zaawansowana jak na tamte czasy |

**W praktyce dla symulacji sondy z CDP1802 polecam:**

- **CRC-16** jako podstawową i najbardziej realistyczną metodę (łatwa do zaimplementowania i bardzo powszechna w tamtej epoce).
- Opcjonalnie prosty **checksum** dla mniej krytycznych danych.
- Dla krytycznych pakietów (np. komendy) można dodać **Hamming** lub retransmisję na żądanie.

W prostym emulatorze sondy warto zaimplementować przynajmniej **CRC-16**, bo daje bardzo dobry stosunek ochrony do złożoności.

---

### CRC-16 – Dokumentacja implementacyjna

**CRC-16** (Cyclic Redundancy Check 16-bit) był jedną z najpopularniejszych metod wykrywania błędów w transmisji danych w latach 70. i 80.

#### Dlaczego CRC-16 jest dobry do symulacji sondy?

- Bardzo skuteczny przy wykrywaniu błędów transmisji
- Łatwy do zaimplementowania nawet na słabym procesorze (takim jak CDP1802)
- Niski narzut obliczeniowy
- Powszechnie używany w misjach kosmicznych tamtego okresu

#### Polecane warianty CRC-16

| Wariant              | Polynomial (hex) | Zastosowanie                          | Rekomendacja |
|----------------------|------------------|---------------------------------------|--------------|
| **CRC-16-CCITT**     | `0x1021`         | Telekomunikacja, wiele misji NASA     | **Najlepszy wybór** |
| **CRC-16-IBM**       | `0x8005`         | Starsze systemy, niektóre satelity    | Dobry        |
| **CRC-16-ANSI**      | `0x8005`         | Różne zastosowania                    | Dobry        |

**Zalecam CRC-16-CCITT** – jest najbardziej uniwersalny i często spotykany w dokumentacji z tamtych lat.

#### Prosta implementacja w C# (dla symulacji)

```csharp
public static class Crc16
{
    private const ushort Polynomial = 0x1021; // CRC-16-CCITT
    private static readonly ushort[] Table = new ushort[256];

    static Crc16()
    {
        for (ushort i = 0; i < 256; i++)
        {
            ushort value = (ushort)(i << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((value & 0x8000) != 0)
                    value = (ushort)((value << 1) ^ Polynomial);
                else
                    value <<= 1;
            }
            Table[i] = value;
        }
    }

    public static ushort Compute(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF; // Initial value (często 0x0000 lub 0xFFFF)

        for (int i = offset; i < offset + length; i++)
        {
            byte index = (byte)((crc >> 8) ^ data[i]);
            crc = (ushort)((crc << 8) ^ Table[index]);
        }

        return crc;
    }

    public static ushort Compute(byte[] data) => Compute(data, 0, data.Length);
}
```

#### Jak używać w pakiecie telemetrii

Typowa struktura pakietu z CRC-16:

```
[ Sync (1-2 bajty) ] [ ID (1 bajt) ] [ Długość (1 bajt) ] [ Dane... ] [ CRC-16 (2 bajty) ]
```

Przykład użycia:
```csharp
byte[] packet = new byte[32];
// ... wypełnij dane ...
ushort crc = Crc16.Compute(packet, 0, packet.Length - 2);
packet[^2] = (byte)(crc >> 8);
packet[^1] = (byte)(crc & 0xFF);
```

#### Wskazówki do symulacji sondy

- CRC sprawdzaj **przy odbiorze** pakietu z Ziemi.
- Przy wysyłaniu telemetrii zawsze dodawaj CRC na końcu.
- W prostym modelu możesz założyć, że błędy występują losowo (np. 1 na 10 000 pakietów) i testować reakcję systemu na błędne CRC.
- CRC-16-CCITT jest wystarczająco dobry nawet dla prostych misji.

Ta implementacja jest lekka i może być łatwo przeniesiona na emulowany procesor CDP1802.

---

#### 3. GPIO (General Purpose Input/Output)

**Do czego służy?**
- Sterowanie elementami wykonawczymi sondy (przekaźniki, grzałki, silniki, zawory).
- Odczyt stanu sensorów cyfrowych (krańcówki, detektory obecności, przełączniki).

**Jak działa?**
- Rejestr `GPIO_Data` – stan pinów (zapis = ustaw wyjście, odczyt = stan wejścia)
- Rejestr `GPIO_Direction` – każdy bit określa czy pin jest wejściem czy wyjściem

**Przykład zastosowania:**
- Bit 0 → grzałka baterii (1 = włącz)
- Bit 3 → odczyt czujnika otwarcia paneli słonecznych

---

#### 4. DMA Controller (wykorzystanie natywnego DMA CDP1802)

**Do czego służy?**
- Szybki transfer dużych bloków danych bez obciążania procesora.
- Najczęstsze zastosowanie: zrzucanie telemetrii do bufora nadawczego UART-a lub do pamięci.

**Jak działa w CDP1802?**
- Procesor ma wbudowane wsparcie dla DMA.
- Używa rejestru **R(0)** jako adresu źródłowego/docelowego.
- Gdy urządzenie zgłasza żądanie DMA (DMA-IN lub DMA-OUT), procesor automatycznie:
  1. Zatrzymuje normalne wykonywanie programu
  2. Przenosi dane między pamięcią a urządzeniem
  3. Inkrementuje R(0)
  4. Wznawia pracę

**Zaleta:** Procesor może w tym czasie robić inne rzeczy (lub po prostu czekać).

---

#### 5. Watchdog Timer

**Do czego służy?**
- Ochrona przed zawieszeniem oprogramowania (tzw. „software hang”).
- W kosmosie bardzo ważne – promieniowanie może spowodować błędy w programie.

**Jak działa?**
- Licznik zlicza w dół.
- Oprogramowanie musi regularnie „nakarmić” watchdoga (zapisać specjalną wartość do rejestru).
- Jeśli licznik dojdzie do zera → następuje **reset procesora** lub przejście w tryb awaryjny (safe mode).

**Typowy schemat:**
```c
// W pętli głównej lub w przerwaniu timera
FeedWatchdog();   // co 500 ms
```

---

#### 6. Status & Control Register (Centralny rejestr sterujący)

**Do czego służy?**
- Zbiera najważniejsze informacje o stanie sondy w jednym miejscu.
- Umożliwia oprogramowaniu szybkie sprawdzenie błędów i podjęcie decyzji.

**Typowe bity:**
- `System_Error` – ogólny błąd
- `Power_Low` – niski poziom zasilania
- `Safe_Mode` – sonda w trybie awaryjnym
- `Reset_Cause` – przyczyna ostatniego resetu
- `Telemetry_Ready` – gotowość do wysłania telemetrii

Oprogramowanie często czyta ten rejestr na początku pętli głównej lub w przerwaniu.

**4. DMA Controller (wykorzystanie natywnego DMA 1802)**
- Procesor CDP1802 ma bardzo dobry wbudowany mechanizm DMA
- Idealny do szybkiego zrzucania bloków telemetrii do bufora nadawczego
- Używa rejestru **R(0)** jako wskaźnika

**5. Watchdog Timer**
- Musi być regularnie "karmiony" przez oprogramowanie
- Po przekroczeniu czasu → reset procesora lub tryb awaryjny
- Bardzo ważny w misjach kosmicznych

**6. Centralny rejestr Status/Control**
- Jeden lub kilka rejestrów memory-mapped
- Zawierają m.in.:
  - Flagi błędów
  - Stan zasilania
  - Tryb pracy (nominalny / awaryjny)
  - Komendy resetu podsystemów

### 7.3 Proponowana mapa pamięci (przykład)

```
0x0000 – 0x7FFF   : Pamięć programu (ROM)
0x8000 – 0xBFFF   : Pamięć danych (RAM)
0xC000 – 0xC0FF   : Rejestry peryferiów (memory-mapped I/O)
    0xC000        : Timer Control
    0xC001        : Timer Value
    0xC010        : UART Data
    0xC011        : UART Status
    0xC020        : GPIO Data
    0xC021        : GPIO Direction
    0xC030        : Watchdog
    0xC040        : Status/Control Register
```

### 7.4 Rekomendacje implementacyjne

- Zaczynaj od **Timer + UART + GPIO** — to wystarczy do bardzo prostej symulacji sondy.
- DMA warto zaimplementować wcześnie, bo jest naturalną cechą CDP1802.
- Wszystkie peryferia powinny być memory-mapped (łatwiej debugować i rozszerzać).
- Warto przygotować interfejs `IPeripheral`, żeby później łatwo dodawać nowe urządzenia.

---

**Następny krok w dokumentacji?**

Chcesz, żebym rozwinął któryś z peryferiów bardziej szczegółowo (np. dokładny opis rejestrów Timera lub UART-a)? Czy może dodać sekcję o tym, jak te peryferia mogłyby wyglądać w symulacji sondy kosmicznej?