# HYDRON Phase 1 — Task State & Progress

**Last Updated:** June 27, 2026  
**Format:** Area-based, reflects actual repository state as of last commit (`bb97ae0`)

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Complete and audited — known open issues addressed |
| 🔶 | Structurally present but carries known defects from audit |
| 🔲 | Not started |
| 🏗️ | Project stub exists (`.csproj` + placeholder), no real logic yet |

---

## 1. Core Infrastructure

### 1.1 Data Models (`HYDRON.Models`)

- ✅ **1.1.1** `Atomos` — physics-pegged currency value type; 6 denominations (HYA→HYZ); full arithmetic operator set; `IComparable<Atomos>`, `IEquatable<Atomos>`; denomination conversion helpers
- ✅ **1.1.2** `Account` — user account state; balance management; nonce; handle; stealth public key; SHA-256 state hash with invalidation cache; validator subclass support
- ✅ **1.1.3** `Transaction` — transfer primitive; privacy modes (`Public`, `HiddenReceiver`, `FullyPrivate`); sender/receiver signature tracking; validator assignment & supermajority threshold; status lifecycle with valid-transition guard; fee; priority; block number assignment; finalization
- ✅ **1.1.4** `Validator` — validator account; staking/withdrawal; reputation score; correct/total vote counters; penalty application; tier (`Core`/`Edge`); status (`Active`/`Warned`/`Suspended`/`Penalized`/`Inactive`/`Unreachable`); network endpoint management (IPv4, IPv6, DNS, Tor)
- ✅ **1.1.5** `Validation` — per-validator vote record; approve/reject/confirm/dispute/penalize lifecycle; Ed25519 signature field; reward assignment; speed tracking
- ✅ **1.1.6** `TransactionBlock` — 100-TX block structure; Merkle root & state root slots; previous-hash chaining; validator address; block number; `IsValid()` check
- ✅ **1.1.7** `Rewards` — `BlockReward` + `ValidatorReward` records; per-TX (1 HYA), per-block (1 HYB), per-state-block (1 HYG) reward constants; fee reward; settlement status
- ✅ **1.1.8** `KeySafe` — HD wallet (BIP-32-style Ed25519 + X25519); HMAC-SHA-512 child derivation; stealth payment (`ComputeStealthPayment` / `ScanForStealthPayments`); key rotation; `IDisposable` with zeroing; sub-account construction
- ✅ **1.1.9** `ValidatorRank` — ranking snapshot record; normalized reputation, uptime, speed, stake fields; tier classification
- ✅ **1.1.10** `ValidatorsCapacity` — per-validator capacity snapshot; staked amount, avg speed, uptime score, block observation count, computed-at timestamp
- ✅ **1.1.11** `Enumerators` — all domain enums: `TransactionStatus`, `ValidationStatus`, `ValidatorStatus`, `ValidatorTier`, `Priority`, `PrivacyMode`, `RewardStatus`

### Known open defects in completed models (from audit — must fix before moving to Phase 2)

- 🔶 `Atomos` — `FromDenomination`/`ToDenomination` use `double` for large denominations (Hyd+), causing precision loss
- 🔶 `Account` — balance mutations (`TryDeductBalance`, `AddBalance`) are not atomic under concurrent access; state hash excludes validator-specific fields
- 🔶 `Transaction` — zero-validator consensus threshold possible; `RemoveValidator` allowed post-freeze; `AssignBlockNumber` is re-entrant; minimum fee (1 HYD) not enforced
- 🔶 `Validation` — `Confirm()` and `Reject()` do not require a signature to be present; zero-amount penalties accepted
- 🔶 `Validator` — IPv4/IPv6 field validation uses `IPAddress.TryParse` without address-family check; duplicate validation/rejection IDs not guarded; `GetVotingWeight()` non-zero for penalized validators
- 🔶 `KeySafe` — child derivation is unhardened (uses public key in HMAC data); `Dispose()` does not zero raw byte arrays; disposed-instance access not fully guarded in export methods
- 🔶 `ValidatorsCapacity` — all properties are fully mutable (`{ get; set; }`); `ValidatorAddress` and `Validator.Address` can diverge silently; `ValidationActivityCount` is `long` while validator uses `BigInteger`
- 🔶 `Enumerators` — all enums use implicit integer values (serialization fragility)

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
- 🔲 **1.3.1** `HashProvider` — SHA-256 canonical hasher for transactions, blocks, and state roots (replaces externally-injected hash strings)
- 🔲 **1.3.2** `MerkleTree` — binary Merkle tree builder from transaction hash list; produces canonical root that `TransactionBlock.SetMerkleRoot()` accepts
- 🔲 **1.3.3** `SignatureVerifier` — Ed25519 verify wrapper used by services (decoupled from `KeySafe` which is wallet-side only)
- 🔲 **1.3.4** `CryptoConstants` — system-wide crypto parameter definitions (key sizes, hash output lengths, HMAC algorithm identifiers)

### 1.4 Configuration & Bootstrapping (`HYDRON.Core`)

- 🏗️ Project stub exists
- 🔲 **1.4.1** `appsettings.json` template (mainnet / testnet / dev variants)
- 🔲 **1.4.2** Strongly-typed `HydronConfig` class
- 🔲 **1.4.3** DI service registry (`IServiceCollection` extensions)
- 🔲 **1.4.4** `HydronEngine` — main bootstrap; wires DB, crypto, network, validator, RPC
- 🔲 **1.4.5** `SystemConstants` — reward amounts, block sizes, consensus threshold (2/3), minimum fee (1 HYD), immutability window (100 blocks), physics constant (13.6 eV)

### 1.5 Error Handling & Logging

- 🔲 **1.5.1** Custom exception hierarchy (`HydronException`, `ConsensusException`, `InsufficientFundsException`, `InvalidTransactionException`, `CryptographyException`, etc.)
- 🔲 **1.5.2** Structured error codes & result types (`Result<T, HydronError>` pattern to replace throw-everywhere)
- 🔲 **1.5.3** `IHydronLogger` abstraction
- 🔲 **1.5.4** Structured logging via `Microsoft.Extensions.Logging` with context enrichment (block number, validator address, TX hash)

### 1.6 Unit Tests (`HYDRON.Tests` — new project needed)

- 🔲 **1.6.1** `AtomosTests` — arithmetic, denomination conversion, overflow, equality, comparison
- 🔲 **1.6.2** `AccountTests` — balance mutations under concurrency, state hash, nonce increment, handle validation
- 🔲 **1.6.3** `TransactionTests` — status lifecycle, supermajority threshold, signature requirements, finalization, fee guard
- 🔲 **1.6.4** `ValidationTests` — confirm/reject signature requirement, penalty guard, reward assignment
- 🔲 **1.6.5** `ValidatorTests` — staking, penalty, voting weight, endpoint validation, reachability
- 🔲 **1.6.6** `KeySafeTests` — derivation, stealth payment round-trip, rotation, disposal safety
- 🔲 **1.6.7** `TransactionBlockTests` — block validity, capacity, hash chaining

---

## 2. Account & Transaction Processing (`HYDRON.Core` services)

### 2.1 Account Management

- 🔲 **2.1.1** `AccountService` — load/save via repository; create new account
- 🔲 **2.1.2** Balance check queries (thread-safe read)
- 🔲 **2.1.3** Atomic balance deduction with lock (fix for audit defect AC-6)
- 🔲 **2.1.4** Nonce reservation & verification (prevent double-spend at service layer)
- 🔲 **2.1.5** Reward/penalty application from settled block

### 2.2 Transaction Processing

- 🔲 **2.2.1** `TransactionBuilder` — constructs and signs a `Transaction` from a `KeySafe`
- 🔲 **2.2.2** Sender signature verification on ingest
- 🔲 **2.2.3** Balance sufficiency check (amount + fee ≤ balance)
- 🔲 **2.2.4** Nonce ordering check (sender nonce must equal account nonce + 1)
- 🔲 **2.2.5** Fee validation — minimum 1 HYD enforced at service layer
- 🔲 **2.2.6** Double-spend prevention via nonce reservation in mempool
- 🔲 **2.2.7** Transaction status lifecycle orchestration (Pending → PendingValidation → ConsensusReached → Settled)
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

### 3.3 Validator Ranking (`HYDRON.Validator` service layer)

- 🔲 **3.3.1** `ValidatorRankingService` — computes `ValidatorRank` for each active validator (extracted from models per commit `1d1c2e5`)
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
- 🔲 **3.5.3** Penalty application keeps `Validation` and `Validator` in sync (fix for audit defect V-7)
- 🔲 **3.5.4** `Suspended` or `Penalized` validators removed from active assignment pool

---

## 4. Block System (`HYDRON.Core` / `HYDRON.Validator` services)

### 4.1 TransactionBlock

- 🔲 **4.1.1** `TransactionBlockBuilder` — assembles 100 finalized transactions into a `TransactionBlock`
- 🔲 **4.1.2** Merkle root computation via `HYDRON.Cryptography.MerkleTree`
- 🔲 **4.1.3** Block hash computation via `HYDRON.Cryptography.HashProvider`
- 🔲 **4.1.4** `IBlockRepository` read/write for TransactionBlocks
- 🔲 **4.1.5** Genesis block factory (handles `previousHash = "0"×64` special case)

### 4.2 StateBlock

- 🔲 **4.2.1** `StateBlock` model — wraps 100 `TransactionBlock`s; electricity price; state root; immutability flag
- 🔲 **4.2.2** `StateBlockBuilder` — assembles from 100 confirmed TransactionBlocks
- 🔲 **4.2.3** State root = SHA-256 of all account state hashes (Patricia-trie or sorted-hash approach)
- 🔲 **4.2.4** Electricity price embedded at state block boundary (from oracle consensus)
- 🔲 **4.2.5** `IBlockRepository` read/write for StateBlocks
- 🔲 **4.2.6** Immutability enforcement: `IsImmutable = true` after 100-StateBlock depth

### 4.3 Block Finality

- 🔲 **4.3.1** Finality depth tracker (counts confirmations since block was included)
- 🔲 **4.3.2** Deterministic finality flag set at 66%+ supermajority of validators
- 🔲 **4.3.3** State settlement — apply all TX balance changes to account state on StateBlock commit
- 🔲 **4.3.4** Reorg window: blocks within 100-StateBlock depth can be challenged; beyond that are permanent

---

## 5. Rewards System (`HYDRON.Core` service)

### 5.1 Reward Calculation

- 🔲 **5.1.1** `RewardCalculator` service — deterministically computes `BlockReward` from block contents (replaces externally-supplied reward numbers)
- 🔲 **5.1.2** Per-TX core reward: 1 HYA (100 atomos) per finalized transaction
- 🔲 **5.1.3** Per-TransactionBlock reward: 1 HYB (10,000 atomos) split among block validators
- 🔲 **5.1.4** Per-StateBlock reward: 1 HYG (100,000,000 atomos) split among state-block validators
- 🔲 **5.1.5** Reward multiplier application based on validator reputation tier
- 🔲 **5.1.6** Consistency check: sum of `ValidatorReward.TotalReward` must equal `BlockReward` totals

### 5.2 Fee Handling

- 🔲 **5.2.1** Fee collection from sender balance at transaction ingest
- 🔲 **5.2.2** Fee distribution to first validator only (per protocol spec)
- 🔲 **5.2.3** Minimum fee enforcement: 1 HYD (10^16 atomos) at service layer
- 🔲 **5.2.4** Fee market: future enhancement — dynamic fee floor based on mempool congestion

---

## 6. Electricity Price Oracle (`HYDRON.Connectivity`)

- 🏗️ Project stub exists (`HYDRON.Connectivity`)

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
- 🔲 **7.1.2** TLS-over-TCP with Ed25519 peer identity (reuse validator key for peer auth)
- 🔲 **7.1.3** Peer discovery — bootstrap nodes + DHT (Kademlia-style)
- 🔲 **7.1.4** Connection pool with max-peer cap and backpressure
- 🔲 **7.1.5** Peer metadata tracking (address, port, latency, last-seen, validator flag)

### 7.2 Message Protocol

- 🔲 **7.2.1** Message framing format (length-prefixed + message type byte)
- 🔲 **7.2.2** Message types: `TxBroadcast`, `ValidationVote`, `BlockProposal`, `OraclePriceProposal`, `PeerHandshake`, `PeerPing`
- 🔲 **7.2.3** JSON or MessagePack serialization (decision pending benchmark)
- 🔲 **7.2.4** Gossip fan-out for transaction and block propagation
- 🔲 **7.2.5** Deduplication: seen-message cache (LRU by hash) to prevent re-broadcast loops

### 7.3 Reliability

- 🔲 **7.3.1** Per-connection read/write timeouts
- 🔲 **7.3.2** Exponential-backoff reconnect for known peers
- 🔲 **7.3.3** Dead peer eviction and `Unreachable` validator status propagation
- 🔲 **7.3.4** Network partition detection and recovery handshake

---

## 8. RPC API (`HYDRON.Core` / HTTP layer)

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

### 9.1 Unit Tests (see §1.6 for model-layer tests)

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

### 9.4 Code Quality

- 🔲 **9.4.1** Audit defect resolution checklist (see `docs/Audit Findings.md`)
- 🔲 **9.4.2** Performance benchmarks (Atomos arithmetic, TX throughput, Merkle computation)
- 🔲 **9.4.3** Memory profiling (especially `KeySafe` disposal and `BigInteger` allocation hot paths)

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

**Last commit:** `bb97ae0` — "Corrected some logic gaps" (June 13, 2026)

| Area | Status |
|------|--------|
| Data Models (11 classes) | 🔶 Structurally complete; audit defects pending resolution |
| Database Layer | 🏗️ Stub only |
| Cryptography services | 🏗️ Stub only |
| Core bootstrapping | 🏗️ Stub only |
| Validator services | 🏗️ Stub only |
| Connectivity / Oracle | 🏗️ Stub only |
| Network / P2P | 🏗️ Stub only |
| RPC API | 🔲 Not started |
| Unit tests | 🔲 Not started |
| Integration tests | 🔲 Not started |
| Deployment / Docs | 🔲 Not started |

**Immediate next priorities:**
1. Resolve critical audit defects in `Atomos`, `Account`, `Transaction`, `Validation`, `KeySafe` before building any service layer on top of them
2. Implement `HYDRON.Cryptography` — `HashProvider`, `MerkleTree`, `SignatureVerifier` (unblocks block validity and end-to-end TX verification)
3. Implement `SystemConstants` (unblocks reward calculation and fee enforcement)
4. Write model-layer unit tests (§1.6) to lock in invariants before service wiring begins
5. Implement database layer (§1.2) to enable persistence for integration testing
