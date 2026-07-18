# HYDRON Phase 1 — Task State & Progress

**Last Updated:** July 18, 2026  
**Format:** Area-based, reflects actual repository state as of last commit (`224e17f`)

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Complete and audited — all known issues addressed |
| 🔶 | Structurally present but carries known defects |
| 🔲 | Not started |
| 🏗️ | Project stub exists (`.csproj` + placeholder), no real logic yet |

---

## 1. Core Infrastructure

### 1.1 Data Models (`HYDRON.Models`)

- ✅ **1.1.1** `Atomos` — physics-pegged currency value type; 6 denominations (HYA→HYZ); full arithmetic operator set; `IComparable<Atomos>`, `IEquatable<Atomos>`; denomination conversion helpers; `BigInteger`-backed to eliminate precision loss at large denominations (Hyd+)
- ✅ **1.1.2** `Account` — user account state; balance management with `Lock` for thread-safe mutations; nonce (thread-safe via `_balanceLock`); handle; stealth public key; SHA-256 state hash with lock-protected invalidation cache
- ✅ **1.1.3** `Transaction` — transfer primitive; privacy modes (`Public`, `HiddenReceiver`, `FullyPrivate`); sender/receiver signature tracking; validator assignment & supermajority threshold (minimum 1); frozen validator count guard; status lifecycle with valid-transition map; fee; priority; block number assignment; finalization; `AddValidator` guard changed from enum-order comparison to `_frozenValidatorCount.HasValue`
- ✅ **1.1.4** `Validator` — validator account; staking/withdrawal; reputation score; correct/total vote counters; penalty application; tier (`Core`/`Edge`); status (`Active`/`Warned`/`Suspended`/`Penalized`/`Inactive`/`Unreachable`); network endpoint validation (IPv4/IPv6 address-family verified); separate `_confirmedValidationIds` / `_rejectedValidationIds` sets to prevent cross-track collisions; `ReceiveReward` blocks penalized validators and only restores `Inactive`; `GetVotingWeight` returns zero for `Penalized` and `Suspended`
- ✅ **1.1.5** `Validation` — per-validator vote record; sign-before-confirm/reject enforced; `Penalize` works on both `Confirmed` and `Rejected` outcomes; reward assignment; speed tracking
- ✅ **1.1.6** `TransactionBlock` — 100-TX block structure; `Lock`-protected `Seal` and `AddTransaction`; individual `SetHash`/`SetMerkleRoot`/`SetStateRoot` setters (note: redundant with `Seal` — see open items below); previous-hash chaining; `IsValid()` check
- ✅ **1.1.7** `Rewards` — `BlockReward` + `ValidatorReward` records; `TotalMinted` excludes fee rewards (fees are paid, not minted); `ValidatorReward.TotalReward` = blockReward + validationReward + feeReward; settlement status
- ✅ **1.1.8** `KeySafe` — HD wallet (BIP-32-style Ed25519 + X25519); HMAC-SHA-512 child key derivation — both key and chain code stored; stealth spend key derived via HMAC-SHA-256 with label `"HYDRON stealth spend"` (no XOR with master key); stealth payment (`ComputeStealthPayment` / `IsStealthPaymentMine`); key rotation; `IDisposable` with `CryptographicOperations.ZeroMemory`; all export methods guard disposed state
- ✅ **1.1.9** `ValidatorRank` — ranking snapshot record; normalized reputation, uptime, speed, stake fields; tier classification
- ✅ **1.1.10** `Enumerators` — all domain enums: `TransactionStatus`, `ValidationStatus`, `ValidatorStatus`, `ValidatorTier`, `Priority`, `PrivacyMode`, `RewardStatus`

### Open items in completed models (low severity — address before Phase 2 service layer)

- 🔶 `TransactionBlock` — `SetHash`, `SetMerkleRoot`, `SetStateRoot` individual setters are redundant alongside `Seal` and create an inconsistent two-phase write path. Either remove the individual setters and mandate `Seal`, or remove `Seal` and enforce the individual setters. Having both is a footgun.
- 🔶 `Account` — `IncrementNonce` is not guarded by `_balanceLock`; concurrent calls from two threads can lose an increment. Wrap in `lock (_balanceLock)` before service layer is built.
- 🔶 `Enumerators` — enum members use implicit integer values; reordering during refactoring will silently break any serialized data. Assign explicit values before the database layer lands.

### 1.2 Database Layer (`HYDRON.Database`)

- 🏗️ Project stub exists
- 🔲 **1.2.1** `IDataStore` interface — generic key/value contract
- 🔲 **1.2.2** RocksDB wrapper — `RocksDbDataStore : IDataStore`
- 🔲 **1.2.3** Key naming & namespace scheme (account, transaction, block, validator prefixes)
- 🔲 **1.2.4** `IAccountRepository` + implementation
- 🔲 **1.2.5** `ITransactionRepository` + implementation
- 🔲 **1.2.6** `IValidatorRepository` + implementation
- 🔲 **1.2.7** `IBlockRepository` + implementation (TransactionBlock + StateBlock)
- 🔲 **1.2.8** Batch write operations (atomic multi-key commits)
- 🔲 **1.2.9** Range / iterator queries (e.g. transactions by sender prefix)
- 🔲 **1.2.10** JSON serialization codec for all model types (must handle `BigInteger`, `Atomos`, `DateTimeOffset`)

### 1.3 Cryptography (`HYDRON.Cryptography`)

- 🏗️ Project stub exists
- 🔲 **1.3.1** `HashProvider` — SHA-256 canonical hasher for transactions, blocks, and state roots
- 🔲 **1.3.2** `MerkleTree` — binary Merkle tree builder from transaction hash list; produces root accepted by `TransactionBlock.Seal()`
- 🔲 **1.3.3** `SignatureVerifier` — Ed25519 verify wrapper used by services (decoupled from `KeySafe`)
- 🔲 **1.3.4** `CryptoConstants` — system-wide crypto parameter definitions

### 1.4 Configuration & Bootstrapping (`HYDRON.Core`)

- 🏗️ Project stub exists
- 🔲 **1.4.1** `appsettings.json` template (mainnet / testnet / dev variants)
- 🔲 **1.4.2** Strongly-typed `HydronConfig` class
- 🔲 **1.4.3** DI service registry (`IServiceCollection` extensions)
- 🔲 **1.4.4** `HydronEngine` — main bootstrap; wires DB, crypto, network, validator, RPC
- 🔲 **1.4.5** `SystemConstants` — reward amounts (1 HYA/TX, 1 HYB/block, 1 HYG/state-block), block sizes (100 TX, 100 blocks), consensus threshold (2/3), minimum fee (1 HYD), immutability window (100 blocks), physics constant (13.6 eV)

### 1.5 Error Handling & Logging

- 🔲 **1.5.1** Custom exception hierarchy (`HydronException`, `ConsensusException`, `InsufficientFundsException`, `InvalidTransactionException`, `CryptographyException`)
- 🔲 **1.5.2** Structured error codes & result types (`Result<T, HydronError>` pattern to replace throw-everywhere)
- 🔲 **1.5.3** `IHydronLogger` abstraction
- 🔲 **1.5.4** Structured logging via `Microsoft.Extensions.Logging` with context enrichment (block number, validator address, TX hash)

### 1.6 Unit Tests (`HYDRON.Tests`)

- 🔲 **1.6.1** `AtomosTests` — arithmetic, denomination conversion, overflow, equality, comparison
- 🔲 **1.6.2** `AccountTests` — balance mutations under concurrency, state hash, nonce increment, handle validation
- 🔲 **1.6.3** `TransactionTests` — status lifecycle, supermajority threshold, frozen validator guard, signature requirements, finalization
- 🔲 **1.6.4** `ValidationTests` — sign-before-confirm, penalize on both Confirmed and Rejected, reward assignment
- 🔲 **1.6.5** `ValidatorTests` — staking, penalty, voting weight, endpoint validation, reachability, reward block on Penalized
- 🔲 **1.6.6** `KeySafeTests` — child derivation (key + chain code), stealth payment round-trip, HMAC spend key derivation, rotation, disposal safety
- 🔲 **1.6.7** `TransactionBlockTests` — block validity, capacity, hash chaining, lock behaviour

---

## 2. Account & Transaction Processing (`HYDRON.Core` services)

### 2.1 Account Management

- 🔲 **2.1.1** `AccountService` — load/save via repository; create new account
- 🔲 **2.1.2** Balance check queries (thread-safe read)
- 🔲 **2.1.3** Nonce reservation & verification (prevent double-spend at service layer)
- 🔲 **2.1.4** Reward/penalty application from settled block

### 2.2 Transaction Processing

- 🔲 **2.2.1** `TransactionBuilder` — constructs and signs a `Transaction` from a `KeySafe`
- 🔲 **2.2.2** Sender signature verification on ingest
- 🔲 **2.2.3** Balance sufficiency check (amount + fee ≤ balance)
- 🔲 **2.2.4** Nonce ordering check (sender nonce must equal account nonce + 1)
- 🔲 **2.2.5** Fee validation — minimum 1 HYD enforced at service layer
- 🔲 **2.2.6** Double-spend prevention via nonce reservation in mempool
- 🔲 **2.2.7** Transaction status lifecycle orchestration (InitiatedBySender → PendingValidation → ConsensusReached → Settled)
- 🔲 **2.2.8** Transaction queries (by hash, by sender, by status, by block number)

---

## 3. Validator System (`HYDRON.Validator`)

- 🏗️ Project stub exists

### 3.1 Validator Core

- 🔲 **3.1.1** `ValidatorService` — registration, load/save, status transitions
- 🔲 **3.1.2** Validator registration flow (stake deposit → `Active`)
- 🔲 **3.1.3** Online/offline heartbeat tracking
- 🔲 **3.1.4** Validation capacity limit (stake-based transaction assignment ceiling)
- 🔲 **3.1.5** Validator repository integration

### 3.2 Validation & Consensus

- 🔲 **3.2.1** `ITransactionValidator` interface — validation pipeline
- 🔲 **3.2.2** Signature verification step (using `HYDRON.Cryptography.SignatureVerifier`)
- 🔲 **3.2.3** Balance sufficiency re-check at validation time
- 🔲 **3.2.4** Nonce ordering re-check at validation time
- 🔲 **3.2.5** Consensus vote aggregation — monitor `RequiredSupermajorityValidationsCount`; trigger finalization when reached
- 🔲 **3.2.6** First-validator veto gate — first validator's vote must be `Approved` before supermajority is counted
- 🔲 **3.2.7** Auto-finalization when 66%+ approve; auto-rejection when majority reject

### 3.3 Validator Ranking

- 🔲 **3.3.1** `ValidatorRankingService` — computes `ValidatorRank` for each active validator
- 🔲 **3.3.2** Normalized scoring: reputation, uptime, avg speed, stake weight
- 🔲 **3.3.3** Tier assignment thresholds (`Core` vs `Edge` cutoffs)
- 🔲 **3.3.4** Ranking cache with TTL; `ValidatorsCapacity` snapshot generation

### 3.4 Reputation System

- 🔲 **3.4.1** `+1` reputation on correct vote, applied on block settlement
- 🔲 **3.4.2** `−50` reputation on wrong vote, applied immediately on consensus resolution
- 🔲 **3.4.3** Status tier promotion/demotion: `Active → Warned → Suspended`
- 🔲 **3.4.4** Reward multiplier derived from reputation tier

### 3.5 Financial Penalties

- 🔲 **3.5.1** Invalid approval penalty: `−100 × TX amount` deducted from validator stake
- 🔲 **3.5.2** Valid rejection penalty: `−1 × TX amount` deducted from validator stake
- 🔲 **3.5.3** Penalty application keeps `Validation` and `Validator` in sync
- 🔲 **3.5.4** `Suspended` or `Penalized` validators removed from active assignment pool

---

## 4. Block System

### 4.1 TransactionBlock

- 🔲 **4.1.1** `TransactionBlockBuilder` — assembles 100 finalized transactions into a `TransactionBlock`
- 🔲 **4.1.2** Merkle root computation via `HYDRON.Cryptography.MerkleTree`
- 🔲 **4.1.3** Block hash computation via `HYDRON.Cryptography.HashProvider`
- 🔲 **4.1.4** `IBlockRepository` read/write for TransactionBlocks
- 🔲 **4.1.5** Genesis block factory (handles `previousHash = "0"×64` special case)

### 4.2 StateBlock

- 🔲 **4.2.1** `StateBlock` model — wraps 100 `TransactionBlock`s; electricity price; state root; immutability flag
- 🔲 **4.2.2** `StateBlockBuilder` — assembles from 100 confirmed TransactionBlocks
- 🔲 **4.2.3** State root = SHA-256 of all account state hashes (sorted-hash approach)
- 🔲 **4.2.4** Electricity price embedded at state block boundary (from oracle consensus)
- 🔲 **4.2.5** `IBlockRepository` read/write for StateBlocks
- 🔲 **4.2.6** Immutability enforcement: `IsImmutable = true` after 100-StateBlock depth

### 4.3 Block Finality

- 🔲 **4.3.1** Finality depth tracker (counts confirmations since block was included)
- 🔲 **4.3.2** Deterministic finality flag set at 66%+ supermajority of validators
- 🔲 **4.3.3** State settlement — apply all TX balance changes to account state on StateBlock commit
- 🔲 **4.3.4** Reorg window: blocks within 100-StateBlock depth can be challenged; beyond that are permanent

---

## 5. Rewards System

### 5.1 Reward Calculation

- 🔲 **5.1.1** `RewardCalculator` service — deterministically computes `BlockReward` from block contents
- 🔲 **5.1.2** Per-TX core reward: 1 HYA (100 atomos) per finalized transaction
- 🔲 **5.1.3** Per-TransactionBlock reward: 1 HYB (10,000 atomos) split among block validators
- 🔲 **5.1.4** Per-StateBlock reward: 1 HYG (100,000,000 atomos) split among state-block validators
- 🔲 **5.1.5** Reward multiplier application based on validator reputation tier
- 🔲 **5.1.6** Consistency check: sum of `ValidatorReward.TotalReward` must equal `BlockReward` totals (excluding fees)

### 5.2 Fee Handling

- 🔲 **5.2.1** Fee collection from sender balance at transaction ingest
- 🔲 **5.2.2** Fee distribution to first validator only (per protocol spec)
- 🔲 **5.2.3** Minimum fee enforcement: 1 HYD (10^16 atomos) at service layer

---

## 6. Electricity Price Oracle (`HYDRON.Connectivity`)

- 🏗️ Project stub exists

### 6.1 Data Sources

- 🔲 **6.1.1** EIA Open Data API — US electricity prices (USD/kWh, monthly)
- 🔲 **6.1.2** Eurostat — EU member-state electricity prices
- 🔲 **6.1.3** IEA Data Explorer — OECD country prices
- 🔲 **6.1.4** World Bank Population API — country population weights for weighted average

### 6.2 Price Calculation Pipeline

- 🔲 **6.2.1** Population-weighted global average USD/kWh from all sources
- 🔲 **6.2.2** Unit conversion chain: USD/kWh → USD/J → USD/eV
- 🔲 **6.2.3** `atomos_usd_price = 13.6 eV × consensus_usd_per_eV`
- 🔲 **6.2.4** Price update cadence: one consensus vote per StateBlock boundary

### 6.3 Oracle Consensus

- 🔲 **6.3.1** Each validator independently fetches and computes the electricity price
- 🔲 **6.3.2** Validators broadcast their price proposal; 66%+ agreement required
- 🔲 **6.3.3** Accepted price embedded in the next `StateBlock`
- 🔲 **6.3.4** Outlier rejection: proposals beyond ±20% of median are discarded

---

## 7. P2P Network (`HYDRON.Network`)

- 🏗️ Project stub exists

### 7.1 Transport

- 🔲 **7.1.1** TCP listener & outbound connection management
- 🔲 **7.1.2** TLS-over-TCP with Ed25519 peer identity
- 🔲 **7.1.3** Peer discovery — bootstrap nodes + DHT (Kademlia-style)
- 🔲 **7.1.4** Connection pool with max-peer cap and backpressure
- 🔲 **7.1.5** Peer metadata tracking (address, port, latency, last-seen, validator flag)

### 7.2 Message Protocol

- 🔲 **7.2.1** Message framing format (length-prefixed + message type byte)
- 🔲 **7.2.2** Message types: `TxBroadcast`, `ValidationVote`, `BlockProposal`, `OraclePriceProposal`, `PeerHandshake`, `PeerPing`
- 🔲 **7.2.3** JSON or MessagePack serialization (decision pending benchmark)
- 🔲 **7.2.4** Gossip fan-out for transaction and block propagation
- 🔲 **7.2.5** Deduplication: seen-message cache (LRU by hash)

### 7.3 Reliability

- 🔲 **7.3.1** Per-connection read/write timeouts
- 🔲 **7.3.2** Exponential-backoff reconnect for known peers
- 🔲 **7.3.3** Dead peer eviction and `Unreachable` validator status propagation
- 🔲 **7.3.4** Network partition detection and recovery handshake

---

## 8. RPC API

### 8.1 Wallet & Transfer Methods

- 🔲 **8.1.1** `wallet_create` — generate new `KeySafe` HD wallet; return public address
- 🔲 **8.1.2** `wallet_import` — import from master seed (Base64)
- 🔲 **8.1.3** `get_balance` — query account balance by address
- 🔲 **8.1.4** `transfer` — build, sign, and broadcast a `Transaction`
- 🔲 **8.1.5** `get_transaction` — query transaction by hash
- 🔲 **8.1.6** `estimate_fee` — return current minimum fee and suggested priority fee

### 8.2 Validator Methods

- 🔲 **8.2.1** `suggest_validator` — return best-ranked online validator for TX assignment
- 🔲 **8.2.2** `become_validator` — register stake deposit and activate validator node
- 🔲 **8.2.3** `get_validator_info` — full validator state by address
- 🔲 **8.2.4** `get_all_validators` — paginated list of active validators with rank
- 🔲 **8.2.5** `get_validator_stats` — rejection rate, uptime, reward history

### 8.3 Block Methods

- 🔲 **8.3.1** `get_transaction_block` — by number or hash
- 🔲 **8.3.2** `get_state_block` — by number or hash
- 🔲 **8.3.3** `get_block_height` — current TransactionBlock and StateBlock heights

### 8.4 Oracle Methods

- 🔲 **8.4.1** `get_electricity_price` — current consensus price (USD/kWh and derived atomos USD value)
- 🔲 **8.4.2** `get_electricity_price_history` — price per StateBlock (paginated)
- 🔲 **8.4.3** `get_oracle_votes` — current round's validator price proposals and consensus status

### 8.5 Network Methods

- 🔲 **8.5.1** `get_network_stats` — TX/s, active validators, mempool depth
- 🔲 **8.5.2** `get_peer_count` — number of connected peers
- 🔲 **8.5.3** `get_peer_info` — peer list with latencies

---

## 9. Testing & Quality

### 9.1 Unit Tests

- 🔲 **9.1.1** `RewardCalculatorTests`
- 🔲 **9.1.2** `MerkleTreeTests`
- 🔲 **9.1.3** `HashProviderTests`
- 🔲 **9.1.4** `SignatureVerifierTests`
- 🔲 **9.1.5** `OraclePriceCalculationTests` (weighted average + unit conversion)
- 🔲 **9.1.6** `ValidatorRankingTests`

### 9.2 Integration Tests

- 🔲 **9.2.1** Account → Transaction ingest → mempool flow
- 🔲 **9.2.2** Transaction → validator assignment → consensus → finalization flow
- 🔲 **9.2.3** TransactionBlock assembly → StateBlock settlement → account state update
- 🔲 **9.2.4** Reward distribution end-to-end (per-TX + per-block + fee)
- 🔲 **9.2.5** Oracle price consensus round (mock data sources)

### 9.3 End-to-End / Simulation Tests

- 🔲 **9.3.1** Local multi-validator simulation (in-process, no network)
- 🔲 **9.3.2** Full TX lifecycle: creation → settlement → balance update verified
- 🔲 **9.3.3** Consensus failure scenarios: <66% approval, validator dropout
- 🔲 **9.3.4** Penalty scenarios: invalid approval, valid rejection
- 🔲 **9.3.5** Finality depth and immutability window enforcement

---

## 10. Deployment & Documentation

### 10.1 Containerisation

- 🔲 **10.1.1** `Dockerfile` — multi-stage build (`sdk` → `runtime`)
- 🔲 **10.1.2** `docker-compose.yml` — local 3-validator testnet
- 🔲 **10.1.3** Health-check endpoint for container orchestrators

### 10.2 Configuration

- 🔲 **10.2.1** Mainnet `appsettings.Production.json`
- 🔲 **10.2.2** Testnet `appsettings.Testnet.json`
- 🔲 **10.2.3** Dev `appsettings.Development.json`
- 🔲 **10.2.4** Config validation on startup (fail-fast for missing/invalid values)

### 10.3 Documentation

- 🔲 **10.3.1** RPC API specification (OpenAPI / Swagger)
- 🔲 **10.3.2** Architecture guide — layer diagram, data flow, consensus sequence
- 🔲 **10.3.3** Developer guide — how to run locally, how to add a new RPC method
- 🔲 **10.3.4** Deployment guide — node setup, staking, network join
- 🔲 **10.3.5** Physics peg explainer — how 13.6 eV maps to atomos USD price

---

## Status Summary

**Last commit:** `224e17f` — "Small refactoring of the code." (July 12, 2026)

| Area | Status |
|------|--------|
| Data Models (10 classes) | ✅ Complete — all critical audit defects resolved |
| Database Layer | 🏗️ Stub only |
| Cryptography services | 🏗️ Stub only |
| Core bootstrapping & constants | 🏗️ Stub only |
| Validator services | 🏗️ Stub only |
| Connectivity / Oracle | 🏗️ Stub only |
| Network / P2P | 🏗️ Stub only |
| RPC API | 🔲 Not started |
| Unit tests | 🔲 Not started |
| Integration tests | 🔲 Not started |
| Deployment / Docs | 🔲 Not started |

---

## Immediate Next Priorities (Phase 2 entry gate)

### Step 1 — Close remaining model-layer open items (1–2 days)
1. Assign explicit integer values to all enums in `Enumerators.cs` (serialization safety before DB layer)
2. Wrap `IncrementNonce` in `Account.cs` with `_balanceLock`
3. Resolve `TransactionBlock` setter/`Seal` redundancy — pick one pattern and remove the other

### Step 2 — `HYDRON.Cryptography` (2–3 days)
4. `SystemConstants` — reward amounts, block sizes, fee floor, physics constant (13.6 eV), consensus threshold
5. `HashProvider` — SHA-256 canonical hasher for transactions and blocks
6. `MerkleTree` — binary Merkle builder; produces root that `TransactionBlock.Seal()` consumes
7. `SignatureVerifier` — Ed25519 verify wrapper (service-side, decoupled from `KeySafe`)

### Step 3 — Model-layer unit tests `HYDRON.Tests` (3–4 days)
8. `AtomosTests` — arithmetic, denomination round-trips, overflow
9. `AccountTests` — concurrency on balance/nonce, state hash invalidation
10. `TransactionTests` — full status lifecycle, frozen validator guard, supermajority threshold
11. `ValidationTests` — sign-before-confirm/reject, penalize on both outcomes, reward assignment
12. `ValidatorTests` — stake/withdraw, penalty, reward block on Penalized, endpoint validation
13. `KeySafeTests` — child derivation (key + chain code stored), stealth round-trip, disposal
14. `TransactionBlockTests` — lock behaviour, seal idempotency, `IsValid` edge cases

### Step 4 — `HYDRON.Database` (3–5 days)
15. RocksDB wrapper with key namespace scheme
16. Repository interfaces + implementations for Account, Transaction, Validator, Block
17. JSON codec for `BigInteger`, `Atomos`, `DateTimeOffset`
18. Batch write operations for atomic multi-key commits

### Step 5 — Core service layer `HYDRON.Core` (ongoing)
19. `AccountService` — create, load, save, balance query
20. `TransactionBuilder` + ingest pipeline (sig verify, balance check, nonce check, fee guard)
21. `RewardCalculator` — deterministic reward computation from block contents
22. `SystemConstants` wired into all callers
