# HYDRON Phase 1 — Task State & Progress

**Last Updated:** September 6, 2026
**Format:** Area-based, reflects actual repository state as of last commit (`a0c5c5b`)

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
- ✅ **1.1.2** `Account` — user account state; balance management with `Lock` for thread-safe mutations; nonce (thread-safe via `_balanceLock`); handle; stealth public key; SHA-256 state hash with lock-protected invalidation cache; `private protected` restoration constructor + `internal static Restore()` factory for DB hydration
- ✅ **1.1.3** `Transaction` — transfer primitive; privacy modes (`Public`, `HiddenReceiver`, `FullyPrivate`); sender/receiver signature tracking; validator assignment & supermajority threshold; frozen validator count guard; status lifecycle with valid-transition map; fee; priority; block number assignment; finalization; private restoration constructor + `internal static Restore()` factory
- ✅ **1.1.4** `Validator` — validator account; staking/withdrawal; reputation score; correct/total vote counters; penalty application; tier (`Core`/`Edge`); status (`Active`/`Warned`/`Suspended`/`Penalized`/`Inactive`/`Unreachable`); network endpoint validation (IPv4/IPv6 address-family verified); `_confirmedValidationIds` / `_rejectedValidationIds` sets; private restoration constructor + `internal static Restore()` factory
- ✅ **1.1.5** `Validation` — per-validator vote record; sign-before-confirm/reject enforced; `Penalize` works on both `Confirmed` and `Rejected` outcomes; reward assignment; speed tracking
- ✅ **1.1.6** `TransactionBlock` — 100-TX block structure; `Lock`-protected `Seal` and `AddTransaction`; `ElectricityPriceAtomosPerEv` field (oracle snapshot at block-production time); `StateRoot`; `IsSealed` changed to `private protected set`; `internal RestoreSealed()` method added; private restoration constructor + `internal static Restore()` factory; `_restoredTransactionHashes` field for DB-load path
- ✅ **1.1.7** `StateBlock` — seals 100 `TransactionBlock`s; `GlobalStateRoot`; `TotalFeesCollected`; stores `TransactionBlockHashes` only; private restoration constructor + `internal static Restore()` factory
- ✅ **1.1.8** `Rewards` — `BlockReward` + `ValidatorReward` records; `TotalMinted` excludes fee rewards; `ValidatorReward.TotalReward` = blockReward + validationReward + feeReward; settlement status
- ✅ **1.1.9** `KeySafe` — HD wallet (BIP-32-style Ed25519 + X25519); HMAC-SHA-512 child key derivation; stealth payment; key rotation; `IDisposable` with `CryptographicOperations.ZeroMemory`; NSec.Cryptography 26.4.0 / libsodium 1.0.22
- ✅ **1.1.10** `ValidatorRank` — ranking snapshot record; normalized reputation, uptime, speed, stake fields; tier classification
- ✅ **1.1.11** `Enumerators` — all domain enums: `TransactionStatus`, `ValidationStatus`, `ValidatorStatus`, `ValidatorTier`, `Priority`, `PrivacyMode`, `RewardStatus`
- ✅ **1.1.12** `Mempool` — pending transaction queue; thread-safe; priority ordering

### Models — Open items

- `TransactionBlock._restoredTransactionHashes` is populated on DB restore path; the engine must hydrate full `Transaction` objects from those hashes post-load

---

### 1.2 Database Layer (`HYDRON.Database`)

- ✅ **1.2.1** `IDataStore` — generic key/value contract: `Put`, `TryGet`, `Delete`, `Exists`, `WriteBatch` (atomic multi-key), `Iterate` (prefix scan)
- ✅ **1.2.2** `RocksDbDataStore : IDataStore` — RocksDB wrapper using **RocksDB by Curiosity** NuGet (v11.8.1.4423); UTF-8 key encoding; `IDisposable`; `ObjectDisposedException` guards on all methods
- ✅ **1.2.3** `KeyScheme` — all RocksDB key patterns; string-indexed entities use `PREFIX:IDENTIFIER`; block-number keys use **256-bit big-endian hex encoding** (64-char suffix) for correct lexicographic ordering
- ✅ **1.2.4** `IAccountRepository` + `AccountRepository` — save/get by address/exists/get-all; prefix scan via `KeyScheme.AccountPrefix`
- ✅ **1.2.5** `ITransactionRepository` + `TransactionRepository` — save/get by hash/exists/get-by-sender/get-all; atomic dual-write (primary + sender index) via `WriteBatch`
- ✅ **1.2.6** `IValidatorRepository` + `ValidatorRepository` — save/get by address/exists/get-all/get-active/get-by-tier
- ✅ **1.2.7** `IBlockRepository` + `BlockRepository` — save/get TransactionBlocks and StateBlocks by number and by hash; latest block number metadata keys; sealed-only enforcement before persist
- ✅ **1.2.8** Batch write operations — `WriteBatch` on `IDataStore`; atomic multi-key commits
- ✅ **1.2.9** Range / iterator queries — `Iterate(prefix)` on `IDataStore`
- ✅ **1.2.10** JSON serialization codec — `ISerializer` / `HydronJsonSerializer` (System.Text.Json); `BigInteger` and `Atomos` as decimal strings; `DateTimeOffset` via built-in JSON support
- ✅ **1.2.11** DTO layer — `AccountDto`, `ValidatorDto : AccountDto`, `TransactionDto`, `TransactionBlockDto`, `StateBlockDto`
- ✅ **1.2.12** `DtoMapper` — static bidirectional mapping; calls `Restore()` factories on all model types
- ✅ **1.2.13** `InternalsVisibleTo` — `HYDRON.Models.csproj` grants `HYDRON.Database` access to `internal` members
- ✅ **1.2.14** `HYDRON.Database.csproj` — `PublishAot=false`; `ProjectReference` to `HYDRON.Models`

### Database — Open items

- `ValidatorRepository.GetActive()` and `GetByTier()` perform full scans and filter in-memory; a secondary index may be needed at network scale
- `BlockRepository` stores `TransactionBlock` with **transaction hashes only**; engine layer responsible for hydrating full transactions post-load

---

### 1.3 Cryptography (`HYDRON.Cryptography`)

- ✅ **1.3.1** `HashProvider` — SHA-256 canonical hasher; `IncrementalHashWriter` (private sealed, `IDisposable`); canonical field ordering for `Transaction`, `TransactionBlock` header, `StateBlock` header, and state root; `WriteAtomos` / `WriteBigInteger` / `WriteString` (length-prefixed UTF-8, `ArrayPool<byte>`) / `WriteInt32` / `WriteInt64` / `WriteBool`; `stackalloc` for all fixed-width writes; `ComputeStateRoot` sorts hashes lexicographically; empty account set guard throws `ArgumentException`
- ✅ **1.3.2** `MerkleTree` — binary Merkle builder over ordered SHA-256 hex hash list; odd-level duplicate padding (Bitcoin-style); `stackalloc byte[64]` pair-hash; input hash length validation against `CryptoConstants.Sha256HexLength`; `EmptyRoot` sentinel for empty TX list
- ✅ **1.3.3** `SignatureVerifier` — stateless Ed25519 verify wrapper (decoupled from `KeySafe`); `Verify(string, …)` for UTF-8 signed data; `VerifyBytes(byte[]?, …)` for raw byte payloads; length guards on signature (64 B) and public key (32 B) before `PublicKey.Import`; catch-all returns `false`
- ✅ **1.3.4** `CryptoConstants` — `Ed25519PublicKeyBytes` (32), `Ed25519PrivateKeyBytes` (32), `Ed25519SignatureBytes` (64), `Sha256Bytes` (32), `Sha256HexLength` (64), `GenesisPreviousHash` (64 × `'0'`)
- ✅ **1.3.5** `HYDRON.Cryptography.csproj` — class library; `PublishAot=true`; `NSec.Cryptography` 26.4.0; `ProjectReference` to `HYDRON.Models`

### Cryptography — Open items

- `KeySafe.Verify()` (in `HYDRON.Models`) is a functional duplicate of `SignatureVerifier.Verify()` — keep in sync if canonical data format changes; consider routing through `SignatureVerifier` once project reference is available

---

### 1.4 Configuration & Bootstrapping (`HYDRON.Core`)

- ✅ **1.4.5** `SystemConstants` — `TxReward` (1 HYA), `TransactionBlockReward` (1 HYB), `StateBlockReward` (1 HYG), `MinimumFee` (1 HYD); `TransactionsPerBlock` (100), `BlocksPerStateBlock` (100), `ImmutabilityDepth` (100); `SupermajorityThreshold` (2.0/3.0); `HydrogenIonizationEnergyEv` (13.6m); `IsSupermajority(int approvals, int total)` helper using `Math.Ceiling`
- ✅ **1.4.6** `HYDRON.Core.csproj` — class library; `PublishAot=true` (deferred — AOT compatibility to be validated at build phase); `ProjectReference` to `HYDRON.Models`, `HYDRON.Cryptography`, `HYDRON.Database`
- 🔲 **1.4.1** `appsettings.json` template (mainnet / testnet / dev variants)
- 🔲 **1.4.2** Strongly-typed `HydronConfig` class
- 🔲 **1.4.3** DI service registry (`IServiceCollection` extensions)
- 🔲 **1.4.4** `HydronEngine` — main bootstrap; wires DB, crypto, network, validator, RPC

### Core — Open items

- `PublishAot=true` on `HYDRON.Core` will likely conflict with `Microsoft.Extensions.DependencyInjection` reflection-based service scanning — to be resolved at build/publish phase; Option A is `PublishAot=false`, Option B is AOT-compatible source-gen DI

---

### 1.5 Error Handling & Logging

- 🔲 **1.5.1** Custom exception hierarchy (`HydronException`, `ConsensusException`, `InsufficientFundsException`, `InvalidTransactionException`, `CryptographyException`)
- 🔲 **1.5.2** Structured error codes & result types (`Result<T, HydronError>` pattern)
- 🔲 **1.5.3** `IHydronLogger` abstraction
- 🔲 **1.5.4** Structured logging via `Microsoft.Extensions.Logging` with context enrichment

### 1.6 Unit Tests (`HYDRON.Tests`)

- 🔲 **1.6.1** `AtomosTests` — arithmetic, denomination conversion, overflow, equality, comparison
- 🔲 **1.6.2** `AccountTests` — balance mutations under concurrency, state hash, nonce increment, handle validation
- 🔲 **1.6.3** `TransactionTests` — status lifecycle, supermajority threshold, frozen validator guard, signature requirements, finalization
- 🔲 **1.6.4** `ValidationTests` — sign-before-confirm, penalize on both Confirmed and Rejected, reward assignment
- 🔲 **1.6.5** `ValidatorTests` — staking, penalty, voting weight, endpoint validation, reachability, reward block on Penalized
- 🔲 **1.6.6** `KeySafeTests` — child derivation (key + chain code), stealth payment round-trip, HMAC spend key derivation, rotation, disposal safety
- 🔲 **1.6.7** `TransactionBlockTests` — block validity, capacity, hash chaining, lock behaviour
- 🔲 **1.6.8** `DatabaseTests` — round-trip persist/restore for all 5 model types; key ordering correctness; batch atomicity
- 🔲 **1.6.9** `HashProviderTests` — known-vector TX hash; block header hash determinism; state root sort-independence; empty account set guard
- 🔲 **1.6.10** `MerkleTreeTests` — single element; even/odd lists; duplicate padding; empty list; invalid hash length rejection
- 🔲 **1.6.11** `SignatureVerifierTests` — valid round-trip; wrong key; tampered data; malformed Base64; wrong length
- 🔲 **1.6.12** `SystemConstantsTests` — `IsSupermajority` boundary cases (0/0, 1/1, 2/3, 66/100, 67/100, 65/100)

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
- 🔲 **2.2.5** Fee validation — minimum `SystemConstants.MinimumFee` enforced at service layer
- 🔲 **2.2.6** Double-spend prevention via nonce reservation in mempool
- 🔲 **2.2.7** Transaction status lifecycle orchestration
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
- 🔲 **3.2.5** Consensus vote aggregation via `SystemConstants.IsSupermajority`
- 🔲 **3.2.6** First-validator veto gate
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

- 🔲 **4.1.1** `TransactionBlockBuilder` — assembles `SystemConstants.TransactionsPerBlock` finalized transactions into a `TransactionBlock`
- 🔲 **4.1.2** Merkle root computation via `HYDRON.Cryptography.MerkleTree`
- 🔲 **4.1.3** Block hash computation via `HYDRON.Cryptography.HashProvider`
- 🔲 **4.1.4** Genesis block factory (handles `previousHash = CryptoConstants.GenesisPreviousHash`)

### 4.2 StateBlock

- 🔲 **4.2.1** `StateBlockBuilder` — assembles from `SystemConstants.BlocksPerStateBlock` confirmed TransactionBlocks
- 🔲 **4.2.2** State root via `HashProvider.ComputeStateRoot`
- 🔲 **4.2.3** Electricity price consensus: median of oracle snapshots (66%+ validator agreement)
- 🔲 **4.2.4** `IBlockRepository` read/write for StateBlocks
- 🔲 **4.2.5** Immutability enforcement: `IsImmutable = true` after `SystemConstants.ImmutabilityDepth` StateBlocks

### 4.3 Block Finality

- 🔲 **4.3.1** Finality depth tracker
- 🔲 **4.3.2** Deterministic finality flag set at 66%+ supermajority
- 🔲 **4.3.3** State settlement — apply all TX balance changes on StateBlock commit
- 🔲 **4.3.4** Reorg window: `SystemConstants.ImmutabilityDepth` StateBlocks

---

## 5. Rewards System

### 5.1 Reward Calculation

- 🔲 **5.1.1** `RewardCalculator` service
- 🔲 **5.1.2** Per-TX core reward: `SystemConstants.TxReward` (1 HYA)
- 🔲 **5.1.3** Per-TransactionBlock reward: `SystemConstants.TransactionBlockReward` (1 HYB)
- 🔲 **5.1.4** Per-StateBlock reward: `SystemConstants.StateBlockReward` (1 HYG)
- 🔲 **5.1.5** Reward multiplier application based on validator reputation tier
- 🔲 **5.1.6** Consistency check: sum of `ValidatorReward.TotalReward` = `BlockReward` totals (excl. fees)

### 5.2 Fee Handling

- 🔲 **5.2.1** Fee collection from sender balance at transaction ingest
- 🔲 **5.2.2** Fee distribution to first validator only
- 🔲 **5.2.3** Minimum fee enforcement: `SystemConstants.MinimumFee` (1 HYD)

---

## 6. Electricity Price Oracle (`HYDRON.Connectivity`)

- 🏗️ Project stub exists

### 6.1 Data Sources

- 🔲 **6.1.1** EIA Open Data API — US electricity prices (USD/kWh, monthly)
- 🔲 **6.1.2** Eurostat — EU member-state electricity prices
- 🔲 **6.1.3** IEA Data Explorer — OECD country prices
- 🔲 **6.1.4** World Bank Population API — country population weights

### 6.2 Price Calculation Pipeline

- 🔲 **6.2.1** Population-weighted global average USD/kWh
- 🔲 **6.2.2** Unit conversion chain: USD/kWh → USD/J → USD/eV
- 🔲 **6.2.3** `atomos_usd_price = SystemConstants.HydrogenIonizationEnergyEv × consensus_usd_per_eV`
- 🔲 **6.2.4** Price update cadence: one consensus vote per StateBlock boundary

### 6.3 Oracle Consensus

- 🔲 **6.3.1** Each validator independently fetches and computes the electricity price
- 🔲 **6.3.2** 66%+ validator agreement required (`SystemConstants.IsSupermajority`)
- 🔲 **6.3.3** Accepted price embedded in each `TransactionBlock` as `ElectricityPriceAtomosPerEv`
- 🔲 **6.3.4** Outlier rejection: proposals beyond ±20% of median discarded

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

- 🔲 **9.1.1** `AtomosTests`
- 🔲 **9.1.2** `AccountTests`
- 🔲 **9.1.3** `TransactionTests`
- 🔲 **9.1.4** `ValidationTests`
- 🔲 **9.1.5** `ValidatorTests`
- 🔲 **9.1.6** `KeySafeTests`
- 🔲 **9.1.7** `TransactionBlockTests`
- 🔲 **9.1.8** `DatabaseLayerTests`
- 🔲 **9.1.9** `HashProviderTests` — known-vector TX hash; block header hash determinism; state root sort-independence; empty account set guard
- 🔲 **9.1.10** `MerkleTreeTests` — single element; even/odd lists; duplicate padding; empty list; invalid hash length rejection
- 🔲 **9.1.11** `SignatureVerifierTests` — valid round-trip; wrong key; tampered data; malformed Base64; wrong length
- 🔲 **9.1.12** `SystemConstantsTests` — `IsSupermajority` boundary cases (0/0, 1/1, 2/3, 66/100, 67/100, 65/100)

### 9.2 Integration Tests

- 🔲 **9.2.1** Account → Transaction ingest → mempool flow
- 🔲 **9.2.2** Transaction → validator assignment → consensus → finalization flow
- 🔲 **9.2.3** TransactionBlock assembly → StateBlock settlement → account state update
- 🔲 **9.2.4** Reward distribution end-to-end
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

| Area | Status |
|------|--------|
| Data Models — `HYDRON.Models` (12 classes) | ✅ Complete |
| Database Layer — `HYDRON.Database` | ✅ Complete |
| Cryptography — `HYDRON.Cryptography` | ✅ Complete |
| Core constants — `HYDRON.Core` (`SystemConstants`) | ✅ Complete |
| Core services — `HYDRON.Core` (engine, DI, config) | 🔲 Not started |
| Validator services — `HYDRON.Validator` | 🏗️ Stub only |
| Connectivity / Oracle — `HYDRON.Connectivity` | 🏗️ Stub only |
| Network / P2P — `HYDRON.Network` | 🏗️ Stub only |
| RPC API | 🔲 Not started |
| Unit tests | 🔲 Not started |
| Integration tests | 🔲 Not started |
| Deployment / Docs | 🔲 Not started |

---

## Immediate Next Priorities

### Step 3 — Unit Tests (`HYDRON.Tests`) (next)
1. Set up `HYDRON.Tests.csproj` — xUnit; `ProjectReference` to Models, Cryptography, Database, Core; `InternalsVisibleTo` grants from all four projects
2. `SystemConstantsTests` — `IsSupermajority` boundary cases
3. `AtomosTests` — arithmetic, denomination round-trips, overflow, equality
4. `HashProviderTests` — TX hash known-vector; state root sort-independence; empty guard
5. `MerkleTreeTests` — even/odd/single/empty/invalid-length cases
6. `SignatureVerifierTests` — valid round-trip; failure cases
7. `DatabaseLayerTests` — round-trip for all 5 model types; key ordering; batch atomicity
8. Remaining model tests (Account, Transaction, Validation, Validator, TransactionBlock, KeySafe)

### Step 4 — `HYDRON.Core` service layer
9. `AccountService` — create, load, save, balance query
10. `TransactionBuilder` + ingest pipeline (sig verify, balance check, nonce check, fee guard)
11. `RewardCalculator` — deterministic reward computation from block contents
12. `HydronEngine` — main bootstrap wiring DB + crypto + services
