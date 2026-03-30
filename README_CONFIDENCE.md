# Subscription Confidence Logic

This document explains how the app computes confidence for recurring charges and subscriptions.

## Two different confidence values

The system stores two related but different scores:

- `PatternConfidenceScore` (from recurring pattern detection)
- `SubscriptionConfidenceScore` (from classification rules + pattern score)

Both are integers in the range `0..100`.

## 1) Pattern confidence (`PatternConfidenceScore`)

Computed in `RecurringDetectionService.TryDetect(...)`.

### Preconditions for detection

A group is considered only if:

- at least 2 debit transactions exist for the group
- amounts are compatible: each amount must be within `8%` of the group scale
- cadence label is inferred from median day-gap when possible:
  - Weekly: `5..10`
  - Monthly: `25..35`
  - Quarterly: `80..105`
  - Yearly: `300..400`
  - otherwise label is `Unknown` (still treated as recurring if other checks pass)

If amount compatibility fails (or there is only one row), no detection result is produced for that group.

### Formula

When detection passes:

`PatternConfidence = min(100, 40 + (transactionCount * 15) + cadenceConsistencyBonus)`

Where:

- `cadenceConsistencyBonus = 20` if all gaps are close to median gap
- otherwise `cadenceConsistencyBonus = 0`
- gap similarity check: `abs(gap - median) <= max(3, median / 10)`

### Examples

- 2 matching monthly charges, consistent gaps: `40 + 30 + 20 = 90`
- 3 matching charges, inconsistent gaps: `40 + 45 + 0 = 85`

### Flexible cadence behavior

Recurring rows are no longer discarded just because cadence bucket is `Unknown`.

- For standard buckets, next expected date uses fixed step (weekly/monthly/quarterly/yearly).
- For `Unknown`, next expected date is estimated as:  
  `lastChargeDate + medianGapDays`

This allows bi-monthly or otherwise non-standard recurring charges to still appear in subscriptions/review.

## 2) Subscription confidence (`SubscriptionConfidenceScore`)

Computed in `RecurringClassifier.Classify(...)` using merchant text and normalization hints.

### Rule families

- **Alias hints** (high-confidence normalization hint)
  - Software: `CombineScores(90, patternConfidence)`
  - Media: `CombineScores(88, patternConfidence)`
  - Utility/Telecom/Transfer/etc.: fixed low scores (e.g., 14/20/12)
- **Keyword matches**
  - Software keywords: `CombineScores(88, patternConfidence)`
  - Media keywords: `CombineScores(85, patternConfidence)`
  - Salary/Transfer/Utility/Rent/etc.: fixed low scores
- **Fallback (UnknownRecurring)**
  - `clamp(int(patternConfidence * 0.55 + 20), 0, 100)`

### `CombineScores` function

`CombineScores(keywordFloor, patternConfidence) = clamp(max(keywordFloor, (keywordFloor + patternConfidence)/2), 0, 100)`

Interpretation:

- keeps a strong keyword floor
- allows strong pattern confidence to raise the final score
- never drops below the keyword floor

## 3) How scores drive routing

After classification, each detected group is routed:

- **Subscription row**: `IsLikelySubscriptionRow(type, score)`  
  current rule: score `>= 70` and type in `{SoftwareSubscription, MediaSubscription, UnknownRecurring}`
- **Recurring review candidate**: score `40..69` for Software/Media/Unknown
- **Non-subscription recurring candidate**: Utility, Telecom, Salary, Transfer, etc.

## 4) Why UI filtering can hide high scores

The Subscriptions page has two modes:

- **All active subscriptions**: includes all active rows from `subscriptions`
- **Software & media only (confidence >= 70)**: strict filter
  - type must be `SoftwareSubscription` or `MediaSubscription`
  - score must be `>= 70`

So a row with score `75` and type `UnknownRecurring` appears in **All**, but is excluded in **Software & media only**.

## 5) Duplicate detection logic (alerts + dashboard exposure)

Duplicate detection does not look at raw transactions directly. It works from active subscription rows.

### Step A: eligibility filter

A subscription is eligible for duplicate analysis only if:

- `SubscriptionConfidenceScore >= 45`
- `RecurringType` is **not** one of:
  - `Salary`
  - `Transfer`
  - `Rent`
  - `RecurringIncome`

This is implemented in `RecurringClassifier.IncludedInDuplicateDetection(...)`.

### Step B: grouping key

Eligible subscriptions are grouped by merchant key:

- use `NormalizedMerchant` when present
- otherwise fallback to normalized `VendorName` (lowercased, whitespace-collapsed)

A group is treated as duplicate-like only when it has **more than one** row.

### Step C: alert generation behavior

For every subscription in each duplicate group, the alert job creates a `DuplicateTool` alert with:

- severity: `Info`
- status: `Open`
- message: "Multiple recurring charges detected for a similar vendor name."

So a duplicate group of size 3 creates 3 duplicate alerts (one per row).

### Step D: dashboard exposure math

`Duplicate-tool exposure (est.)` is derived from current active subscriptions using the same eligibility/grouping logic.

For each row inside duplicate groups, amount is normalized to monthly and summed:

- Weekly: `amount * 52 / 12`
- Monthly: `amount`
- Quarterly: `amount / 3`
- Yearly: `amount / 12`
- Unknown cadence: `amount` (current fallback)

If no group has more than one row, exposure is `0`.

### Practical implication

You can have many subscriptions and still get duplicate exposure `0` if each merchant key appears only once.
The metric is intentionally "possible overlap for similar vendor keys", not total subscription spend.

## 6) Related thresholds used elsewhere

- Duplicate-tool eligibility: score `>= 45` and excludes salary/transfer/rent/recurring income
- Alert eligibility:
  - Software/Media: score `>= 45`
  - UnknownRecurring: score `>= 60`

## 7) Important note about imports

Recurring detection runs on **debits only**. If imported data marks spend as credits, pattern confidence will be zero because no debit groups are formed.
