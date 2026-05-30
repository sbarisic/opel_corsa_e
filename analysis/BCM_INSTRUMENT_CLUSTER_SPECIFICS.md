# BCM Instrument Cluster Specifics

This note narrows the BCM disassembly to the frames most likely to matter for
instrument-cluster wake, body state, display state, and prerequisite status. It
does not cover immobilizer/security behavior or firmware patching.

## Summary

The useful instrument-cluster evidence now splits into two families:

- Direct IPC bench traffic is low-speed 29-bit GMLAN-like traffic from source
  `0x60`, for example `13FFE060`, `10244060`, `10424060`, and `10ACE060`.
  Those frames project into early BCM Group A object IDs, but they are IPC-origin
  status/capability traffic on the bench.
- The older standard 11-bit BCM/body candidates are real Group A transmit
  objects with payload RAM buffers and aux payload builders. They look like
  BCM-to-body/IPC prerequisite frames, not simple high-speed ECU pass-through.

The strongest firmware-backed IPC prerequisite set remains:

```text
0x13FFE040#                 fake BCM/source presence form of IPC keepalive
0x100#                      SWCAN/network wake object
0x621#0040000000000000      network-management keepawake candidate
0x1F1                       dense ignition/environment/status bitfield
0x160                       compact power/body state
0x0F1                       high-rate body sync/state
0x12A                       door/belt/body/DIC status-style frame
0x135/0x137/0x139           body/display/config status cluster
0x451                       display/body status
0x4E1/0x514                 config/VIN/range-style periodic data
```

The direct IPC startup/status families are now also available as a live bench
profile named `ipc-bcm-live-object-probe`. It waits for the first IPC RX frame,
uses the quiet BCM/source presence baseline, and then sends source-`0x40`
variants of the observed source-`0x60` IPC startup families:

```text
13FFE040#                       presence / keepalive
10244040#02 / #06               state/status family projected to Group A 0x409
10424040#059964 / #079964       status/config family, exact Group A row exists
1045C040#44 / #40               one-byte state family
10ACE040#...                    boot/status family projected to Group A 0x42B
10AE8040#...                    boot/status family projected to Group A 0x42B
10AFC040#38FFFFFFFFFFFF2B       full-state boot/status payload
10B0A040#001E04 / #002B04       short status family
10600040#01609370015B00         config/range payload seen once from IPC
10774040#00, 1084A040#00        one-byte status families
```

This is intentionally different from the older mixed default-bench profile: it
does not send `100#` or the `621#0140...` initiate burst, because the diagnostic
AA00 run showed that startup burst can make the IPC emit a fresh boot/status
sequence before any diagnostic request is sent.

None of the standard builders currently looks like a direct speed/RPM gauge
payload. The likely next static target is still the receive side: trace Group B
ECU frames `0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`, and `0x3D1` into RAM state, then
find which Group A builders consume that state.

## IPC Diagnostic Bench Findings

The standalone IPC responds on the expected low-speed diagnostic pair
`0x24C` request / `0x64C` response. The useful accepted classic GMLAN request
shape is:

```text
24C#021003AAAAAAAAAA -> 64C#0150AAAAAAAAAAAA   diagnostic session/init accepted
24C#013EAAAAAAAAAAAA -> 64C#017EAAAAAAAAAAAA   tester-present accepted
24C#023E80AAAAAAAAAA -> 64C#037F3E12AAAAAAAA   suppress-positive tester rejected
```

`24C#021A90AAAAAAAAAA` returns a multi-frame VIN response:

```text
64C#10135A9057304C30
24C#300000AAAAAAAAAA   old auto ISO-TP flow control, zero STmin
64C#2158455036384734
64C#22313034353435AA

decoded VIN: W0L0XEP68G4104545
```

Later live testing showed that zero-STmin flow control can be too aggressive on
this 33.3 kbit/s bench. The 22:02 confirmed-identity run received the first
frame and then only `64C#22...`, missing consecutive frame sequence `21`.
`analyze-diag` now flags this as incomplete instead of silently decoding a
short VIN:

```text
64C#10135A9057304C30
24C#300000AAAAAAAAAA
64C#223130343435AA
=> ISO-TP incomplete got=13/19, missing_cf_seq=1
```

The automatic flow-control payload has therefore been changed to
`24C#30000AAAAAAAAAAA`, requesting STmin `0x0A` while keeping blocksize zero.

The `AA 00` packet-ID request is a repeatable anomaly:

```text
24C#02AA00AAAAAAAAAA -> 54C#0000000000000000
```

Ten repeated `AA 00` requests all returned the same `54C` all-zero frame with
about 30-32 ms latency. `AA 01` through `AA 0F` returned
`64C#037FAA12AAAAAAAA`, meaning subfunction not supported. This makes `54C` a
real IPC diagnostic/status response path, but not yet decoded.

Important bench behavior: the IPC boot/status burst happened immediately after
the earlier diagnostic startup baseline (`100#`, `13FFE040#`,
`621#0140000000000000`, `621#0040000000000000`) and several seconds before
`AA 00` was first sent. Diagnostic profiles in `cantool` now use a quiet
post-RX baseline with only `13FFE040#` and `621#0040000000000000` at 1 Hz to
avoid causing that restart-like burst.

The first isolated restart test narrowed this further. In
`codex_ipc_restart_wake_only_20260529_02.candump`, `cantool` waited for the IPC
to transmit `10244060#02`, then sent exactly one `100#`. That did not produce a
full boot/status burst within the 500 ms analyzer window:

```text
100# -> no IPC boot/status burst in analyze-restart
```

It did, however, line up with `62C#0140000000000000` and normal steady-state
frames shortly afterward. A later listen-only follow-up capture,
`codex_post_manual_100_capture_20260529.candump`, saw a fresh startup sequence
without any `cantool` TX comments in that capture window:

```text
13FFE060#
100#
62C#0140000000000000
10244060#02
103BC060#00
103D6060#00
10424060#059964
...
```

Because that follow-up was listen-only, the observed `100#` was on the bus, not
emitted by the capture command. The current working model is therefore:

- `100#` alone is not proven to cause the restart-like boot burst.
- `100#` is still a real wake/network-management participant and may provoke or
  precede `62C#0140...`.
- The hard startup bundle remains suspicious because the original AA00 repeat
  log showed the full boot burst about 27-28 ms after the bundled TX frames.
- The next isolation target is still `621#0140000000000000` alone, while the
  IPC is already transmitting steady frames.

Follow-up testing confirmed `621#0140000000000000` as the stronger restart /
boot-sequence trigger. The profile `ipc-restart-wake-then-nm-init-probe` sent
`100#` once, waited five seconds, then sent three delayed
`621#0140000000000000` frames. The refined `analyze-restart` command treats a
real boot burst as at least three unique IPC boot/status IDs within the analysis
window. On `codex_ipc_restart_wake_then_nm_init_20260529.candump`, the result
was:

```text
100#                 weak match only: one steady 10424060 frame at ~395 ms
621#0140... send 1  boot/status burst: 16 frames / 14 unique IDs, ~13 ms
621#0140... send 2  boot/status burst: 16 frames / 14 unique IDs, ~9 ms
621#0140... send 3  boot/status burst: 17 frames / 15 unique IDs, ~8 ms
```

This strongly supports:

- `100#` is a wake/network-management participant but not the full restart
  trigger by itself.
- Repeating `621#0140000000000000` causes the IPC to re-emit the boot/status
  family (`10244060`, `103BC060`, `10448060`, `1045C060`, `10ACE060`,
  `10AE8060`, `10AFC060`, etc.).
- Future diagnostic and gauge profiles should avoid periodic or repeated
  `621#0140...` unless intentionally trying to force an IPC startup/status
  refresh.

The classic `0x1A` scan found these positive identifiers:

```text
1A90 -> VIN W0L0XEP68G4104545
1A98 -> "*16SYPD"
1A99 -> 20 15 12 16
1A9A -> 0A 5F
1A9B -> 00 08
1A9C -> 02 53 FD 0F
1A9D -> "AA"
1A9F -> "*16SYPD   " plus 0xFF padding
1AA0 -> 00
1AA1 -> 1F 11 F0 FF FF FF 7F 00 00 00 00 00
1AA2 -> FF F7 CF 0D 00 00
1AA3 -> A0 04 23 00 00 40 07 00
1AA4 -> 08 30 00 02 00
1AA5 -> 09 09 00 00 A3 00 12 10 68 0A 00 F9 87 FE 05 00 03
1AA6 -> E0 A8 DA 40 10 E1 3F
1AA7 -> 00
1AA8 -> 00
1AB0 -> 60
1AB2 -> FF FF FF FF FF
1AB4 -> "506369FG1F33605G"
```

Use `cantool analyze-diag --log <path>` after each diagnostic run to pair
`24C` requests with `64C` / `54C` responses and reconstruct multi-frame
responses. The next live diagnostic run should repeat
`ipc-diagnostic-aa00-repeat-probe` with the quiet baseline and confirm whether
the IPC still avoids the boot/status burst.

## ECU Receive Breadcrumbs

The priority ECU frames are confirmed in Group B. The current generated
dispatch/wrapper table also exposes wrapper entries for all five priority
indices, including the high-rate `0x0C9` path. These wrappers are not simple
Group A payload builders, but they are now concrete static anchors for tracing
the ECU RX path into object-manager state.

| ID | Group B index | Raw | Observed rate | Current status |
|---:|---:|---:|---:|---|
| `0x0C9` | 145 | `0x03240000` | about 71 Hz | wrapper `0x131BBC`, object key `0x9054` |
| `0x3D1` | 97 | `0x0F440000` | about 20 Hz | wrapper `0x131CDA`, object key `0x9014` |
| `0x3E9` | 95 | `0x0FA40000` | about 20 Hz | wrapper `0x13142E`, object key `0x9012` |
| `0x4C1` | 92 | `0x13040000` | about 20 Hz | wrapper `0x131D42`, object key `0x900D` |
| `0x4D1` | 90 | `0x13440000` | about 20 Hz | wrapper `0x131D5C`, object key `0x900B` |

## Group B Object-Manager Hooks

The receive path is now more constrained than a raw interrupt search. Two
wrapper families matter:

1. The generated dispatch/wrapper table at `0x021A40` maps Group B-style
   wrapper keys to handler functions:

```text
Group B index 90  / 0x4D1 -> key 0x900B -> handler 0x131D5C
Group B index 92  / 0x4C1 -> key 0x900D -> handler 0x131D42
Group B index 95  / 0x3E9 -> key 0x9012 -> handler 0x13142E
Group B index 97  / 0x3D1 -> key 0x9014 -> handler 0x131CDA
Group B index 145 / 0x0C9 -> key 0x9054 -> handler 0x131BBC
```

2. A lower wrapper region around `0x10BE88..0x10BED8` and
   `0x10C3B6..0x10C9F8` loads selected object IDs into `r7` and jumps into the
   generic object managers:

```text
0x10BE88 -> object 97 -> 0x12E5C4    likely Group B index 97 / 0x3D1
0x10BE9C -> object 95 -> 0x12E5C4    likely Group B index 95 / 0x3E9
0x10BEBC -> object 92 -> 0x12E5C4    likely Group B index 92 / 0x4C1
0x10BED4 -> object 90 -> 0x12E5C4    likely Group B index 90 / 0x4D1

0x10C3B6 -> object 97 -> 0x12E3D2
0x10C3BE -> object 97 -> 0x12E270
0x10C578 -> object 90 -> 0x12E3D2
```

The `0x0C9` Group B index is `145`. The generated dispatch table now has a
`0x9054 -> 0x131BBC` wrapper for it, but the 105-entry descriptor table below
still only covers object indices `0..104`. Nearby object `144` wrappers exist
in the lower wrapper region:

```text
0x10C7CC -> object 144 -> 0x12E270
0x10C9F4 -> object 144 -> 0x12E3D2
```

Those `144` wrappers are not evidence that object `144` is `0x0C9`. The safer
working model is now: `0x0C9` has a dispatch/wrapper entry, but likely uses a
different high-rate object-manager path or descriptor set than the four
in-range priority frames.

The generic managers break down as:

```text
0x12E270  dynamic 20-byte entry lookup by byte object ID
0x12E3D2  dynamic 20-byte entry lookup plus status gating/update
0x12E5C4  object registration/maintenance path keyed by byte object ID
0x12E118  12-byte selector lookup for 16-bit selectors such as 0x90A9
```

`0x12E5C4` is especially interesting because it loads a pointer from
`0x0001AF18`, calls `0x10D418`, and then dispatches based on message categories
`0x80`, `0xC0`, and `0xE0` after copying four bytes from the object payload. The
path reports errors/status through callback entries in the static table at
`0x18EAC`.

The object root at `0x03FFB4DC` is initialized by `0x12EB1E`. That routine
populates the root with several static and RAM table pointers:

```text
0x00020D98          root descriptor function table
0x03FFB4DC + 0x00 -> descriptor/callback table at 0x00020D98
0x03FFB4DC + 0x08 -> 105-entry object RAM at 0x03FFCD04
0x03FFB4DC + 0x0C -> static object descriptor table at 0x00023BAC
0x03FFB4DC + 0x10 -> 12-byte selector table at 0x00021854
0x03FFB4DC + 0x14 -> 16-entry dynamic table RAM at 0x03FFD920
0x03FFB4DC + 0x20 -> selector/cache RAM at 0x03FFB508
0x03FFB4DC + 0x1C -> static helper table at 0x00023274
```

During initialization, `0x12EB1E` walks 105 static 24-byte entries, copies
per-entry data into `0x03FFCD04`, and calls the helper callbacks from `0x266BC`.
The follow-up worker at `0x12EC8C` scans those 105 live entries, looks for active
entries (`byte +20 == 1`) with pending work (`byte +22 == 0`), and then invokes
the same helper/status callbacks. This is probably the object scheduler that
bridges decoded input state to application-level state updates.

The root descriptor function table at `0x20D98` starts with:

```text
0x0F05BC, 0x12EB1E, 0x12E4F0, 0x12EC8C,
0x12EACC, 0x12EC64
```

The valid 105-entry object descriptor table covers indices `0..104`, which
explains why Group B indices `90`, `92`, `95`, and `97` have clean object-manager
evidence while high-rate `0x0C9` at Group B index `145` does not. Dumped
descriptor entries for the four in-range priority frames:

```text
index 90 / 0x4D1 @ 0x02441C:
  0x00028283 -> 0x0017E8F8
  0x00048283 -> 0x0017E8F9
  0x00328550 -> 0x0017D098

index 92 / 0x4C1 @ 0x02444C:
  0x0001859E -> 0x0017C333
  0x0002859E -> 0x0017C334
  0x0004859E -> 0x0017C335

index 95 / 0x3E9 @ 0x024494:
  0x0039871F -> 0x0017C339
  0x00018855 -> 0x0017C33A
  0x00028855 -> 0x0017C33B

index 97 / 0x3D1 @ 0x0244C4:
  0x00028969 -> 0x0017E8FB
  0x00048969 -> 0x0017E8FC
  0x0001896A -> 0x0017CC81
```

The `0x0017xxxx` values here look like table/data references rather than V850
code pointers. They are now the best static anchors for finding what each
received ECU frame updates.

The callback table at `0x266BC` is now identified as an object-array helper
table. Its nearby handlers initialize and validate arrays of 8-byte records:

```text
0x13DA36  initialize object descriptor, table pointer 0x266BC
0x13DA64  clear seven halfword slots to 0xFFFF
0x13DA7E  parse 16-bit selectors and check against table around 0x21854
0x13D966  clear byte 4 for each 8-byte record
0x13D986  set byte 1 to 5 for each 8-byte record
0x13D9A8  clear byte 0 for each 8-byte record
0x13D9C8  scan record state bytes and reduce them to status 0/1/2/3/5
```

Practical consequence: the next manual disassembly pass should start at
`0x131BBC`, `0x131D5C`, `0x131D42`, `0x13142E`, and `0x131CDA`, then connect
those wrappers to `0x12E5C4`, `0x12E3D2`, `0x12E270`, and the dynamic entries
rooted at `0x03FFB4DC` / `0x03FFB4F8`. That is more likely to expose the true
ECU-RX decode callbacks than continuing to search for raw CAN ID constants.

### Group B Wrapper Byte-Writer Chains

Raw-byte decoding of the new wrapper targets shows they use the same compact
getter/writer block template as the IPC-facing payload builders:

```text
callt 9
save r6/r7 into r28/r29
zxb r29
jarl getter
restore r6/r7
move getter result into r8
jarl 0x12F124
zxb r10
callt 31
```

Every decoded block below calls writer `0x12F124`, which is the one-byte payload
writer already seen in Group A handler traces. The dispatch entries therefore
look like entry points into shared byte-list builders rather than independent
large receive handlers.

```text
0x0C9 / Group B 145 / handler 0x131BBC:
  0x131BBC -> getter 0x111E28 -> writer 0x12F124
  0x131BD6 -> getter 0x111E2E -> writer 0x12F124
  0x131BF0 -> getter 0x111E34 -> writer 0x12F124
  0x131C72 -> getter 0x1120E0 -> writer 0x12F124
  0x131C8C -> getter 0x11220A -> writer 0x12F124
  0x131CA6 -> getter 0x117B7A -> writer 0x12F124
  0x131CC0 -> getter 0x118140 -> writer 0x12F124
  0x131CDA -> getter 0x117D4E -> writer 0x12F124

0x3E9 / Group B 95 / handler 0x13142E:
  0x13142E -> getter 0x117DFE -> writer 0x12F124
  0x131448 -> getter 0x114342 -> writer 0x12F124
  0x131462 -> getter 0x11434C -> writer 0x12F124
  0x13147C -> getter 0x11486C -> writer 0x12F124
  0x131496 -> getter 0x112CF6 -> writer 0x12F124
  0x1314B0 -> getter 0x112D00 -> writer 0x12F124
  0x1314CA -> getter 0x112B94 -> writer 0x12F124
  0x1314E4 -> getter 0x112D5C -> writer 0x12F124
  0x1314FE -> getter 0x112D66 -> writer 0x12F124
  0x131518 -> getter 0x112DB6 -> writer 0x12F124
  0x131532 -> getter 0x1181F2 -> writer 0x12F124
  0x13154C -> getter 0x118212 -> writer 0x12F124

0x3D1 / Group B 97 / handler 0x131CDA:
  0x131CDA -> getter 0x117D4E -> writer 0x12F124
  0x131CF4 -> getter 0x117DD2 -> writer 0x12F124
  0x131D0E -> getter 0x117E08 -> writer 0x12F124
  0x131D28 -> getter 0x117E1C -> writer 0x12F124
  0x131D42 -> getter 0x118154 -> writer 0x12F124
  0x131D5C -> getter 0x117F70 -> writer 0x12F124
  0x131D76 -> getter 0x117F16 -> writer 0x12F124
  0x131D90 -> getter 0x117F20 -> writer 0x12F124
  0x131DAA -> getter 0x117F2A -> writer 0x12F124
  0x131DC4 -> getter 0x117F34 -> writer 0x12F124
  0x131DDE -> getter 0x117F3E -> writer 0x12F124
  0x131DF8 -> getter 0x117F48 -> writer 0x12F124

0x4C1 / Group B 92 / handler 0x131D42:
  starts at the `0x118154` getter inside the shared `0x3D1`/`0x4D1` chain,
  then continues through `0x117F70`, `0x117F16`, `0x117F20`, `0x117F2A`,
  `0x117F34`, `0x117F3E`, `0x117F48`, `0x117F52`, `0x117F5C`,
  `0x117F66`, and `0x112E7A`.

0x4D1 / Group B 90 / handler 0x131D5C:
  starts one block later, at getter `0x117F70`, then continues through
  `0x117F16`, `0x117F20`, `0x117F2A`, `0x117F34`, `0x117F3E`,
  `0x117F48`, `0x117F52`, `0x117F5C`, `0x117F66`, `0x112E7A`,
  and `0x117016`.
```

The overlap is important: `0x4C1`, `0x4D1`, and `0x3D1` share a contiguous
getter region, with different dispatch entries starting at different offsets.
`0x0C9` also falls through into the `0x3D1` start at `0x131CDA`. This suggests
these object wrappers publish decoded ECU-derived bytes into a common
application/object layer. The next useful static step is to disassemble the
getter functions listed above and identify their RAM sources.

The first byte-level getter classification is generated by
`tools/bcm_group_b_getter_extract.py` and saved in
`analysis/generated/bcm_group_b_wrapper_getters.csv`. Most getters are simple
absolute RAM reads of the form `movhi high -> load offset -> return`. High
`0x0400` with negative offsets resolves back into the `0x03FFxxxx` RAM region.
The short `8457` / `A457` GP-like getters now resolve the same way using an
implicit `0x04000000` base, so they are concrete RAM anchors rather than
unresolved helper noise.
The strongest newly identified RAM clusters are:

```text
0x0C9-derived:
  0x03FF3F91, 0x03FF3E6B, 0x03FF83EB, 0x03FF8391, 0x03FF84BB
  0x03FF84D5, 0x03FF84D7

0x3E9-derived:
  0x03FF5107, 0x03FF5105, 0x03FF5103
  0x03FF43E7, 0x03FF43E3, 0x03FF42C7
  0x03FF43FF, 0x03FF43FB, 0x03FF4189
  0x03FF81C1, 0x03FF81C5
  plus computed getter 0x117DFE

0x3D1/0x4C1/0x4D1 shared:
  0x03FF83DD, 0x03FF83DB, 0x03FF83D9, 0x03FF83D7, 0x03FF83D5, 0x03FF83D3
  0x03FF84BB, 0x03FF84AF, 0x03FF8431, 0x03FF8707
  0x03FF418D, 0x03FF7757
```

These RAM addresses are now better static anchors than the raw CAN IDs. If a
future RAM trace or emulator is available, watch these bytes while replaying
`0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`, and `0x3D1` from the ECU-side logs.

`tools/bcm_ram_anchor_xrefs.py` performs a byte-pattern xref pass for these RAM
anchors and writes `analysis/generated/bcm_group_b_ram_anchor_xrefs.csv`. The
current pass finds 80 matching read xrefs: 57 long absolute-access loads plus
23 compact GP-relative loads. No store-like candidates have been found in these
simple encodings. Notable repeated read clusters:

```text
0x3E9 speed/object bytes:
  0x03FF5107 read at 0x114338 and 0x114342
  0x03FF5105 read at 0x11434C and 0x114876
  0x03FF5103 read at 0x11486C
  0x03FF43E3..0x03FF43FF and 0x03FF4189 read around 0x112B94..0x112DB6

0x3D1/0x4C1/0x4D1 shared status bytes:
  0x03FF83DD, 0x03FF83DB, 0x03FF83D9, 0x03FF83D7, 0x03FF83D5, 0x03FF83D3
  read in paired AA57/8A57 getter blocks around 0x117F16..0x117F7A

0x0C9 / high-rate engine path:
  0x03FF84D5 read at 0x111E22 and 0x111E28
  0x03FF84D7 read at 0x111E2E and 0x111E34
  0x03FF83EB read at 0x117B7A and 0x117F84
  0x03FF8391 read at 0x117B3E and 0x118140
  0x03FF84BB shared with 0x3D1 at 0x117D4E

0x3E9 speed/object short-GP bytes:
  0x03FF81C1 read at 0x0D6706, 0x1181F2, and 0x11825A
  0x03FF81C5 read at 0x11820C and 0x118212
```

Because no simple store-pattern xrefs were found, the next static job is to
disassemble or pattern-match the computed/branchy getters and upstream decode
callbacks that populate these RAM bytes, especially the `0x3E9` cluster at
`0x03FF5103..5107` and the shared `0x03FF83D3..83DD` status cluster.

`tools/bcm_unresolved_getter_windows.py` now dumps the only two remaining
computed/branchy Group B getter windows into
`analysis/generated/bcm_group_b_unresolved_getter_windows.csv`. Those two
entries are not opaque anymore: both start with a short branch/skip sequence and
then fall into a dense RAM-read region. The window around `0x117DFE` /
`0x117E08` references:

```text
0x03FF8651, 0x03FF8431, 0x03FF842B, 0x03FF84B1
0x03FF8789, 0x03FF8799, 0x03FF879D, 0x03FF8739
0x03FF83CF, 0x03FF86F1, 0x03FF83E9
```

That makes the unresolved `0x3E9` first byte and `0x3D1` third byte entries
look like conditional selector/validity reads into the same shared body/status
RAM neighborhood, not raw CAN payload extraction.

`tools/bcm_ram_anchor_loose_contexts.py` adds a deliberately loose second pass:
it searches every low-16-bit RAM offset occurrence for the Group B RAM anchors,
marks offsets already explained by structured load patterns, and records the
two bytes immediately before the offset as a tentative access opcode. This
generated 392 contexts, with 312 not already explained by the known load
patterns. The interesting subset is the 101 contexts whose preceding opcodes
fall into possible write/bit-operation families (`*5F`, `*67`, `*6F`, `*77`,
`*7F`, `*87`, `*8F`). The densest candidates are:

```text
0x03FF84BB  shared 0x0C9/0x3D1 anchor: 33 possible write/bitop contexts
0x03FF81C1  0x3E9 anchor:               8 possible write/bitop contexts
0x03FF81C5  0x3E9 anchor:               4 possible write/bitop contexts
0x03FF8431  0x3D1 anchor:               3 possible write/bitop contexts
0x03FF43E7, 0x03FF43FB, 0x03FF43FF:     smaller 0x3E9 candidate clusters
```

This is not proof of stores yet, but it gives a much better next manual pass:
start around flash offsets `0x0497xx..0x04A7xx` for the shared
`0x03FF84BB` state machine and around the `0x118xxx`/`0x119xxx` windows for
the `0x3E9` short-GP speed/object anchors.

`tools/bcm_ram_anchor_target_report.py` summarizes that loose context CSV into
`analysis/generated/bcm_group_b_ram_anchor_target_report.md`. The current top
manual targets are:

```text
0x03FF84BB / 0x0C9 and 0x3D1:
  33 possible write/bitop contexts.
  Best repeated windows: 0x049B18-0x049B9E, 0x049C02-0x049C88,
  0x049CEC-0x049D72, 0x049DD6-0x049E5C, plus nearby 0x04A0xx..0x04A7xx.

0x03FF81C1 / 0x3E9:
  8 possible write/bitop contexts.
  Candidate windows around 0x0D61E6, 0x0D67D2, 0x0D6F3C,
  0x0D716E, 0x118204, and 0x11873A.

0x03FF81C5 / 0x3E9:
  4 possible write/bitop contexts.
  Candidate windows around 0x0D63A6, 0x0D6A62, and 0x11932E.
```

`tools/bcm_group_b_target_windows.py` extracts those highest-value windows into
both CSV and markdown:
`analysis/generated/bcm_group_b_target_windows.csv` and
`analysis/generated/bcm_group_b_target_windows.md`. This produces 56 focused
byte windows from the raw flash. The most interesting repeated block family is
now visible around `0x049B18..0x04A7F2`; it repeatedly touches
`0x03FF84BB` through opcode forms such as `848F`, `A487`, `A48F`, `846F`,
`A467`, `847F`, `A477`, and `845F`.

Practical next Ghidra/manual sequence:

```text
1. Import/label flash offsets 0x049B18, 0x049C02, 0x049CEC, 0x049DD6,
   0x049F A6, 0x04A0B6, 0x04A1A2, 0x04A29A, 0x04A382, 0x04A46C,
   0x04A59C, 0x04A698, and 0x04A7B2.
2. Treat these as repeated state-machine / table-driven access blocks around
   shared anchor 0x03FF84BB.
3. Separately inspect 0x0D61E6, 0x0D67D2, 0x0D6F3C, 0x0D716E,
   0x118204, and 0x11873A for the 0x3E9 speed/object anchor 0x03FF81C1.
4. Inspect 0x0D63A6, 0x0D6A62, and 0x11932E for the 0x3E9 anchor
   0x03FF81C5.
```

`tools/bcm_group_b_state_machine_clusters.py` then deduplicates frame aliases
and clusters loose contexts whose RAM-offset hits occur exactly six bytes
apart. This confirms that `0x03FF84BB` is not random noise:

```text
0x03FF84BB: 21 deduplicated clusters, 12 paired +6 clusters
common pair: 848F,A487
variant pairs: 846F,A467; 847F,A477; 8487,A47F
```

The repeated pairs are:

```text
0x049B58 / 0x049B5E
0x049C42 / 0x049C48
0x049D2C / 0x049D32
0x049E16 / 0x049E1C
0x049FE6 / 0x049FEC
0x04A0F6 / 0x04A0FC
0x04A1E2 / 0x04A1E8
0x04A2DA / 0x04A2E0
0x04A3C2 / 0x04A3C8
0x04A4AC / 0x04A4B2
0x04A6D8 / 0x04A6DE
0x04AB10 / 0x04AB16
```

This is now the clearest candidate for a shared generated status/state machine
fed by the high-rate `0x0C9` path and reused by the `0x3D1` chain.

`tools/bcm_group_b_window_ram_map.py` scans those focused target windows for
all recognized short-GP and absolute RAM access shapes. It writes
`analysis/generated/bcm_group_b_window_ram_map.csv` and
`analysis/generated/bcm_group_b_window_ram_map.md`. This turns the state-machine
candidate into an apparent local RAM struct instead of isolated bytes. The most
common addresses inside the target windows are:

```text
0x03FF84BB  74 hits   primary shared 0x0C9/0x3D1 anchor
0x03FF8081  47 hits   common status/selector flag used by many windows
0x03FF84B9  36 hits   neighboring state byte, often paired with 0x84BB
0x03FF81C3  16 hits   0x3E9 short-GP neighborhood
0x03FF81C1   8 hits   0x3E9 target anchor
0x03FF81C5   5 hits   0x3E9 target anchor
0x03FF84C3..0x03FF84CF repeated nearby 0x84BB struct members
```

The repeated `0x03FF84BB` windows nearly always touch:

```text
0x03FF8081
0x03FF84B9
0x03FF84BB
one of 0x03FF84C3 / C7 / C9 / CB / CD / CF
```

So the best current interpretation is a generated state machine over a compact
RAM struct around `0x03FF84B9..0x03FF84CF`, gated or indexed by
`0x03FF8081`. This is a strong candidate for the BCM's internal decoded
engine/status state that later feeds IPC-facing body/status builders.

`tools/bcm_group_b_to_group_a_bridge.py` now checks those Group B RAM anchors
against the known Group A IPC/body builders using only recognized RAM-access
instruction shapes. The refined scan is intentionally stricter than the first
loose pass: it no longer treats arbitrary low-16-bit byte matches as evidence.
The current output is:

```text
Focus RAM anchors scanned: 20
Total structured access hits: 1652
Hits within 0x500 bytes of a focus Group A builder: 0
Access kinds: absolute=3, short-gp=1649
```

That removes the earlier noisy 49k-row false-positive bridge and says there is
no direct read of the decoded Group B state inside the final focus Group A
payload builders. The likely bridge is still indirect: Group B receive handlers
update intermediate application state, and the Group A builders then consume
that state through helper/getter layers.

The direct IPC low-speed object-family getters already identified a useful RAM
cluster that may be fed by this RX path:

```text
0x03FF3135, 0x03FF34A0, 0x03FF34B4, 0x03FF34C8, 0x03FF3588, 0x03FF35D8
0x03FF3774, 0x03FF3778, 0x03FF377C
0x03FF5238, 0x03FF5240, 0x03FF535C
0x03FE8A54..0x03FE8A83
```

Those addresses show up as getter sources for the live `0x1024xxxx`,
`0x103Bxxxx`, `0x103Cxxxx`, `0x1042xxxx`, `0x1043xxxx`, `0x1060xxxx`,
`0x1077xxxx`, `0x1081xxxx`, `0x1084xxxx`, and `0x10ACxxxx` object families.
Tracing writes to these RAM locations is now more promising than fuzzing more
zero payloads.

The CAN service code around `0x02E66C..0x02ECxx` is now a better RX-path anchor
than the `0x021A40` wrapper table. It repeatedly calls:

```text
0x34BD2  SFR/register helper
0x34AAE  5-byte CAN command queue helper
```

That service path stages controller bytes in RAM blocks such as `0x03FF0F10`,
`0x03FF1140`, `0x03FF117A`, and `0x03FF11CC`. This looks like raw CAN hardware
buffer management, not a per-ID application handler yet. The per-ID decode
should be downstream of this service path.

Another useful discovery is the selector trampoline table around `0x10B5xx`.
Entries load selector-like values such as `0x90A9`, `0x90A8`, and `0x909C` into
`r7`, then jump to the generic object manager at `0x12E118`. That manager
searches dynamic tables rooted near `0x03FFB4DC`, calls a registered function,
and updates status through `0x03FF9C8C`. This likely explains how some live
low-speed getter values are accessed indirectly:

```text
0x10B5E8 -> selector 0x90A9 -> 0x12E118
0x10B5F0 -> selector 0x90A8 -> 0x12E118
0x10B630 -> selector 0x909C -> 0x12E118
```

Those selectors line up with live IPC status RAM around `0x03FF909C`,
`0x03FF90A8`, and `0x03FF90A9`. They are not direct writers, but they are good
labels for the next Ghidra pass.

## Group A IPC/Body Objects

These entries come from `analysis/generated/bcm_group_a_object_metadata.csv`.
All are clean standard-ID encodings in Group A.

| ID | Index | DLC | Payload RAM | Aux builder | Current role guess |
|---:|---:|---:|---:|---:|---|
| `0x514` | 105 | 8 | `0x03FFA9EC` | `0x127EDC` | config/range/VIN-adjacent |
| `0x4E1` | 107 | 8 | `0x03FFA9FC` | `0x1280D8` | config/range/VIN-adjacent |
| `0x451` | 110 | 6 | `0x03FFAA10` | `0x128D54` | display/body status |
| `0x1F1` | 118 | 8 | `0x03FFAA48` | `0x12833C` | ignition/environment/status |
| `0x160` | 120 | 5 | `0x03FFAA58` | `0x12929A` | compact power/body state |
| `0x139` | 123 | 8 | `0x03FFAA68` | `0x129832` | body/display state |
| `0x137` | 124 | 8 | `0x03FFAA70` | `0x1298BC` | packed multi-signal status |
| `0x135` | 125 | 8 | `0x03FFAA78` | `0x129946` | packed multi-signal status |
| `0x12A` | 127 | 8 | `0x03FFAA84` | `0x1294D4` | door/belt/body/DIC status |
| `0x0F1` | 130 | 6 | `0x03FFAA94` | `0x12903A` | high-rate body sync/state |

`tools/bcm_group_a_payload_writer_map.py` now scans the selected payload RAM
buffers directly, using the same recognized RAM-access instruction shapes as
the Group B scans. This checks whether other firmware regions write directly
into the final Group A payload byte buffers.

Current result:

```text
Focus Group A rows: 10
Payload byte addresses scanned: 73
Total structured payload-buffer hits: 2
Hits within 0x500 bytes of a focus Group A builder: 0
```

Both hits are for `0x1F1` byte 3 at `0x03FFAA4B`, around `0x106E2E` and
`0x106E3A`, far away from the selected final builders. No direct structured
accesses were found near the builders for `0x0F1`, `0x12A`, `0x135`, `0x137`,
`0x139`, `0x160`, `0x451`, `0x4E1`, or `0x514`. This supports the existing
builder interpretation: the final builders mostly fill payloads through
pointer-oriented helper/writer calls, not direct stores to fixed `0x03FFAAxx`
addresses.

`tools/can_native_body_seed_miner.py` mines saved non-live Corsa E captures for
real observed payloads on the same standard Group A body candidates. The output
is `analysis/generated/native_body_seed_summary.md`. The strongest replay
seeds are:

```text
0F1  000000400000 / 1C0000400000 / 280000400000 / 340000400000 at ~15 ms
451  000000000000 at ~18 ms
12A  0006606B00000080, 0000606B00000080, 0000605900000080,
     0000605D00008080, 0006605E00008080, 0006605D00008080 at ~96 ms
135  04080A0002180000 / 04080D0002180000 and lower-count variants at ~96 ms
137  0000000000000000 / 0000000030000000 at ~96 ms
139  0000000000000000 at ~96 ms
160  803C96B503, with occasional 0000000000, at ~96 ms
1F1  AE0F173E18000072 plus lower-count 800E/850F/AB0F variants at ~96 ms
4E1  4B34303238383139 at ~1 s
514  3056305845503638 at ~1 s
```

These real payloads now back the `ipc-native-body-seed-probe` profile. Unlike
the wait-for-RX priority profiles, it does not wait for IPC traffic; it can be
started just before applying ignition/run power so native BCM-like body context
is already on the bus if the IPC wakes.

## Builder Findings

### `0x1F1` Builder `0x12833C`

This is a dense status bitfield writer into `0x03FFAA48`. It calls many getters
and writes individual bits across bytes 0, 1, 4, and 7.

Useful helper calls seen:

```text
0x114A38  -> 2-bit/3-bit state, shifted into payload byte 1
0x114A0A  -> scaled value path through 0x0F3C62 and 0x106E46
0x1149E6  -> validity/state compare against 0x81 and 0x24
0x114A2E  -> payload byte 1 bit 1 and byte 7 bit 4
0x116B26  -> payload byte 7 bit 1
0x1195F2  -> payload byte 0 bit 7 and byte 7 bit 5
0x117E58  -> payload byte 1 bit 0
0x1199AC  -> payload byte 0 bit 2
```

This frame is still one of the highest-value bench prerequisites because it
looks like ignition, environment, and validity state rather than a display
payload.

### `0x160` Builder `0x12929A`

This compact 5-byte frame writes into `0x03FFAA58`. The clearest writes are to
payload byte 4 (`0x03FFAA5C`):

```text
0x117E80  -> sets/clears byte 4 bit 1
0x117F7A  -> sets/clears byte 4 bit 0
0x117E6C  -> helper path through 0x106DBC
0x117F02  -> helper path through 0x106D98
```

This makes `0x160` look like a compact power/body-mode frame, not gauge data.

### `0x0F1` Builder `0x12903A`

This is a high-rate 6-byte frame at `0x03FFAA94`. It combines fast status bits
with two scaled numeric slots and a state/counter nibble.

Observed writes:

```text
byte 3 bit 6         from 0x118EFC
byte 0 bits 2..3     from 0x118E94
byte 1               scaled/saturated value after 0x118EB8
byte 2               scaled/saturated value after 0x118EDC
byte 0 bits 4..5     conditional on RAM flag 0x03FFB3CF == 1
byte 3 bits 4..5     from absolute state 0x03FFAB32 & 0x03
```

The helper group around `0x118Dxx..0x118Fxx` is now a good reverse-engineering
target. If the cluster requires a coherent BCM body heartbeat before accepting
gauge data, this frame is a prime suspect.

### `0x12A` Builder `0x1294D4`

This 8-byte builder writes directly into `0x03FFAA84` and is one of the richest
body/DIC-looking status frames.

Key patterns:

```text
0x112BD0 -> scaled through 0x0F3DA8 / 0x0F3C62, stored with validity via 0x106AEA
0x112BC6 -> validity compare against 0x81 and 0x24
0x1140C4 -> byte 2 bit 6
0x11189C -> byte 1 bit 1
0x111C7E -> byte 7 bit 7
0x114028 -> byte 1 bits 5..7
0x112DF2 -> byte 2 low nibble
0x120466 -> byte 6 bit 4
0x11401E -> byte 2 bit 4
0x114032 -> byte 2 bit 5
0x1117F6 -> byte 1 bit 2
0x11184C -> byte 1 bit 0
```

It also reads packed RAM around `0x03FFAA50`, `0x03FFAB28`, and `0x03FFAB30`
style addresses and folds those bits into bytes 0, 4, 5, and 6. This is more
likely door/belt/body/DIC state than direct speed/RPM.

### `0x135`, `0x137`, `0x139` Builders

These three adjacent builders form a small packed-status cluster:

- `0x139` at `0x129832` writes `0x03FFAA68` using packed words near
  `0x03FFA9E4`, `0x03FFAB08`, and `0x03FFAB68`. It mostly moves bitfields and
  nibbles, with no obvious large gauge value.
- `0x137` at `0x1298BC` calls helper/writer pairs `0x10641A -> 0x106BF4`,
  `0x1063EC -> 0x106CFE`, `0x106508 -> 0x106C34`, `0x1064CC -> 0x106CB8`,
  and `0x106540 -> 0x106C76`, then folds `0x11956C & 0x7` into payload byte 1.
- `0x135` at `0x129946` is richer. It copies packed bytes from RAM blocks
  `0x03FFAB10` and `0x03FFAB54` into payload `0x03FFAA78`, then adds status
  from helpers `0x11675C`, `0x118D40/0x118D4A`, `0x1117C8`, `0x1117BE`,
  `0x118A60/0x118A6A`, `0x119576`, `0x112228`, and `0x1120A4`.

These look like body/display state packing functions. They should stay in the
bench profile set, but they are less likely to be the missing needle-motion
message than the ECU RX-to-RAM path.

### `0x451` Builder `0x128D54`

This 6-byte frame writes `0x03FFAA10`. It stores a byte from nearby RAM into
payload byte 4 and sets/clears payload byte 2 bits 6 and 7 from absolute status
flags. It also calls `0x1065EC` and `0x1070E6`.

Current guess: display/body status, possibly useful for DIC state or telltales.

### `0x4E1` Builder `0x1280D8`

This builder writes into `0x03FFA9FC` and appears to be config/range-like:

```text
0x1199EA -> if nonzero, value + 9 -> 0x10719A
0x1143C8 -> scale via 0x0F3CD8, divide by 64, clamp 0..255 -> 0x10705A
```

The function immediately after it also packs multiple 2-bit fields from helpers
around `0x116ED6..0x117066`. This area looks more like config/status than a
direct gauge command.

### `0x514` Builder `0x127EDC`

This is adjacent to the same config/range family as `0x4E1`:

```text
0x1199EA -> if nonzero, value + 1 -> 0x10720E
0x11895C/0x118952 -> scaled/validity path -> 0x1072E8
0x118948/0x11893E -> scaled/validity path -> 0x1072BA
0x1188DE/0x1188D4 -> scaled/validity path -> 0x10728A
```

This still fits VIN/config/range periodic data better than a live gauge value.

## Practical Bench Implications

The failed direct needle tests make more sense with the static evidence:

- The standard 11-bit Group A frames are real BCM outputs, but the visible gauge
  values are probably not a single raw `0x0C9` or `0x3E9` replay on the IPC wire.
- The direct IPC 29-bit frames seen on the bench are mostly IPC-origin source
  `0x60` status/capability frames; replaying source-like or zero payloads is not
  expected to move needles.
- The BCM builders rely heavily on internal RAM state and validity flags.
  Gauge movement may require coherent prerequisite state plus translated values
  written by ECU RX handlers before the Group A builders run.
- Live checks on 2026-05-29 after removing `621#0140000000000000` from normal
  profiles showed the current bench state was silent. A quiet `default-bench`
  run with only `100#`, `13FFE040#`, and `621#0040...` produced local echo only.
  A controlled delayed `100#` then `621#0140...` restart-isolation run also
  produced local echo only and no IPC boot/status burst. Earlier captures still
  prove `621#0140...` can force the IPC startup burst, so this later result
  should be treated as a current power/wiring/IPC-state observation rather than
  a contradiction of the trigger finding.
- `cantool` now supports `--no-flush` for `capture` and `send-profile` because
  the old 300 ms stale-frame drain can discard immediate post-open IPC chatter.
  It also supports `--log-flush`, which proved the 63-frame flush observed after
  a quiet `default-bench` run was buffered local echo from the prior TX schedule
  (`10050040`, `10002040`, `13FFE040`, `621#0040...`, and battery frames), not
  IPC-origin traffic. A second `--log-flush` capture immediately afterward
  drained zero frames.
- TX runs now perform a short logged final RX drain before exit. A three-frame
  `13FFE040#` smoke test kept all local echoes in its own log and the immediate
  follow-up `--log-flush` capture drained zero frames, so future stale-frame
  counts should be much less ambiguous.
- `summarize --ignore-tx-echo` now removes RX frames that exactly match logged
  `# tx` comments. Recent quiet bench TX logs reduce to zero non-echo frames
  with this option, and a fresh 15 second `capture --log-flush` at 19:42 also
  saw zero frames. Current bench state remains electrically/tooling-clean but
  IPC-silent.
- `bench-health --seconds 5` was added as a repeatable pre-flight check. The
  19:45 run found one candleLight adapter, captured zero passive frames, sent
  three harmless `13FFE040#` frames with three exact local echoes, then captured
  zero follow-up frames. This proves the adapter TX/RX path is working and not
  leaking echo backlog, while the IPC/bus remains silent in the current bench
  state.
- `ipc-wake-recovery-probe` was added for the current silent-IPC state. It does
  not wait for first RX; it sends slow `100#` wake frames, sparse
  `621#0140...` retries, steady `13FFE040#` / `621#0040...`, `10002040#03`,
  key/ignition context candidates, and isolated `24C` diagnostic pings. The
  first 30 second run on 2026-05-29 still produced only local echoes and no
  `...060`, `64C`, or `54C` IPC-origin traffic. A follow-up `--log-flush`
  capture drained delayed local echoes, and the next clean capture saw zero
  frames. This strengthens the suspicion that the IPC is not electrically awake
  or not currently connected to the active SWCAN side, rather than merely
  missing a higher-level message.
- `ipc-wake-first-probe` is now the clean first TX profile for that state. It
  sends no diagnostics: only slow `100#`, sparse `621#0140...`, steady
  `13FFE040#` / `621#0040...`, `10002040#03`, and key/ignition context
  candidates. A 20 second live run at 20:26 produced only three local echoes
  from the startup batch; `summarize --ignore-tx-echo` reduced the log to zero
  non-echo frames. A following `bench-health --seconds 3` captured those delayed
  local echoes in the passive startup drain, then the clean-check capture saw
  zero frames. The current bench therefore still needs a physical/electrical
  wake fix before wait-for-first-RX profiles can run.
- A 21:00 wake-only isolation run added one more data point:
  `ipc-wake-pulse-only-probe` sends only `100#` once per second, with no
  `621#0140...`, no power-mode frame, no diagnostics, and no body context. The
  8 second run `cantool_tx_rx_20260529_210001.candump` sent eight `100#` frames,
  saw three exact local echoes, and reduced to zero frames with
  `summarize --ignore-tx-echo`. A 12 second `ipc-wake-recovery-probe` run
  immediately afterward (`cantool_tx_rx_20260529_210054.candump`) sent `100#`,
  sparse `621#0140...`, `13FFE040#`, `621#0040...`, `10002040#03`, key/ignition
  candidates, and eight `24C` diagnostic requests; it also reduced to zero
  non-echo frames. `analyze-diag` found no `64C` or `54C` response, and
  `analyze-restart --verbose` found no IPC boot/status burst after any TX event.
  Current conclusion: software wake frames and BCM/body-context guesses are
  reaching the adapter, but the IPC is not electrically awake or not connected
  to the active low-speed GMLAN side in the current bench setup.
- `ipc-power-toggle-watch` was added for the next physical test. It is a
  normal-mode, no-TX, ACKing capture like `ipc-ack-sniff`, but defaults to
  no startup flush so the first pin-8 toggle chatter cannot be drained away. The
  first sample run at 21:04 intentionally captured 63 buffered local echoes from
  the preceding recovery probe; the immediate second run
  `cantool_ipc-power-toggle-watch_rx_20260529_210441.candump` captured zero
  frames. This confirms the bench is clean and ready for the physical run/crank
  toggle test.
- `analyze-wake` was added to classify the next physical wake logs. It flags
  clean silence, stale local-echo backlog, exact TX echoes only, and IPC wake
  evidence. It treats extended source-`0x060` frames, known IPC boot/status IDs,
  `62C`, and diagnostic `64C` / `54C` responses as wake evidence. Validation
  runs classified `cantool_ipc-power-toggle-watch_rx_20260529_210424.candump`
  as stale backlog, `cantool_ipc-power-toggle-watch_rx_20260529_210441.candump`
  as clean silent, and `cantool_tx_rx_20260529_210054.candump` as exact local
  echoes only. It now also prints the next command for each verdict: repeat
  `wake-watch` for silence/stale backlog, re-check power/GMLAN pins for TX-echo
  only, or move to `wake-then-profile` diagnostic/native-context/simulator
  runs once IPC wake evidence appears.
- Analyzer commands now support `--latest` plus an optional `--pattern` glob.
  The fastest post-toggle check is:
  `analyze-wake --latest --pattern "cantool_ipc-power-toggle-watch_rx_*.candump"`.
  On the current clean baseline this selects
  `cantool_ipc-power-toggle-watch_rx_20260529_210441.candump` and reports
  `clean silent capture; no IPC wake evidence`.
- `wake-watch` is now the preferred single command for physical wake testing.
  It opens a normal-mode no-TX capture, discards existing adapter backlog for
  300 ms by default, prints `ARMED`, then records the actual test window and
  runs `analyze-wake` on the produced log when the capture ends. Wait for
  `ARMED`, then toggle IPC pin 8 run/crank +12 V.
- `wake-then-profile` was added for the post-fix workflow. It preclears stale
  RX, waits for the first IPC frame, then starts a selected TX profile such as
  `ipc-simulator`. Use this after `wake-watch` proves the physical pin-8 /
  low-speed GMLAN wiring is awake, so simulator or fuzz traffic is not sent into
  a sleeping IPC.
- Fresh armed baseline at 21:21:
  `cantool_wake_watch_20260529_212100.candump` precleared zero stale frames,
  captured for 10 seconds in normal ACKing/no-TX mode, and saw zero RX frames.
  `analyze-wake --latest --pattern "cantool_wake_watch_*.candump"` selected
  that log and reported `clean silent capture; no IPC wake evidence`.
- Full guided physical wake matrix at 21:36:
  `cantool_wake_matrix_20260529_213614.candump` ran six labelled phases:
  pin 3 passive, pin 4 passive, pin 3 with `100#` once per second, pin 4 with
  `100#` once per second, pin 3 with `100#` plus sparse
  `621#0140000000000000`, and pin 4 with the same active wake/network-init
  frames. It sent 56 frames and saw only three exact local echoes from the first
  pin-3 `100#` pulses. `summarize --ignore-tx-echo` reduced the log to zero
  frames, and `analyze-wake` reported `only exact local echoes of this log's TX
  frames; no IPC wake evidence`. This is the strongest current evidence that
  the adapter is transmitting but the IPC is not electrically awake, not tied to
  the active low-speed GMLAN conductor, or the CANable high-speed transceiver
  hack is insufficient to produce the required single-wire physical signalling
  for a sleeping IPC.
- Manual-advance wake-matrix follow-up at 21:41/21:42 changed the picture:
  three short stop-on-wake runs all saw real IPC-origin source-`0x060` traffic
  during the first `pin3-passive` phase, before any tool TX:

```text
cantool_wake_matrix_20260529_214132.candump -> 13FFE060# in pin3-passive
cantool_wake_matrix_20260529_214149.candump -> 10424060#079964 in pin3-passive
cantool_wake_matrix_20260529_214211.candump -> 13FFE060# in pin3-passive
```

  This confirms the active low-speed GMLAN conductor is IPC pin 3 for the
  current wiring and that the IPC can transmit while the adapter is in normal
  ACKing/no-TX mode. The immediate automated diagnostic follow-up,
  `wake-then-profile --profile ipc-diagnostic-gmlan-classic-probe --seconds 36
  --wait-rx-timeout-ms 8000`, timed out with zero RX and sent zero TX. That
  means the next diagnostic/profile run should be armed before the same pin-8
  toggle or awake interval that produces `13FFE060`, rather than started after
  the wake-matrix has already stopped.
- Armed diagnostic run at 21:44 succeeded:
  `cantool_wake_then_ipc-diagnostic-gmlan-classic-probe_tx_rx_20260529_214409.candump`
  waited 27 seconds, caught `13FFE060#`, then transmitted the quiet classic
  diagnostic probe. The log contains 93 TX comments, 198 RX frames, and 105
  non-echo RX frames. It confirms:

```text
24C#021003AAAAAAAAAA -> 64C#0150AAAAAAAAAAAA    session/init accepted
24C#023E00AAAAAAAAAA -> 64C#037F3E12AAAAAAAA    tester-present subfunction rejected
24C#021004AAAAAAAAAA -> 64C#037F1012AAAAAAAA    wake-up-links/session 0x04 rejected
24C#021A91AAAAAAAAAA -> 64C#037F1A31AAAAAAAA    classic DID 0x91 out of range
24C#021A97AAAAAAAAAA -> 64C#037F1A31AAAAAAAA    classic DID 0x97 out of range
24C#02AA00AAAAAAAAAA -> 54C#0000000000000000    AA00 all-zero 54C anomaly repeated
24C#02AA02AAAAAAAAAA -> 64C#037FAA12AAAAAAAA    AA02 subfunction rejected
```

  The broad classic profile also overlaps periodic `023E00` with some read
  requests, so a cleaner isolated run is still needed for `1A90`, `1A92`,
  `1A9A`, and `AA01`. An immediate isolated follow-up at 21:45 timed out after
  30 seconds with zero RX and sent zero TX, again showing that diagnostic runs
  must be armed before the physical pin-8 wake/talk interval.
- Clean isolated classic diagnostic run at 21:49 resolved that ambiguity:
  `cantool_wake_then_ipc-diagnostic-isolated-classic-probe_tx_rx_20260529_214927.candump`
  caught first RX `13FFE060#`, then sent one diagnostic request every roughly
  2.5 seconds with no periodic `023E00` overlap. The log contains 113 TX
  comments, 236 RX frames, and 123 non-echo RX frames. The important confirmed
  responses are:

```text
24C#021003AAAAAAAAAA -> 64C#0150AAAAAAAAAAAA    session/init accepted
24C#013EAAAAAAAAAAAA -> 64C#017EAAAAAAAAAAAA    tester-present without subfunction accepted
24C#023E80AAAAAAAAAA -> 64C#037F3E12AAAAAAAA    suppress-positive tester-present rejected
24C#021A90AAAAAAAAAA -> 64C#10135A9057304C30... positive VIN, decoded W0L0XEP68G4104545
24C#021A91AAAAAAAAAA -> 64C#037F1A31AAAAAAAA    classic DID 0x91 out of range
24C#021A92AAAAAAAAAA -> 64C#037F1A31AAAAAAAA    classic DID 0x92 out of range
24C#021A97AAAAAAAAAA -> 64C#037F1A31AAAAAAAA    classic DID 0x97 out of range
24C#021A9AAAAAAAAAAA -> 64C#045A9A0A5FAAAAAA    positive DID 0x9A, data 0A 5F
24C#02AA00AAAAAAAAAA -> 54C#0000000000000000    AA00 all-zero 54C anomaly confirmed
24C#02AA01..0F       -> 64C#037FAA12AAAAAAAA    AA packet IDs 0x01-0x0F subfunction rejected
```

  This proves the IPC diagnostic pair is `24C` request / `64C` response for
  classic GMLAN single-frame and ISO-TP-style replies. It also proves that this
  IPC wants tester-present as `24C#013EAAAAAAAAAAAA`; the common UDS-shaped
  `24C#023E00AAAAAAAAAA` is consistently rejected with NRC `0x12`.
- Two live control runs at 21:52-21:54 showed why these diagnostic profiles
  must still be armed before physical wake. A read-only `wake-watch --seconds
  30 --stop-on-wake` saw zero frames. Then `ipc-wake-first-probe --seconds 12`
  sent 196 wake/body-context frames and saw only three exact local echoes, and
  `ipc-wake-pulse-only-probe --seconds 8` sent eight `100#` pulses and again
  saw only three exact local echoes. Software TX and local echo are healthy,
  but TX-only wake does not bring the IPC up from the current silent state;
  applying/toggling pin 8 run/crank remains the reliable trigger.
- Follow-up armed runs at 21:57 and 21:59 timed out without first RX:
  `ipc-diagnostic-classic-1a-scan` and the new short
  `ipc-diagnostic-confirmed-identity-probe` both sent zero TX because no IPC
  frame arrived during their 30 second first-RX windows. The short confirmed
  identity profile was added so the next physical wake window can quickly
  re-check the known-good path (`10 03`, accepted `013E`, VIN `1A90`, `1A9A`,
  and `AA00`) before committing to the longer 1A scan.
- A no-timeout confirmed-identity run launched in a separate PowerShell at
  22:02 did catch first RX and completed:
  `cantool_wake_then_ipc-diagnostic-confirmed-identity-probe_tx_rx_20260529_220237.candump`.
  It reconfirmed `10 03`, accepted `013E`, positive `1A9A`, and `AA00 -> 54C`.
  Its VIN response missed consecutive frame sequence `21`, which is why the
  auto ISO-TP flow-control STmin was changed from zero to `0x0A` for future
  diagnostic runs.
- The repeated confirmed-identity run at 22:06 proved that fix. With
  flow-control `24C#30000AAAAAAAAAAA`, the IPC returned both consecutive frames
  `64C#21...` and `64C#22...`, and `analyze-diag` decoded the full VIN again:

```text
24C#021A90AAAAAAAAAA
64C#10135A9057304C30
24C#30000AAAAAAAAAAA
64C#2158455036384734
64C#22313034353435AA
=> VIN W0L0XEP68G4104545
```

  The same run again confirmed `10 03 -> 64C#0150...`, accepted `013E ->
  64C#017E...`, `1A9A -> 0A 5F`, and `AA00 -> 54C#0000000000000000`.
- A timed `ipc-diagnostic-classic-1a-scan` follow-up at 22:07 missed the wake
  window and sent zero TX. A no-timeout 1A scan was then launched in a separate
  PowerShell at 22:08 and is armed waiting for the next physical pin-8 wake.
- The no-timeout `ipc-diagnostic-classic-1a-scan` did catch the next wake and
  completed:
  `cantool_wake_then_ipc-diagnostic-classic-1a-scan_tx_rx_20260529_220806.candump`.
  It found the following positive classic `0x1A` identifiers:

```text
1A90 -> VIN W0L0XEP68G4104545
1A98 -> 2A313653595044202020                 ascii "*16SYPD"
1A99 -> 20151216
1A9A -> 0A5F
1A9B -> 0008
1A9C -> 0253FD0F
1A9D -> 4141                                ascii "AA"
1A9F -> 2A313653595044202020FFFFFFFFFFFFFFFFFFFF
1AA0 -> 00
1AA1 -> 1F11F0FFFFFF7F0000000000
1AA2 -> FFF7CF0D0000
1AA3 -> A004230000400700
1AA4 -> 0830000200
1AA5 -> 09090000A3001210680A00F987FE050003
1AA6 -> E0A8DA4010E13F
1AA7 -> 00
1AA8 -> 00
1AB0 -> 60
1AB2 -> FFFFFFFFFF
1AB4 -> 35303633363946473146333336303547    ascii "506369FG1F33605G"
```

  Everything else in the scanned ranges `0x80..0xBF` and `0xF0..0xFF` was
  either `7F 1A 31` request-out-of-range or did not answer before the next
  request. The strongest human-readable leads are `1A98/1A9F` (`*16SYPD`),
  `1A99` (`20151216`, likely a date), and `1AB4` (`506369FG1F33605G`, likely
  a part/calibration/software label).

Best next static targets:

1. Trace Group B receive handlers for `0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`, and
   `0x3D1` to their RAM writes.
2. Search for writes into the RAM blocks consumed above, especially
   `0x03FFAA48..0x03FFAA99`, `0x03FFAB10`, `0x03FFAB28`, `0x03FFAB30`,
   `0x03FFAB54`, and `0x03FFAB68`.
3. Reverse the helper clusters:
   `0x1117xx..0x1140xx`, `0x1167xx..0x1199xx`, and the payload writer family
   `0x1063xx..0x1072xx`.
4. Treat `0x1F1`, `0x160`, `0x0F1`, and `0x12A` as the highest-value standard
   prerequisite frames when bench testing.
