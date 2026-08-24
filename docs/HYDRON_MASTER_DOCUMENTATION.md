# HYDRON: A Physics-Pegged Decentralized Financial Network

**Version:** 0.1 (Working Draft)
**Last Updated:** July 24, 2026
**Repository:** [github.com/Aleks7544/HYDRON](https://github.com/Aleks7544/HYDRON)

> This document serves as the canonical living reference for HYDRON's design, architecture, and implementation state. It is intended to be read by both contributors and the AI assistant collaborating on the project. Sections marked **[IMPLEMENTED]** reflect code already present in the repository. Sections marked **[PLANNED]** describe design decisions not yet translated into code.

---

## Table of Contents

1. [Abstract](#1-abstract)
2. [Motivation & Philosophy](#2-motivation--philosophy)
3. [The Atomos: A Physics-Pegged Base Unit](#3-the-atomos-a-physics-pegged-base-unit)
4. [Denomination System](#4-denomination-system)
5. [Cryptographic Foundation](#5-cryptographic-foundation)
6. [Account Model & Identity](#6-account-model--identity)
7. [The Transaction Lifecycle](#7-the-transaction-lifecycle)
8. [The Validator System](#8-the-validator-system)
9. [Consensus Mechanism](#9-consensus-mechanism)
10. [Block Architecture](#10-block-architecture)
11. [Rewards, Fees & Monetary Issuance](#11-rewards-fees--monetary-issuance)
12. [The Electricity Price Oracle](#12-the-electricity-price-oracle)
13. [Privacy Model](#13-the-privacy-model)
14. [Network & RPC Layer](#14-network--rpc-layer)
15. [Implementation State](#15-implementation-state)
16. [Open Design Questions](#16-open-design-questions)

---

## 1. Abstract

HYDRON is a decentralized, permissionless financial processing network whose monetary unit is pegged not to a fiat currency, commodity, or algorithmic supply schedule, but to an immutable physical constant: the ionization energy of the hydrogen atom. The smallest unit of value in HYDRON, called the **atomos**, derives its fiat-denominated pricings from the energy required to ionize a single hydrogen atom in the ground state (13.6 eV), via a consensus-voted, population-weighted global electricity price. This anchoring mechanism removes speculative monetary inflation from the system's design while preserving decentralization, since the electricity price oracle is determined by validator consensus rather than any central authority.

The network processes transactions through a delegated supermajority Byzantine Fault Tolerant (BFT) consensus protocol. Validators are economically incentivized to behave correctly through a tiered reward structure and an asymmetric slashing regime. Blocks are organized into two layers — TransactionBlocks (containing up to 100 transactions each) and StateBlocks (containing 100 TransactionBlocks each) — with deterministic finality achieved at a two-thirds supermajority and permanent immutability enforced after 100 StateBlocks.

The implementation language is C# (.NET), with RocksDB as the persistent storage engine and Ed25519 / X25519 as the cryptographic primitives.

---

## 2. Motivation & Philosophy

Other cryptocurrencies that exist derive their monetary values from sources like: market speculation (Bitcoin, Ethereum), fiat collateral (USDC, USDT), commodity collateral (PAX Gold, Tether Gold), or an algorithmic supply-and-demand model (algorithmic stablecoins, most of which have failed catastrophically). None of these approaches produces a unit of account with an intrinsic, physics-grounded value. Market-driven assets are volatile by design. Fiat-backed assets are only as trustworthy as their custodians. Algorithmic models are circular — they are stable only as long as participants believe they will remain stable.

HYDRON proposes a different model: pegging monetary value to a quantity of energy as defined by atomic physics. The energy required to strip the sole electron from a hydrogen atom in its ground state is 13.6 electronvolts. This number is not a policy decision, not a consensus outcome, and not subject to revision. It is a property of the universe. By multiplying this constant against the real cost of generating one electronvolt's worth of electrical energy — derived from actual global electricity market data and weighted by population — HYDRON's base unit acquires a price that is anchored to physical reality rather than to financial convention.

This approach does not promise price stability in the traditional sense. The atomos price will fluctuate as global electricity prices change. But that fluctuation is driven by the actual cost of energy in the physical world, making it economically meaningful rather than speculative. A currency whose value moves with the price of energy is, in many ways, a more honest representation of economic productivity than a currency whose value moves with trader sentiment.

The network itself is designed to be a lean, high-throughput financial processing layer. It is not a smart contract platform. It does not support general computation. Its scope is deliberately narrow: transfer value securely, finalize transactions quickly, and do so in a decentralized manner that does not require trust in any individual participant.

---

## 3. The Atomos: A Physics-Pegged Base Unit

The **atomos** is the indivisible base unit of HYDRON. Its name is derived from the ancient Greek word for "uncuttable" — the philosophical predecessor to the modern atom — which is fitting both linguistically and conceptually, since it represents the energy content of a single hydrogen atom.

The pricing formula is:

```
atomos_price[REF] = 13.6 × electricity_price_[REF]_per_eV
```

Where `13.6 eV` is the first ionization energy of hydrogen (H-1, protium), and `electricity_price_[REF]_per_eV` is derived from the consensus-voted, population-weighted global average cost of electricity, converted from [REF]/kWh through the following chain - [REF] in this case can be any fiat currency or IMF's Special Drawing Right (SDR) by default:

```
electricity_price_[REF]_per_kWh
  ÷ 3,600,000 (J/kWh)         → [REF]/J
  ÷ 1.602176634 × 10⁻¹⁹ (J/eV) → [REF]/eV
```

The hydrogen isotope used as the reference is H-1 (protium), selected for its natural abundance of 99.99% and atomic mass of 1.007825 u. These properties make it the most representative and universally available atomic reference point.

The atomos is always a whole integer. Fractional atomos do not exist. This means the system operates with discrete, countable units at the base level, and the denomination system (described in the next section) provides the necessary granularity for human-readable amounts. Internally, all balances and transaction amounts are stored and computed as raw atomos counts using arbitrary-precision integers (`BigInteger` in the C# implementation), which ensures there is no overflow risk even at the HYZ denomination level (10⁶⁴ atomos).

**[IMPLEMENTED]** The `Atomos` struct in `HYDRON.Models` implements this value type with full arithmetic, denomination conversion, scaling utilities, and all necessary comparison operators. Negative values are rejected at the type level.

---

## 4. Denomination System

Because the atomos represents an extraordinarily small unit of value (a fraction of a microcentime at current electricity prices), HYDRON defines six higher denominations for practical use. The denomination ladder follows a squaring progression: each denomination contains a number of atomos equal to the square of the previous denomination's atomos count. This produces an exponential scale that accommodates everything from micro-transactions at the HYA level to institutional-scale settlements at the HYZ level.

| Code | Greek Name | Atomos Count | Notes |
|------|-----------|--------------|-------|
| HYA  | Alpha     | 10² = 100    | Smallest named denomination |
| HYB  | Beta      | 10⁴ = 10,000 | HYA² |
| HYG  | Gamma     | 10⁸          | HYB² |
| HYD  | Delta     | 10¹⁶         | HYG² intended for everyday multi-purpose usage |
| HYE  | Epsilon   | 10³²         | HYD² |
| HYZ  | Zeta      | 10⁶⁴         | HYE² maximum denomination |

Amounts may be expressed as decimals in any denomination. For example, 34,562 atomos is equivalently 345.62 HYA, 3.4562 HYB, or 0.00000034562 HYG. The system always stores the canonical atomos count and performs denomination conversion only for display or input purposes.

**[IMPLEMENTED]** The `Atomos` struct carries all six denomination factors as static `BigInteger` constants and provides `FromDenomination()`, `ToDenomination()`, and `RemainderAfterDenomination()` conversion methods. The `Denominations` enum maps the six codes.

---

## 5. Cryptographic Foundation

HYDRON's cryptographic design prioritizes modern, well-audited primitives with strong security properties and efficient performance. The following algorithms are used:

**EdDSA Ed25519** is the signing algorithm for all transaction signatures, validation votes, and identity. Ed25519 was chosen over ECDSA (secp256k1) for its deterministic signature generation (no random nonce required, eliminating the nonce-reuse vulnerability), its shorter key and signature sizes, and its significantly faster verification time. All addresses on the network are derived by taking the SHA-256 hash of the Ed25519 public key and encoding it as a lowercase hexadecimal string.

**X25519** (Elliptic-Curve Diffie-Hellman over Curve25519) is used for stealth payment key agreement. Each account carries a separate X25519 public key — called the stealth public key — which is used by senders to compute a one-time shared secret that allows them to send funds to an unlinkable stealth address. This is conceptually similar to the stealth address scheme described in Monero's design, adapted here for use alongside Ed25519 identity keys.

**SHA-256** is the hash function used universally: for address derivation, transaction hashing, block hashing, Merkle tree construction, state root computation, and account state fingerprinting.

**HMAC-SHA512** and **HKDF-SHA256** are used for HD key derivation and stealth secret derivation respectively.

### The KeySafe

The `KeySafe` class is HYDRON's wallet primitive. It encapsulates an Ed25519 private key, an X25519 stealth key pair, and an HD master seed. From a `KeySafe` instance, an account can sign transactions, compute stealth payment addresses, scan incoming transactions for stealth payments addressed to it, rotate its stealth key, and derive a hierarchy of child `KeySafe` instances.

HD child derivation follows a BIP32-style scheme adapted for Ed25519:

```
HMAC-SHA512(chainCode, [0x01 || parentPrivKey || index]) → childKey[0..32] + childChainCode[32..]
```

The `KeySafe` implements `IDisposable`. On disposal, the private key bytes are zeroed in memory using `CryptographicOperations.ZeroMemory()`, preventing key material from residing in memory after the wallet is closed.

**[IMPLEMENTED]** `KeySafe` in `HYDRON.Models` fully implements all of the above: key generation, signing, verification, HD derivation, stealth payment computation and scanning, stealth key rotation, and secure disposal.

---

## 6. Account Model & Identity

An account in HYDRON represents a participant's state on the network. It is defined by an immutable address and Ed25519 public key, a rotatable X25519 stealth public key, an optional human-readable handle (up to 1,000 UTF-8 bytes), a balance denominated in atomos, and a nonce.

The **nonce** is a monotonically increasing integer attached to every account. Each transaction submitted by an account must carry a nonce equal to the account's current nonce value, and the account's nonce is incremented exactly once upon transaction settlement. This prevents replay attacks: a valid transaction signed with nonce N cannot be resubmitted after it has been processed, because the account's nonce will have advanced to N+1.

Each account maintains a **state hash** — a SHA-256 digest computed over all account fields concatenated in a canonical format. This hash is computed lazily and cached; it is invalidated whenever any field changes. The state hash is used to construct the state root of each block, which allows the entire account state of the network to be verified by checking a single hash per block.

**Thread safety** is built into the account model. Balance mutations and nonce increments are protected by lock primitives, making the model safe for use in the concurrent validator environment where multiple goroutines may attempt to read or write account state simultaneously.

### Validator Accounts

Validators are a specialization of accounts. A `Validator` inherits all account properties and adds: a staked amount (the economic security deposit), a tier designation (Core or Edge), a status (Active, Inactive, Unreachable, Penalized, Warned, or Suspended), vote statistics, and network endpoint metadata.

The **staked amount** serves two purposes. First, it is the economic collateral that validators risk when participating in consensus — misbehavior results in slashing this stake. Second, it is used to compute voting weight: a validator's influence on consensus outcomes is proportional to its stake, subject to the constraint that penalized or suspended validators have zero voting weight regardless of stake.

Two validator tiers exist. **Core validators** are high-reputation, high-stake participants who earn larger block rewards. **Edge validators** are standard participants who earn smaller block rewards but still participate fully in consensus. Tier promotion and demotion are driven by the `ValidatorRank` scoring mechanism, which computes a composite rank from staked amount, average validation speed, activity count, normalized reputation score, and blocks observed.

The **reputation score** is computed as `min(correctVotes / totalVotes × 100, 100)`. A validator starts at 0% reputation and builds it through correct voting. Incorrect votes carry a much steeper penalty (-50 reputation points per wrong vote) than rewards (+1 per correct vote), creating a strong asymmetric incentive toward careful, correct validation.

**[IMPLEMENTED]** `Account` and `Validator` in `HYDRON.Models` fully implement the account and validator models, including all state mutations, thread safety, state hashing, staking, penalties, reward receipt, vote recording, and status transitions. `ValidatorRank` is implemented as an immutable record capturing the ranking snapshot.

---

## 7. The Transaction Lifecycle

A transaction in HYDRON is the atomic unit of value transfer. It carries a sender address, receiver address, amount, fee, nonce, sender Ed25519 signature, and optionally a receiver signature and ephemeral X25519 public key for private transactions. The transaction proceeds through a well-defined state machine with strict transition rules.

### State Machine

A transaction begins in the `InitiatedBySender` state the moment it is constructed and signed by the sender. From there it has two possible immediate paths.

If the transaction requires receiver confirmation (an opt-in feature allowing the receiver to accept or reject the payment before it enters consensus), it moves to `AwaitingReceiverAcceptance`. The receiver can accept — causing the transaction to advance to `PendingValidation` — or reject it, in which case it terminates in `AbortedByReceiver`. The sender can also abort at this stage (`AbortedBySender`), and the transaction can time out if the receiver does not respond within the allowed window (`TimedOut`).

If no receiver confirmation is required, the transaction moves directly from `InitiatedBySender` to `PendingValidation`, either after the sender triggers it or after the receiver signs if confirmation was required.

Once in `PendingValidation`, the transaction is live in the consensus process. Validators have been assigned and their count is frozen at the moment of this transition. The transaction can either reach `ConsensusReached` (if a supermajority of assigned validators approve) and then `Settled` (the terminal success state), or it can be `Rejected` (the terminal failure state).

`Settled`, `Rejected`, `AbortedBySender`, `AbortedByReceiver`, and `TimedOut` are all terminal states. Once a transaction enters any terminal state, it is finalized: `IsFinalized` is set to `true`, `FinalizedAt` is recorded, and no further mutations are possible. Only finalized transactions may be included in a `TransactionBlock`.

### Validator Assignment & Supermajority

Before a transaction enters `PendingValidation`, validators are assigned to it. The number of assigned validators and their identities are fixed at the moment the status transitions to `PendingValidation` — this is called freezing the validator count. The required number of approvals to reach consensus is `ceil(frozenCount × 2/3)`, the standard two-thirds supermajority threshold of BFT consensus.

Validators who submit votes but were not among the originally assigned set are tracked separately as unregistered validators. Their votes are recorded but do not count toward the supermajority threshold. This prevents late-joining validators from diluting consensus security.

**[IMPLEMENTED]** `Transaction` in `HYDRON.Models` implements the full state machine, validator assignment, validator count freezing, supermajority calculation, receiver signature handling, hash assignment, block number assignment, and finalization logic. All invalid state transitions throw exceptions.

---

## 8. The Validator System

Validators are the backbone of HYDRON's security and liveness. They receive transactions, validate them against the rules of the network, cast cryptographically signed votes, and collect them into blocks. Every action a validator takes is economically accounted for, with correct behavior rewarded and incorrect behavior slashed.

### Becoming a Validator

Any account can register as a validator by staking a minimum of 1 atomos and providing at least one network endpoint (IPv4 address, IPv6 address, or DNS name). The staked amount is the validator's skin in the game: it is the capital at risk during consensus participation, and it also determines the validator's voting weight.

### The Validation Record

When a validator votes on a transaction, it produces a `Validation` record. This record carries a time-ordered UUID v7 identifier, the hash of the transaction being voted on, the validator's address, and an Ed25519 signature over the validation data. The validator must sign the validation before casting a confirm or reject decision. The speed at which the validator responds (measured in milliseconds) is recorded in the validation record and factors into the validator's future `ValidatorRank` score.

### Economic Security: Rewards and Slashing

The penalty regime is asymmetric by design. A validator that approves an invalid transaction is slashed 100% of the transaction amount from its stake. A validator that rejects a valid transaction is slashed 1% of the transaction amount. This asymmetry reflects the relative severity of the two failure modes: approving a fraudulent transfer is categorically more damaging to network integrity than incorrectly blocking a legitimate one.

On the reputation side, every correct vote earns +1 reputation point, while every wrong vote costs -50 reputation points. The steep penalty for wrong votes is intentional: it means that a validator cannot afford to frequently cast incorrect votes even if each individual error seems minor.

Stake can never drop below zero — penalties are clamped to the available staked amount. A validator whose stake is fully depleted by penalties enters the `Penalized` status and cannot receive rewards or vote until it stakes again.

### Validator Status Transitions

A validator's `Status` field reflects its current operational state. An `Active` validator participates in consensus normally. A validator that repeatedly votes incorrectly or violates network rules can be `Warned` and then `Suspended`. A validator that becomes unreachable on the network is marked `Unreachable` and excluded from further consensus assignments until it reconnects. A validator whose entire stake has been slashed is `Penalized` — it can neither vote nor earn rewards until it re-stakes.

**[IMPLEMENTED]** `Validator` and `Validation` in `HYDRON.Models` implement the full validator model, validation record lifecycle, penalty application, reward receipt, status transitions, vote recording, and duplicate-validation prevention.

---

## 9. Consensus Mechanism

HYDRON uses a delegated supermajority BFT consensus protocol. It is not proof-of-work and not proof-of-stake in the conventional sense — validators do not compete to produce blocks through computational effort or through a lottery weighted by stake. Instead, validators are assigned to specific transactions and required to cast signed votes within a bounded time window. 
**[PLANNED]** This window is adaptive rather than fixed: it is calculated per validator, per StateBlock epoch, as the maximum of (a) the 99th-percentile transaction processing time across the entire network over the last 100 finalized StateBlocks, and (b) that specific validator's own 99th-percentile processing time over the same window. Processing time is measured as the elapsed time between a validator receiving a transaction and submitting its signed vote, corresponding to the ValidationSpeedMs field already present in the Validation model. New validators without 100 StateBlocks of personal history default to the network-wide P99 alone until sufficient history accumulates.

### Adaptive Timeout & Graceful Degradation

Each validator's response window 𝑇 is computed as described in Section **[16. Validator Response Timeout]**. Rather than treating 𝑇 as a hard cutoff, HYDRON applies a three-phase degradation model to absorb transient network disturbances:

- Normal window (0 to T): The assigned validator submits its vote as expected.

- Grace window (T to 100T): The assigned validator has not yet been dropped and may still submit its vote. Concurrently, previously unassigned validators may submit votes that provisionally backfill the slow validator's slot. If the original assignee's vote arrives at any point before 100T, it always takes priority over any backfilled vote for that slot, regardless of arrival order — the backfilled vote is discarded and the reward for that slot is attributed to the rightful assignee.

- Hard drop (after 100T): Any assigned validator still silent at this point is permanently excluded from the transaction's consensus batch. Whatever vote currently occupies that slot — a backfilled unassigned vote, or nothing — becomes final.

Throughout all three phases, the frozen validator count 𝑁 and the required supermajority threshold ceil(𝑁 × 2/3) never change. Backfilling substitutes who fills a slot; it never resizes the consensus pool itself.

### Transaction-Level Consensus

Consensus in HYDRON operates at the transaction level, not the block level. Each transaction is independently validated and achieves its own finality verdict. A transaction is confirmed when `ceil(N × 2/3)` of its assigned validators submit `Confirm` votes, where N is the frozen validator count at the time `PendingValidation` was entered.

The **first validator** in a transaction's assigned list holds a special role. This validator is the one that first received the transaction from the sender and is responsible for initiating the consensus round by broadcasting it to the other assigned validators. The first validator also receives the transaction fee as a direct reward. A planned feature — not yet implemented — gives the first validator a veto: the ability to unilaterally reject a transaction before it even enters the consensus round, acting as a first-pass filter. The exact mechanics of this veto are an open design question (see Section 16).

### Block-Level Finality

While individual transaction outcomes are determined through transaction-level consensus, block-level finality provides a second layer of commitment. A `TransactionBlock` is finalized when a two-thirds supermajority of the active validator set endorses it. Once finalized, a block's contents cannot be altered. After 100 StateBlocks have elapsed past a finalized block, that block enters permanent immutability — no reorganization of any kind is possible, and the block's data can be safely treated as permanently settled history.

**[PLANNED]** The consensus voting engine, first-validator veto logic, vote aggregation service, and block-level finality tracking are not yet implemented. The data models that consensus will operate on are implemented.

---

## 10. Block Architecture

HYDRON organizes its transaction history into a two-level block hierarchy designed to separate the high-frequency concerns of transaction processing from the lower-frequency concerns of state settlement.

### TransactionBlock

A `TransactionBlock` (conceptually also called a ValidatorBlock) is the primary unit of the chain. It is produced by a single validator and contains up to 100 finalized transactions. Each block carries a block number, a SHA-256 hash of its own contents, the hash of the previous block (forming the chain), a Merkle root over all included transaction hashes, a state root over all affected account states, the producing validator's address, and a creation timestamp.

The block lifecycle is strictly controlled. Transactions can only be added before the block is sealed. The Merkle root and state root can each be set exactly once. When the block is ready, it is sealed with a single call that sets the block hash and locks all mutations permanently. A sealed block that passes the `IsValid()` check has confirmed: it is sealed, non-empty, all transactions are finalized and hashed, and all three root hashes are present.

### StateBlock

A `StateBlock` is a higher-order block that aggregates 100 `TransactionBlock`s. It serves as the settlement layer of the network. When a StateBlock is finalized, the electricity price oracle is updated through validator consensus, validator rewards for the entire epoch are distributed, and the full account state of the network is checkpointed. The state root embedded in a StateBlock is a hash over the state hashes of every account on the network at that point.

The 100-block immutability window is counted in StateBlocks. After 100 StateBlocks have been finalized on top of a given StateBlock, that StateBlock and all the TransactionBlocks it contains become permanently immutable.

**[IMPLEMENTED]** `TransactionBlock` in `HYDRON.Models` implements the full TransactionBlock lifecycle with thread-safe mutations, sealing, Merkle root and state root management, and validity checking.

**[PLANNED]** The `StateBlock` model does not yet exist in the codebase. The service that creates TransactionBlocks, the Merkle tree calculation logic, state root computation, and all block-level finality tracking are also not yet implemented.

---

## 11. Rewards, Fees & Monetary Issuance

HYDRON's monetary issuance is entirely driven by validator activity. There is no genesis allocation, no pre-mine, and no inflationary schedule divorced from network usage. New atomos are created only when validators do work.

### Reward Schedule

Three tiers of rewards exist, corresponding to the three levels of work validators perform:

- **Per-transaction reward:** Every transaction that is successfully validated and settled earns the validating validators a reward of **1 HYA (100 atomos)**.
- **Per-TransactionBlock reward:** The validator that proposes and seals a TransactionBlock earns **1 HYB (10,000 atomos)**.
- **Per-StateBlock reward:** The settlement of a StateBlock triggers an issuance of **1 HYG (100,000,000 atomos)**, distributed to the validators responsible for the epoch.

Rewards are split between Core and Edge tier validators at rates that reflect their respective contributions. Core validators, who bear greater responsibility and are held to a higher standard, receive a larger share of block rewards. The exact split ratio is a planned design decision.

### Fees

Transaction fees are distinct from minted rewards. They are not new issuance — they are a transfer from sender to validator. The minimum fee for any transaction is **1 HYD (10¹⁶ atomos)**. The fee goes entirely to the first validator assigned to the transaction, compensating that validator for the cost of receiving and broadcasting the transaction to other validators.

Senders may pay more than the minimum fee. A higher fee increases the transaction's `Priority` (Low, Medium, High, or Urgent), which signals to the mempool and validators that the transaction should be processed preferentially. The fee market is therefore dynamic: during periods of high network activity, senders must compete on fees.

### What Is and Is Not Minted

The total amount of new atomos created by a block reward is `TotalCoreBlockReward + TotalEdgeBlockReward + TotalValidationReward`. Transaction fees are explicitly excluded from this sum because they represent value that already existed on the network — they are redistribution, not creation. This distinction is captured in the `BlockReward` model's `TotalMinted` field.

**[IMPLEMENTED]** `BlockReward` and `ValidatorReward` in `HYDRON.Models` implement the reward snapshot models with all the totals described above, plus a `Settle()` method to mark a reward as distributed. The calculation and distribution logic that populates these models is not yet implemented.

---

## 12. The Electricity Price Oracle

The oracle is the most novel and most critical component of HYDRON's monetary system. It is the mechanism by which a physical constant — the ionization energy of hydrogen — is connected to a real-world USD price.

### The Problem

The atomos price formula requires a real-time, trustworthy, manipulation-resistant electricity price in USD per electronvolt. No single data source can be trusted to provide this without introducing a centralized point of failure or manipulation. The solution is to make the electricity price a consensus output of the validator network.

### The Formula Chain

The conversion from a human-readable electricity price to the atomos USD price proceeds as follows:

```
Step 1: Collect USD/kWh prices from EIA, IEA, Eurostat, and World Bank data feeds.
Step 2: Weight each regional price by that region's share of world population,
        producing a single population-weighted global average USD/kWh.
Step 3: Convert to USD/J:
        USD/J = USD/kWh ÷ 3,600,000
Step 4: Convert to USD/eV:
        USD/eV = USD/J ÷ 1.602176634 × 10⁻¹⁹
Step 5: Compute atomos price:
        atomos_USD = 13.6 × USD/eV
```

### Consensus Voting

Each validator independently computes the electricity price using the above data sources and submits its result to the network at the end of each StateBlock epoch. A two-thirds supermajority of validators must agree on the price (within an agreed tolerance band) for it to be accepted. The accepted price is embedded in the StateBlock, becoming part of the immutable historical record. This means anyone can look at any StateBlock and know exactly what the atomos was worth at that point in time, with full auditability of the oracle inputs.

The population-weighting is essential. Without it, a validator cartel located in a region with artificially cheap electricity could drive down the global average price, deflating the atomos and benefiting those validators at the expense of the rest of the network. Population weighting ensures that no single region, regardless of its electricity market, can disproportionately influence the oracle outcome.

**[PLANNED]** The oracle system, including EIA/IEA/Eurostat/World Bank integrations, the population-weighted averaging algorithm, validator voting for the oracle price, and the embedding of the result in StateBlocks, is not yet implemented.

---

## 13. The Privacy Model

HYDRON supports three privacy modes for transactions, allowing participants to choose the appropriate level of confidentiality for each transfer.

**Public mode** is the default. The sender address, receiver address, and amount are all visible on the chain to any observer. This is appropriate for transparent, auditable transfers.

**HiddenReceiver mode** uses stealth addresses to obscure the receiver's identity. The sender generates a one-time ephemeral X25519 key pair, computes a shared secret with the recipient's stealth public key using Diffie-Hellman key exchange, and derives a one-time stealth address from that shared secret using HKDF-SHA256. The transaction is addressed to this one-time address rather than the recipient's public address. An observer cannot link the stealth address back to the recipient's public identity without knowing the recipient's X25519 private key. The ephemeral public key is included in the transaction so the recipient can scan for incoming stealth payments.

**FullyPrivate mode** extends HiddenReceiver with additional metadata obfuscation. The exact additional protections at this level are a planned design decision.

Recipients scan for incoming stealth payments by re-deriving the shared secret using their X25519 private key and the ephemeral public key broadcast in the transaction, then checking whether the resulting stealth address matches the transaction's receiver field. This scanning is done locally by the recipient's wallet and does not require revealing the X25519 private key to the network.

Stealth keys are rotatable. A validator or user can replace their X25519 stealth public key at any time by calling `RotateStealthKeyPair()`, which generates a new X25519 key pair and broadcasts the new stealth public key to the network. This is useful for unlinking historical and future stealth payment activity.

**[IMPLEMENTED]** All cryptographic primitives for the privacy model are implemented in `KeySafe`: ephemeral key generation, ECDH shared secret computation, stealth address derivation, stealth payment scanning, spend key derivation, and stealth key rotation. The `PrivacyMode` enum and `EphemeralPublicKey` field are present in `Transaction`. The network-level broadcasting of stealth payments and the scanning infrastructure are planned.

---

## 14. Network & RPC Layer

The network and API layers are the interfaces through which external participants interact with HYDRON. Both layers are entirely in the planned stage; the domain models that they will serve are implemented, but the transport and API code is not.

### P2P Network

The peer-to-peer network will use TCP sockets for all communication. Peer discovery will be implemented via a Distributed Hash Table (DHT), allowing new nodes to find the network without a centralized directory. Once connected, message propagation will use a gossip protocol to efficiently broadcast transactions, consensus votes, and finalized blocks to all network participants.

Three primary message types will traverse the network: transaction broadcasts (when a user submits a new transaction), consensus vote broadcasts (when validators cast their votes), and block broadcasts (when a validator seals a TransactionBlock or a StateBlock is finalized).

The network layer will include connection timeout management, automatic retry logic, dead peer detection and removal, and partition recovery handling.

### RPC API

The RPC interface will expose the following methods to external clients (wallets, explorers, applications):

| Method | Description |
|--------|-------------|
| `suggest_validator()` | Returns the address of the best currently active validator for transaction submission, ranked by ValidatorRank |
| `wallet_create()` | Generates a new HD wallet (KeySafe) and returns the public address and stealth public key |
| `transfer()` | Submits a signed transaction to the network |
| `get_balance()` | Returns the current atomos balance of an address |
| `get_transaction()` | Returns the current state of a transaction by its hash |
| `become_validator()` | Registers an account as a validator with a stake and network endpoint |
| `get_electricity_price()` | Returns the current oracle-determined electricity price and derived atomos USD value |
| `get_electricity_price_history()` | Returns historical oracle prices by StateBlock number |
| `get_validator_info()` | Returns full details of a validator including tier, reputation, and stake |
| `get_network_stats()` | Returns current TPS, active validator count, and peer count |

**[PLANNED]** All network and RPC code lives in `HYDRON.Network` and `HYDRON.Connectivity` respectively. Both projects exist in the solution but contain no implementation as of July 2026.

---

## 15. Implementation State

The codebase is organized as a .NET solution with seven projects. As of July 24, 2026, meaningful implementation exists only in `HYDRON.Models`. All other projects are either stubs or empty shells.

```
HYDRON/
├── src/
│   ├── HYDRON.Models/       ← IMPLEMENTED (all domain models)
│   ├── HYDRON.Core/         ← STUB (Program.cs entry point only)
│   ├── HYDRON.Cryptography/ ← EMPTY
│   ├── HYDRON.Database/     ← EMPTY
│   ├── HYDRON.Network/      ← EMPTY
│   ├── HYDRON.Connectivity/ ← EMPTY
│   └── HYDRON.Validator/    ← EMPTY
├── tests/                   ← EMPTY
└── docs/
    └── HYDRON_MASTER_DOCUMENTATION.md  ← This file
```

### What Is Fully Implemented

Every domain model in `HYDRON.Models` is complete and production-quality in terms of its own internal logic:

- **`Atomos`** — The physics-pegged currency value type with full arithmetic, denomination conversion, and BigInteger backing.
- **`Account`** — Thread-safe account state with balance, nonce, handle, stealth key, and lazily-computed state hash.
- **`Validator`** — Full validator model including staking, penalties, rewards, status transitions, vote accounting, tier management, and network endpoint validation.
- **`Transaction`** — Complete transaction lifecycle with a strict state machine, validator assignment and freezing, supermajority threshold calculation, privacy mode support, and finalization.
- **`Validation`** — Per-validator vote record with signing, confirmation/rejection, speed tracking, reward assignment, and penalty recording.
- **`TransactionBlock`** — Thread-safe block container with transaction addition, Merkle/state root management, and permanent sealing.
- **`BlockReward` / `ValidatorReward`** — Reward snapshot models for block-level and per-validator reward accounting.
- **`KeySafe`** — Complete wallet cryptographic primitive with Ed25519 signing, X25519 stealth payments, HD derivation, and secure disposal.
- **`ValidatorRank`** — Immutable ranking snapshot record.
- **`Enumerators`** — All system enums (Denominations, Priority, TransactionStatus, ValidationStatus, ValidatorStatus, ValidatorTier, RewardStatus, PrivacyMode).

### What Remains To Be Built

The following major systems have no implementation yet and represent the bulk of future work:

1. **Persistence layer** (`HYDRON.Database`) — RocksDB integration, repository interfaces and implementations for accounts, transactions, validators, and blocks, and a JSON serialization codec.
2. **Service layer** (`HYDRON.Core` / `HYDRON.Validator`) — Transaction processing pipeline, signature verification, balance and nonce validation, double-spend prevention, consensus voting engine, first-validator veto, block assembly service, reward distribution, and the mempool.
3. **Oracle** — Electricity price data integrations, population-weighted averaging, and consensus voting for the oracle price.
4. **P2P Network** (`HYDRON.Network`) — TCP socket management, DHT peer discovery, gossip protocol, and message broadcasting.
5. **RPC API** (`HYDRON.Connectivity`) — The full JSON-RPC or REST surface.
6. **Tests** — Unit, integration, and end-to-end test suites.
7. **Deployment** — Docker images, multi-environment configurations, mainnet/testnet/dev configs.

---

## 16. Open Design Questions

The following design decisions are unresolved. They should be settled before implementation of the relevant system begins.

**First-validator veto mechanics.** The specification states that the first validator has veto power. Does a first-validator rejection immediately kill the transaction, bypassing the normal consensus round? Or does it count as one reject vote within the standard supermajority calculation? The answer has significant implications for network liveness and the economic value of the first-validator slot.

**Minimum fee enforcement.** The minimum transaction fee of 1 HYD is a stated rule but is not enforced in the current `Transaction` constructor, which accepts any `Fee` value including zero. A guard must be added to the constructor or to the transaction processing service.

**ValidatorRank scoring formula.** The `ValidatorRank` record captures five input dimensions (staked amount, average validation speed, activity count, normalized reputation, blocks observed) and a composite `FinalRank` output. The weights assigned to each dimension and the normalization method have not been specified. This formula must be defined before the validator assignment algorithm can be implemented.

**Reward multipliers.** The design mentions reputation-based reward multipliers, but neither the thresholds nor the formula for computing the multiplier have been defined.

**StateBlock model.** The `StateBlock` class does not yet exist in the codebase. Its fields, construction logic, and relationship to the electricity price oracle need to be specified and implemented.

**Mempool design.** The mempool (the queue of unconfirmed transactions awaiting validator assignment) has no specification yet: its capacity limit, eviction policy (FIFO vs. fee-prioritized), and the algorithm by which transactions are dequeued and assigned to validators are all undefined.

**Reward issuance timing.** It is not yet decided whether per-transaction and per-TransactionBlock rewards are issued immediately when each unit is settled, or whether they are batched and distributed only at StateBlock finalization. This decision affects both the reward model implementation and the frequency of monetary issuance events.

**Commission rate application.** The `CommissionRate` field exists on `Validator` but its semantics are undefined. If validators have delegators who stake on their behalf, the commission rate would govern the split between the validator and its delegators. Whether HYDRON supports delegation at all is not yet decided.

**FullyPrivate mode details.** The `FullyPrivate` privacy mode is defined in the enum but its additional protections beyond `HiddenReceiver` mode are not yet specified.

**Canonical block naming.** The task tracker uses the term `ValidatorBlock` while the codebase uses `TransactionBlock`. A canonical name should be chosen and applied consistently across all documentation, code, and future discussions.
