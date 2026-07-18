# HYDRON Master Documentation
> **Purpose:** Living reference for both AI assistant and developer. Use this alongside `Project State.md` to track business logic decisions and implementation state.
> **Last generated:** July 18, 2026

---

## Table of Contents
1. [Core Concept & Philosophy](#1-core-concept--philosophy)
2. [Denomination System](#2-denomination-system)
3. [Physics Oracle & Pricing](#3-physics-oracle--pricing)
4. [Cryptography & Key System](#4-cryptography--key-system)
5. [Account Model](#5-account-model)
6. [Transaction Model](#6-transaction-model)
7. [Validator Model](#7-validator-model)
8. [Block System](#8-block-system)
9. [Reward & Fee System](#9-reward--fee-system)
10. [Consensus & Validation](#10-consensus--validation)
11. [P2P Network & RPC API](#11-p2p-network--rpc-api)
12. [Project Structure](#12-project-structure)
13. [✅ DONE — Implemented Code](#13--done--implemented-code)
14. [🔲 PLANNED — Not Yet Implemented](#14--planned--not-yet-implemented)
15. [Open Design Questions](#15-open-design-questions)

---

## 1. Core Concept & Philosophy

HYDRON is a **decentralized, permissionless, cryptographically secured financial processing network** with a **physics-pegged monetary system**. Unlike all other cryptocurrencies, HYDRON's base unit price is not market-driven — it is anchored to a physical constant.

### The Atomos
The smallest unit of HYDRON is the **atomos** (plural: atomos). It represents the intrinsic energy value of **1 hydrogen atom (H-1)**.

**Pricing formula:**
```
1 atomos (USD) = 13.6 eV × consensus_electricity_price_per_eV
```

- `13.6 eV` is the ionization energy of hydrogen — a universal physical constant. It **never changes**.
- `consensus_electricity_price_per_eV` is a **population-weighted global average electricity price** voted on by validators using EIA, IEA, Eurostat, and World Bank data.
- Atomos is always a **whole integer** — it is indivisible.

**Why hydrogen?** H-1 (protium) has a natural abundance of 99.99%, mass of 1.007825 u, and is the most stable reference point in existence, making it ideal as an economic anchor.

---

## 2. Denomination System

Each denomination equals the **square of the previous denomination's atomos count**, starting from 1 HYA = 100 atomos.

| Code | Greek Name | Atomos Count | Conversion |
|------|-----------|--------------|-----------|
| HYA | Alpha | 10² = 100 | Base unit |
| HYB | Beta | 10⁴ = 10,000 | HYA² |
| HYG | Gamma | 10⁸ = 100,000,000 | HYB² |
| HYD | Delta | 10¹⁶ | HYG² |
| HYE | Epsilon | 10³² | HYD² |
| HYZ | Zeta | 10⁶⁴ | HYE² |

**Amounts are expressed as decimals in any denomination.** Examples:
- 34,562 atomos = 345.62 HYA = 3.4562 HYB = 0.00034562 HYG

**Implementation:** Internally, all amounts are stored and calculated in raw **atomos** using `BigInteger` to support the massive range of HYZ (10⁶⁴ atomos).

---

## 3. Physics Oracle & Pricing

### Formula Chain
```
USD/kWh (global avg)
  → USD/J      (÷ 3,600,000)
  → USD/eV     (÷ 1.602176634 × 10⁻¹⁹)
  → atomos price (× 13.6)
```

### Data Sources
- **EIA** (U.S. Energy Information Administration)
- **IEA** (International Energy Agency)
- **Eurostat** (EU statistics)
- **World Bank** (population data for weighting)

### Consensus Mechanism
- Validators scrape/submit electricity price data each StateBlock cycle.
- A **66% supermajority** vote is required to finalize the price.
- The accepted price is embedded in each StateBlock.

---

## 4. Cryptography & Key System

### Algorithms
| Purpose | Algorithm |
|---------|-----------|
| Signing / Identity | EdDSA Ed25519 |
| Stealth Payments | X25519 (ECDH) |
| Hashing | SHA-256 |
| HD Key Derivation | HMAC-SHA512 |
| Stealth Secret Derivation | HKDF-SHA256 |

### Address Derivation
```
Address = SHA256(Ed25519 Public Key) → hex string (lowercase)
```

### KeySafe
`KeySafe` is the secure key management class. It holds:
- **Ed25519 private key** — used for transaction signing and identity.
- **X25519 key pair** — used for stealth payments (receiver privacy).
- **HD master seed** — 32 random bytes used to derive child keys.
- **HD chain code** — derived via `HMAC-SHA512("HYDRON", masterSeed)[32..]`.

### HD Wallet (BIP32-style)
Child keys are derived via:
```
HMAC-SHA512(chainCode, [0x01 || parentPrivKey || index]) → [0..32] = childKey, [32..] = childChainCode
```
Child accounts (stealth sub-accounts) **cannot** derive further children.

### Stealth Payments (Privacy)
For `HiddenReceiver` or `FullyPrivate` transactions:
1. Sender generates ephemeral X25519 key pair.
2. Sender computes: `sharedSecret = X25519(ephemeralPrivKey, recipientStealthPubKey)`.
3. `stealthAddress = SHA256(HKDF-SHA256(sharedSecret, "HYDRON stealth"))`.
4. Sender broadcasts `ephemeralPublicKey` in the transaction.
5. Receiver scans: computes same `stealthAddress` using their X25519 private key.

### Privacy Modes (enum `PrivacyMode`)
| Value | Description |
|-------|-------------|
| `Public` | Sender, receiver, and amount all visible |
| `HiddenReceiver` | Receiver is a stealth address; ephemeral key required |
| `FullyPrivate` | Both receiver and metadata hidden; ephemeral key required |

---

## 5. Account Model

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Address` | `string` | SHA256(PublicKey) hex, immutable |
| `PublicKey` | `string` | Ed25519 public key, base64, immutable |
| `StealthPublicKey` | `string` | X25519 public key for stealth payments, rotatable |
| `Handle` | `string?` | Optional display name, max 1000 UTF-8 bytes |
| `Balance` | `Atomos` | Thread-safe, always ≥ 0 |
| `Nonce` | `BigInteger` | Monotonically increasing, for replay protection |
| `StateHash` | `string` | SHA256 of all account fields, lazily computed & cached |

### Thread Safety
- `Balance` and `Nonce` mutations use `Lock` primitives.
- `StateHash` is lazily computed and invalidated on every state change.

### `Validator` extends `Account`
Validators are accounts with extra fields:
- `StakedAmount` — must be ≥ 1 atomos to be active.
- `Tier` — `Core` or `Edge`.
- `Status` — `Active | Inactive | Unreachable | Penalized | Warned | Suspended`.
- `CorrectVotes` / `TotalVotes` → `ReputationScore` (0–100%).
- Commission rate (0–100%), network endpoints (IPv4/IPv6/DNS), description.

### Validator Tiers
| Tier | Description |
|------|-------------|
| `Core` | High-reputation, high-stake validators; higher block rewards |
| `Edge` | Standard validators; lower block rewards |

### Validator Status Transitions
```
Active → Warned → Suspended
Active → Unreachable → Active (on reconnect)
Active/Inactive → Penalized (stake slashed to 0)
Penalized → Active (only after re-staking ≥ 1 atomos)
```

### ValidatorRank
A computed, immutable `record` used for ranking validators for assignment to transactions:
- `StakedAmount`, `AvgValidationSpeedMs`, `ValidationActivityCount`, `ReputationNormalized`, `BlocksObserved`, `FinalRank`, `Tier`.

---

## 6. Transaction Model

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Sender` | `string` | Address |
| `Receiver` | `string` | Address (or stealth address) |
| `Amount` | `Atomos` | Must be > 0 |
| `Fee` | `Atomos` | Minimum 1 HYD; goes to first validator |
| `Nonce` | `BigInteger` | Must match sender's account nonce |
| `SenderSignature` | `string` | Ed25519 sig over canonical TX data |
| `ReceiverSignature` | `string?` | Optional; required if `RequiresReceiverConfirmation` |
| `Hash` | `string` | Set after construction; immutable once set |
| `Status` | `TransactionStatus` | State machine |
| `PrivacyMode` | `PrivacyMode` | Public / HiddenReceiver / FullyPrivate |
| `EphemeralPublicKey` | `string?` | Required for non-public transactions |
| `Priority` | `Priority?` | Low / Medium / High / Urgent |

### Transaction Status State Machine
```
InitiatedBySender
  ├→ AwaitingReceiverAcceptance (if receiver confirmation required)
  │    ├→ PendingValidation
  │    ├→ AbortedBySender [terminal]
  │    ├→ AbortedByReceiver [terminal]
  │    └→ TimedOut [terminal]
  ├→ PendingValidation (direct, no receiver confirmation needed)
  │    ├→ ConsensusReached
  │    │    └→ Settled [terminal]
  │    └→ Rejected [terminal]
  └→ AbortedBySender [terminal]
```

### Validator Assignment
- Validators are assigned **before** `PendingValidation` status.
- Once status transitions to `PendingValidation`, validator list is **frozen** (`_frozenValidatorCount`).
- Unassigned validators who submit votes are tracked separately (`_unassignedValidators`).
- **Supermajority threshold** = `ceil(frozenCount × 2/3)`.

### Finalization
- Once in any terminal status, `FinalizeTransaction()` is called — marks `IsFinalized = true`, sets `FinalizedAt`.
- A finalized transaction cannot be modified in any way.
- Only finalized transactions can be added to a `TransactionBlock`.

---

## 7. Validator Model

### Validation Record (`Validation` class)
Each validator's vote on a transaction is represented as a `Validation`:
| Field | Description |
|-------|-------------|
| `Id` | UUID v7 (time-ordered) |
| `TransactionHash` | Which TX is being validated |
| `ValidatorAddress` | Who is voting |
| `Status` | `Pending → Confirmed | Rejected` |
| `ValidationSignature` | Ed25519 sig; required before confirm/reject |
| `ValidationSpeedMs` | Latency from TX broadcast to vote submission |
| `FeeReward` | Assigned after confirmation |
| `PenaltyAmount` | Non-null if validator was penalized for this vote |

### Penalty Rules
| Scenario | Penalty |
|----------|---------| 
| Approved an invalid TX | −100% of TX amount (slashed from stake) |
| Rejected a valid TX | −1% of TX amount (slashed from stake) |
| Wrong vote (reputation) | −50 reputation points |
| Correct vote (reputation) | +1 reputation point |

---

## 8. Block System

### TransactionBlock (ValidatorBlock)
Each `TransactionBlock` is proposed and sealed by **one validator**:
| Field | Description |
|-------|-------------|
| `BlockNumber` | Sequential, BigInteger |
| `Hash` | SHA256 of block contents |
| `PreviousHash` | Links to previous block |
| `MerkleRoot` | Merkle tree root of all TX hashes |
| `StateRoot` | Root hash of all account states at this block |
| `ValidatorAddress` | The block proposer |
| `Transactions` | List of finalized transactions |

**Block lifecycle:**
1. Transactions are added (only finalized, hashed transactions).
2. `SetMerkleRoot()` and `SetStateRoot()` are called separately.
3. `Seal(hash, merkleRoot, stateRoot)` locks the block permanently.
4. A sealed block passes `IsValid()` when: sealed, has transactions, all txs finalized, all hashes non-empty.

### StateBlock (Planned)
- Contains 100 `TransactionBlock`s.
- Triggers a full **state settlement** — electricity price consensus, rewards issuance.
- After 100 StateBlocks past finality, block is **permanently immutable**.

### Finality Rules (Planned)
- **Deterministic finality** at 66% validator votes.
- **100-block immutability window** — no reorganization possible beyond this.

---

## 9. Reward & Fee System

### Reward Distribution
| Trigger | Amount | Recipient |
|---------|--------|-----------|
| Per transaction validated | 1 HYA (100 atomos) | Validating validator |
| Per ValidatorBlock | 1 HYB (10,000 atomos) | Block proposer |
| Per StateBlock | 1 HYG (100,000,000 atomos) | State settling validator(s) |

### Fee Structure
- **Minimum fee:** 1 HYD per transaction.
- **Fee recipient:** First validator (the one who received the TX first and initiated consensus).
- Fee market is dynamic — senders can pay more to increase `Priority`.

### BlockReward Model
`BlockReward` captures per-block reward metadata:
- `TotalCoreBlockReward`, `TotalEdgeBlockReward` (split by tier).
- `TotalValidationReward` (per-TX rewards summed).
- `TotalFeeReward` (fees collected).
- `TotalMinted = TotalCoreBlockReward + TotalEdgeBlockReward + TotalValidationReward` (fees are not minted — they are transferred).
- Each validator's individual reward is captured in `ValidatorReward` (blockReward + validationReward + feeReward).

### Reward Eligibility (Planned)
- Penalized validators cannot receive rewards.
- Suspended validators have zero voting weight (`GetVotingWeight() = Atomos.Zero`).
- Reward multipliers based on reputation score are planned but not yet implemented.

---

## 10. Consensus & Validation

### Overview
HYDRON uses a **delegated supermajority BFT-style consensus**:
1. A transaction is submitted and broadcasted.
2. Validators are assigned to the transaction.
3. First validator has **veto power** (first-validator gating).
4. All assigned validators vote: **Confirm** or **Reject**.
5. If `≥ ceil(N × 2/3)` confirm → `ConsensusReached` → `Settled`.
6. If `< ceil(N × 2/3)` confirm → `Rejected`.

### Validator Assignment (Planned)
- Based on `ValidatorRank.FinalRank` composite score.
- Factors: staked amount, avg validation speed, activity count, normalized reputation, blocks observed.

### First-Validator Veto (Planned)
- The first validator in `AssignedValidators` has special veto authority.
- If first validator rejects, the TX may be immediately killed regardless of other votes (exact logic TBD).

---

## 11. P2P Network & RPC API

### Network Stack (All Planned)
- **TCP sockets** for peer-to-peer communication.
- **DHT** (Distributed Hash Table) for peer discovery.
- **Gossip protocol** for message propagation.
- Message types: TX broadcast, consensus votes, block broadcast.

### RPC API (All Planned)
| Method | Description |
|--------|-------------|
| `suggest_validator()` | Returns best online validator for TX submission |
| `wallet_create()` | HD wallet generation |
| `transfer()` | Submit a transaction |
| `get_balance()` | Query account balance |
| `get_transaction()` | Query TX by hash |
| `become_validator()` | Register as validator |
| `get_electricity_price()` | Current oracle price |
| `get_network_stats()` | TPS, validator count |

### Project Modules (Solution Structure)
| Module | Purpose |
|--------|---------|
| `HYDRON.Models` | All data models (implemented) |
| `HYDRON.Core` | Main entry point, bootstrapping (stub only) |
| `HYDRON.Cryptography` | Crypto utilities (empty) |
| `HYDRON.Database` | RocksDB persistence (empty) |
| `HYDRON.Network` | P2P networking (empty) |
| `HYDRON.Connectivity` | External connectivity / RPC (empty) |
| `HYDRON.Validator` | Validator node logic (empty) |

---

## 12. Project Structure

```
HYDRON/
├── src/
│   ├── HYDRON.Models/          ← ✅ Implemented
│   │   ├── Atomos.cs
│   │   ├── Account.cs
│   │   ├── Validator.cs
│   │   ├── Transaction.cs
│   │   ├── TransactionBlock.cs
│   │   ├── Validation.cs
│   │   ├── Rewards.cs
│   │   ├── KeySafe.cs
│   │   ├── ValidatorRank.cs
│   │   └── Enumerators.cs
│   ├── HYDRON.Core/            ← 🔲 Stub (Program.cs only)
│   ├── HYDRON.Cryptography/    ← 🔲 Empty
│   ├── HYDRON.Database/        ← 🔲 Empty
│   ├── HYDRON.Network/         ← 🔲 Empty
│   ├── HYDRON.Connectivity/    ← 🔲 Empty
│   └── HYDRON.Validator/       ← 🔲 Empty
├── tests/                      ← 🔲 Empty
├── docs/                       ← This file lives here
└── HYDRON.slnx
```

---

## 13. ✅ DONE — Implemented Code

> All completed code lives in `src/HYDRON.Models/`.

### Atomos (`Atomos.cs`)
- ✅ `readonly struct` wrapping `BigInteger` — immutable value type.
- ✅ All 6 denomination conversion factors as static `BigInteger` constants.
- ✅ Full arithmetic: `+`, `-`, `*`, `/`, `%`, `++`, `--` with negative-guard throws.
- ✅ `Scale(numerator, denominator)` for proportional calculations.
- ✅ `FromDenomination(double, Denominations)` and `ToDenomination(Denominations)`.
- ✅ `RemainderAfterDenomination(Denominations)` for mixed-denomination display.
- ✅ Implements `IComparable<Atomos>`, `IEquatable<Atomos>`, `IFormattable`.
- ✅ `Atomos.Zero` and `Atomos.One` static sentinels.

### Account (`Account.cs`)
- ✅ Immutable identity (`Address`, `PublicKey`).
- ✅ Thread-safe `Balance` with `Lock`.
- ✅ `TryDeductBalance()` (returns false if insufficient) and `AddBalance()`.
- ✅ `IncrementNonce()`.
- ✅ `UpdateHandle()` with UTF-8 byte length validation (max 1000).
- ✅ `ApplyStealthKeyRotation()` (internal).
- ✅ `StateHash` — lazily computed SHA256 fingerprint of all fields; auto-invalidated on any mutation.
- ✅ `AppendExtraHashFields()` virtual hook for subclass hash extension (used by Validator).

### Transaction (`Transaction.cs`)
- ✅ Full constructor with all validation guards.
- ✅ Status state machine with `ValidTransitions` dictionary — invalid transitions throw.
- ✅ `AssignValidators()` / `AddValidator()` / `RemoveValidator()` — locked before `PendingValidation`.
- ✅ `FreezeValidatorCount` on `PendingValidation` transition.
- ✅ `RequiredSupermajorityValidationsCount` = `ceil(frozenCount × 2/3)`.
- ✅ `AddValidation()` — routes registered vs unregistered validators.
- ✅ `SetReceiverSignature()` / `SetHash()` with idempotency guards.
- ✅ `FinalizeTransaction()` — only from terminal statuses.
- ✅ `AssignBlockNumber()` — only after `Settled`.
- ✅ `ChangePriority()` — only before `PendingValidation`.
- ✅ `GetTotalCost()` = Amount + Fee.
- ✅ Privacy mode + ephemeral public key fields.

### Validator (`Validator.cs`)
- ✅ Extends `Account`.
- ✅ Constructor validates: stake ≥ 1 atomos, at least 1 network endpoint, valid IPv4/IPv6/DNS formats.
- ✅ `AddStake()` / `WithdrawStake()` with status auto-transitions.
- ✅ `ApplyPenalty()` — clamped to staked amount, sets `Penalized` when stake → 0.
- ✅ `ReceiveReward()` — adds to stake AND `TotalRewardsEarned`; blocked when penalized.
- ✅ `RecordVote()` / `RecordValidation()` / `RecordRejection()`.
- ✅ `Warn()` / `Suspend()` / `MarkUnreachable()` / `RestoreReachable()` transitions.
- ✅ `GetVotingWeight()` — returns `Atomos.Zero` for penalized/suspended.
- ✅ `ReputationScore` = `min(CorrectVotes / TotalVotes × 100, 100)`.
- ✅ `AppendExtraHashFields()` override — includes stake, tier, status in hash.
- ✅ Duplicate validation guard using `_confirmedValidationIds` and `_rejectedValidationIds` HashSets.

### Validation (`Validation.cs`)
- ✅ UUID v7 ID (time-ordered).
- ✅ `SignValidation()` — must be called before confirm/reject.
- ✅ `Confirm()` / `Reject()` with speed tracking.
- ✅ `AssignReward()` — only for confirmed validations, once.
- ✅ `Penalize()` — with evidence string and timestamp.

### KeySafe (`KeySafe.cs`)
- ✅ Ed25519 key generation via `NSec.Cryptography`.
- ✅ X25519 key generation for stealth.
- ✅ `Sign()` and static `Verify()`.
- ✅ HD child key derivation (`DeriveChild(uint index)`).
- ✅ Stealth payment: `ComputeStealthPayment()` and `IsStealthPaymentMine()`.
- ✅ `DeriveStealthSpendKeySafe()` for receiving stealth funds.
- ✅ `RotateStealthKeyPair()`.
- ✅ `ImportFromKeys()` static factory for wallet restore.
- ✅ Secure disposal: `CryptographicOperations.ZeroMemory()` on private key bytes.

### TransactionBlock (`TransactionBlock.cs`)
- ✅ Thread-safe `AddTransaction()` with sealed-block guard.
- ✅ `SetMerkleRoot()` / `SetStateRoot()` — immutable once set.
- ✅ `Seal(hash, merkleRoot, stateRoot)` — locks block permanently.
- ✅ `IsValid()` check.
- ✅ `GetTotalFees()` aggregation.

### Rewards (`Rewards.cs`)
- ✅ `BlockReward` class — captures per-block reward snapshot with all totals.
- ✅ `ValidatorReward` class — per-validator breakdown (block reward + validation reward + fee reward).
- ✅ `TotalMinted` computed as sum of block + validation rewards (fees excluded as they are not new issuance).
- ✅ `Settle()` method — marks reward as distributed.

### ValidatorRank (`ValidatorRank.cs`)
- ✅ Immutable `record` with all ranking fields.
- ✅ Captures composite `FinalRank`, `Tier`, computed timestamp.

### Enumerators (`Enumerators.cs`)
- ✅ `Denominations` (Hya–Hyz).
- ✅ `Priority` (Low / Medium / High / Urgent).
- ✅ `TransactionStatus` (8 states).
- ✅ `ValidationStatus` (Pending / Confirmed / Rejected).
- ✅ `ValidatorStatus` (Active / Inactive / Unreachable / Penalized / Warned / Suspended).
- ✅ `ValidatorTier` (Core / Edge).
- ✅ `RewardStatus` (Pending / Settled).
- ✅ `PrivacyMode` (Public / HiddenReceiver / FullyPrivate).

---

## 14. 🔲 PLANNED — Not Yet Implemented

> Based on `Project State.md` as of January 11, 2026 and code review as of July 18, 2026. Grouped by system area.

### 1. Core Infrastructure
- [ ] **Database Layer** — `IDataStore` interface, RocksDB wrapper, all repository interfaces (`IAccountRepository`, `ITransactionRepository`, `IValidatorRepository`, `IBlockRepository`), batch operations, JSON serialization codec.
- [ ] **Cryptography module** (`HYDRON.Cryptography`) — `KeyPair` wrapper, `SignatureVerifier`, `HashProvider`, crypto constants.
- [ ] **Configuration & Bootstrapping** — `appsettings.json`, config class, DI container (`ServiceRegistry`), `HydronEngine` main bootstrap.
- [ ] **Error Handling & Logging** — custom exception hierarchy, structured logging.
- [ ] **Unit Tests** — for all 7+ models, database layer, cryptography.

### 2. Account & Transaction Processing Logic
- [ ] Account repository (load/save to RocksDB).
- [ ] Balance management service (add/deduct with DB writes).
- [ ] Nonce management service.
- [ ] Reputation management service.
- [ ] Transaction creation flow (full pipeline).
- [ ] Signature verification service (using `KeySafe.Verify()`).
- [ ] Balance, nonce, and fee validation rules.
- [ ] Double-spend prevention.
- [ ] Transaction status lifecycle management service.
- [ ] Transaction queries (by sender, by hash, by status).

### 3. Validator System Logic
- [ ] Validator registration flow.
- [ ] Validator online/offline tracking.
- [ ] Validator assignment algorithm (using `ValidatorRank`).
- [ ] Transaction validator interface.
- [ ] Consensus voting system (66%+ aggregation).
- [ ] First-validator veto gating logic.
- [ ] Vote aggregation & finality trigger.
- [ ] Reputation tier logic and status transitions service.
- [ ] Financial penalty application service.

### 4. Block System
- [ ] `ValidatorBlock` creation service (collects 100 TXs → seals block).
- [ ] Merkle tree calculation.
- [ ] `StateBlock` model and creation service (100 ValidatorBlocks).
- [ ] State root calculation (aggregated account state hashes).
- [ ] Electricity price consensus at StateBlock.
- [ ] Block finality tracking and 100-block immutability enforcement.
- [ ] `Mempool` model and queue management.

### 5. Reward & Fee System
- [ ] Per-TX reward distribution logic.
- [ ] Per-ValidatorBlock reward distribution.
- [ ] Per-StateBlock reward distribution.
- [ ] Reward eligibility checks (tier, status).
- [ ] Reward multiplier calculation.
- [ ] Minimum fee enforcement (1 HYD) in TX submission.
- [ ] Fee market dynamics.

### 6. Physics Oracle
- [ ] EIA API integration.
- [ ] IEA data source.
- [ ] Eurostat data source.
- [ ] World Bank population API.
- [ ] Population-weighted USD/kWh calculation.
- [ ] Full formula chain: USD/kWh → USD/J → USD/eV → atomos price.
- [ ] Validator consensus voting for oracle price.
- [ ] Price update frequency policy.

### 7. P2P Network (`HYDRON.Network`)
- [ ] TCP socket management.
- [ ] Peer connection and discovery (DHT).
- [ ] Connection pool management.
- [ ] Message serialization format.
- [ ] Transaction broadcast.
- [ ] Consensus vote broadcast.
- [ ] Block broadcast.
- [ ] Gossip protocol.
- [ ] Connection timeouts, retry logic, dead peer removal.

### 8. RPC API (`HYDRON.Connectivity`)
- [ ] All RPC methods (`suggest_validator`, `wallet_create`, `transfer`, `get_balance`, `get_transaction`, `become_validator`, etc.).
- [ ] Validator analytics methods.
- [ ] Block query methods.
- [ ] Oracle price query methods.
- [ ] Network stats methods.

### 9. Testing & Quality
- [ ] All unit tests (models, database, crypto, validator logic, block creation, reward calculation, oracle).
- [ ] Integration tests (account → TX flow, TX → consensus → block flow, block → state settlement).
- [ ] End-to-end tests (multi-validator simulation, TX lifecycle, finality guarantee).
- [ ] Performance benchmarks and memory profiling.

### 10. Deployment
- [ ] Docker image and multi-stage build.
- [ ] Docker Compose for local testing.
- [ ] Mainnet, testnet, dev configs.
- [ ] API specification, architecture guide, deployment guide.

---

## 15. Open Design Questions

> These are unresolved or ambiguous design decisions. Resolve before implementation begins for each area.

1. **First-validator veto** — exact veto mechanics not fully specified. Does a first-validator rejection immediately kill the TX, or does it just count as one vote?

2. **Reward multiplier** — reputation-based multiplier for rewards is referenced but the formula (weights, thresholds) is not defined.

3. **Mempool design** — capacity, eviction policy, and priority ordering not yet specified.

4. **StateBlock model** — currently only `TransactionBlock` exists in code. `StateBlock` model is entirely absent from the codebase.

5. **ValidatorRank `FinalRank` formula** — the model has the field but the scoring algorithm (weights for stake, speed, activity, reputation, blocks observed) is not yet defined.

6. **Fee minimum enforcement** — 1 HYD minimum is the stated rule but **not enforced** in the current `Transaction` constructor (accepts any `Fee` value including 0). Needs a guard.

7. **Commission rate** — `CommissionRate` field exists on `Validator` but its application logic (e.g., split between validator and delegators) is not specified.

8. **Tracker sync** — `Project State.md` still marks `Validator`, `Validation`, `TransactionBlock`, `Rewards`, `KeySafe`, and `ValidatorRank` as incomplete. The tracker should be updated to reflect the current codebase.

9. **`TransactionBlock` naming** — the tracker calls it `ValidatorBlock`, but the code class is `TransactionBlock`. Decide on canonical naming before building the service layer.

10. **Reward issuance timing** — rewards are modeled but not clear whether they are issued per-block as the block is settled, or batched at StateBlock time.
