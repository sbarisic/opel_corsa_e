# BCM r2 Targeted Disassembly

This file records the first useful `radare2` pass over `bcm_p_flash.bin` using
the WSL-installed r2 build.

## Invocation

Use raw offsets first. The V850 decoder works directly on this image:

```powershell
wsl sh -lc "cd /mnt/e/Projects/opel_corsa_e && r2 -q -a v850 -b 32 -e cfg.bigendian=false -e scr.color=false -c 'pd 32 @ 0x8260' -c q data/raw/bcm_p_flash.bin"
```

Useful defaults:

```text
-a v850
-b 32
-e cfg.bigendian=false
-e scr.color=false
```

## Processor Confirmation

The bytes at `0x8260` decode as a V850-style vector/stub area. This is strong
local confirmation that the BCM program flash is V850-family code.

```text
0x00008260      8107e8e5       jr 0x26848
0x00008264      0000           nop
0x00008266      0000           nop
0x00008268      0000           nop
0x0000826a      0000           nop
0x0000826c      0000           nop
0x0000826e      0000           nop
0x00008270      8207fe63       jr 0x2e66e
0x00008274      0000           nop
0x00008276      0000           nop
0x00008278      0000           nop
0x0000827a      0000           nop
0x0000827c      0000           nop
0x0000827e      0000           nop
0x00008280      e0074001       reti
0x00008290      e0074001       reti
0x000082a0      e0074001       reti
```

## Reset / Startup Candidate

The `jr 0x26848` target initializes core registers and stack-like state. This is
a reset/startup candidate.

```text
0x00026848      24067805ff03   mov 0x3ff0578, gp
0x0002684e      250648e21300   mov 0x13e248, tp
0x00026854      2306fcefff03   mov 0x3ffeffc, sp
0x0002685a      1c0a           mov -4, r1
0x0002685c      4119           and r1, sp
0x0002685e      4007fcf1       st.b r0, [r0]
0x00026862      400728f8       st.b r0, [r0]
0x00026870      bf0738ff       jr 0x267a8
```

Notes:

- `gp = 0x03ff0578`
- `tp = 0x0013e248`
- `sp = 0x03ffeffc`, then aligned down by `and -4, sp`
- The repeated `st.b r0, [r0]` style instructions are likely SFR/watchdog or
  startup side effects under the V850 memory model, not normal RAM stores.

## Interrupt / Handler Candidate

The `jr 0x2e66e` target is valid V850 code and repeatedly calls `0x34bd2`.

```text
0x0002e66e      031ea0ff       addi -96, sp, sp
0x0002e672      63ff5d00       st.w lp, 92[sp]
0x0002e676      80ff50d4       jarl 0x3bac6, lp
0x0002e67a      400eff03       movhi 1023, r0, r1
0x0002e67e      c13f6e0d       set1 7, 3438[r1]
0x0002e682      23171900       ld.w 24[sp], r2
0x0002e686      80ffacd4       jarl 0x3bb32, lp
0x0002e68a      5c00           switch r28
0x0002e6c8      80ff0a65       jarl 0x34bd2, lp
0x0002e6e6      80ffec64       jarl 0x34bd2, lp
```

This region is worth following because it manipulates byte buffers and calls the
common helper at `0x34bd2`.

## CAN / Buffer Manipulation Candidate

The online GM BCM hint for `0x2e940` is meaningful in this dump. r2 decodes this
as valid V850 code with repeated calls to `0x34bd2`, bit set/clear operations,
and byte stores.

```text
0x0002e940      401700fc       st.b r2, [r0]
0x0002e944      400720fc       st.b r0, [r0]
0x0002e948      6070           sld.bu 0, r14
0x0002e94a      5671           and r22, r14
0x0002e94c      8073           sst.b r14, [ep]
0x0002e94e      06f0           mov r6, ep
0x0002e950      0c32           mov 12, r6
0x0002e952      80ff8062       jarl 0x34bd2, lp
0x0002e956      0032           mov 0, r6
0x0002e958      80ff7a62       jarl 0x34bd2, lp
0x0002e95c      20363e00       movea 62, r0, r6
0x0002e960      80ff7262       jarl 0x34bd2, lp
0x0002e97e      d1370000       set1 6, 0[r17]
0x0002e986      d0b70000       clr1 6, 0[r16]
0x0002e98c      80ff4662       jarl 0x34bd2, lp
0x0002e998      80ff3a62       jarl 0x34bd2, lp
0x0002e9a0      ce370000       set1 6, 0[r14]
0x0002e9a4      cebf0000       clr1 7, 0[r14]
```

Nearby `0x2e88c` also decodes cleanly and appears to extract/pack bits from
byte buffers:

```text
0x0002e88c      6078           sld.bu 0, r15
0x0002e88e      1479           or r20, r15
0x0002e890      807b           sst.b r15, [ep]
0x0002e892      6060           sld.bu 0, r12
0x0002e894      5661           and r22, r12
0x0002e896      8063           sst.b r12, [ep]
0x0002e89a      0232           mov 2, r6
0x0002e89c      80ff3663       jarl 0x34bd2, lp
0x0002e8a6      b75f0300       ld.bu 3[r23], r11
0x0002e8aa      97370500       ld.bu 4[r23], r6
0x0002e8ae      c25a           shl 2, r11
0x0002e8b0      a632           sar 6, r6
0x0002e8b2      0b31           or r11, r6
0x0002e8c6      80ff0c63       jarl 0x34bd2, lp
0x0002e8ca      ca161000       andi 16, r10, r2
0x0002e8d0      c2560100       andi 1, r2, r10
0x0002e8d4      15170300       ld.b 3[r21], r2
0x0002e8de      0a11           or r10, r2
0x0002e8e0      55170300       st.b r2, 3[r21]
```

## Common Helper at 0x34bd2

Many candidate routines call `0x34bd2`. It waits on a byte at `0x03ff11d1`,
writes a halfword through the low/SFR space, then uses `r6` as a selector.

```text
0x00034bd2      0002           callt 0
0x00034bd4      c600           zxh r6
0x00034bd6      408eff03       movhi 1023, r0, r17
0x00034bda      b18fd111       ld.bu 4561[r17], r17
0x00034bde      e089           cmp r0, r17
0x00034be0      bafd           bne 0x34bd6
0x00034be2      603716fd       st.h r6, [r0]
0x00034be6      007a           mov 0, r15
0x00034bee      c0ff52f1       tst1 7, [r0]
0x00034c08      0032           mov 0, r6
0x00034c0a      bfff70fc       jarl 0x3487a, lp
0x00034c14      8600           zxb r6
0x00034c16      e031           cmp r0, r6
0x00034c24      201620fd       movea -736, r0, r2
0x00034c2a      201610fd       movea -752, r0, r2
0x00034c30      201600fd       movea -768, r0, r2
0x00034c38      42570000       st.b r10, 0[r2]
0x00034c3c      7f00           jmp [lp]
```

Working interpretation: `0x34bd2` is likely a hardware/SFR communication helper
or register access helper used by CAN/buffer code. Its callers pass selector
values in `r6`.

## CAN Queue Helper at 0x34aae

`0x34aae` appears to enqueue 5-byte commands into a 16-entry ring at
`0x03ff117a`, then pushes queued bytes to hardware registers at `0x03ff11cc`.
This is probably part of the CAN controller transmit/config command path.

```text
0x00034aae      208e04f1       movea -3836, r0, r17
0x00034ab2      f1870100       ld.hu 0[r17], r16
0x00034aba      22067a11ff03   mov 0x3ff117a, r2
0x00034ac0      90860200       ori 2, r16, r16
0x00034acc      a26f5100       ld.bu 81[r2], r13
0x00034ad0      ed760500       mulhi 5, r13, r14
0x00034ad6      4e370000       st.b r6, 0[r14]
0x00034ae4      4b3f0100       st.b r7, 1[r11]
0x00034af2      50470200       st.b r8, 2[r16]
0x00034b04      4d4f0400       st.b r9, 4[r13]
0x00034b08      425f5100       st.b r11, 81[r2]
0x00034b30      3106cc11ff03   mov 0x3ff11cc, r17
0x00034b54      8073           sst.b r14, [ep]
0x00034b56      816b           sst.b r13, 1[ep]
0x00034b58      8263           sst.b r12, 2[ep]
0x00034b5a      845b           sst.b r11, 4[ep]
```

The calling convention observed near `0x2ea00` passes `r6` as command/register
selector, `r7`/`r8` as value or mask bytes, and `r9` as a channel/object byte.

## Dispatch / Wrapper Table

The region at `0x021A40..0x023258` is a 514-entry table with 12-byte records:

```text
u32 object_key
u32 handler_address
u32 entry_class
```

Many `object_key` values match `raw_value >> 16` from Group A/Group B CAN table
entries. The handler addresses point to tiny wrappers that fetch or compute one
value and write it into a payload buffer via a common helper.

Example record:

```text
0x022244: 0x0000906F 0x00131344 0x00000001
```

`0x00131344` decodes as:

```text
0x00131344      0902           callt 9
0x00131346      06e0           mov r6, r28
0x00131348      07e8           mov r7, r29
0x0013134a      9d00           zxb r29
0x0013134c      beff880a       jarl 0x111dd4, lp
0x00131350      1c30           mov r28, r6
0x00131352      1d38           mov r29, r7
0x00131354      0a40           mov r10, r8
0x00131356      bfffcedd       jarl 0x12f124, lp
0x0013135a      8a00           zxb r10
0x0013135c      1f02           callt 31
```

`0x111dd4` and nearby functions are single-byte getters from `gp`-relative RAM.
`0x12f124` stores one byte into the payload buffer at `r6 + r7` and returns the
next byte index.

That gives a plausible transmit-build pattern:

```text
wrapper(object_buffer, byte_index)
  value = getter_from_state()
  next_index = store_payload_byte(object_buffer, byte_index, value)
```

## Data Regions, Not Code

The important non-table CAN constants around `0x022246` and `0x02249E` should
not be treated as executable code. r2 word dumps show structured records, while
forced disassembly produces invalid instructions and breakpoints.

Example around `0x022246` (`0x4D1 << 18 = 0x13440000`):

```text
0x022238: 0x0000906E 0x0013135E 0x00000001
0x022244: 0x0000906F 0x00131344 0x00000001
```

Example around `0x02249E` (`0x160 << 18 = 0x05800000`):

```text
0x022490: 0x000090A7 0x0013059A 0x00000004
0x02249C: 0x000090A8 0x00130580 0x00000004
```

The apparent `0x4D1` and `0x160` constants are unaligned byte overlaps across
`object_key` and `handler_address` fields, not real CAN constants.

## CAN Working Model

Current firmware model:

```text
Group A / Group B CAN object tables
        |
        | object_key = raw_value >> 16 for many object variants
        v
0x021A40 dispatch/wrapper table
        |
        v
small handler wrappers around 0x130xxx..0x134xxx
        |
        +--> state getters, often gp-relative single-byte reads
        +--> payload writers such as 0x12f124 / 0x12f208
        |
        v
CAN hardware helpers 0x34bd2 and 0x34aae
```

For the IPC work, Group A transmit/body objects are currently easier to follow
than Group B ECU receive objects because the dispatch table links directly to
many Group A object keys. Group B priority IDs (`0x0C9`, `0x3E9`, `0x4C1`,
`0x4D1`, `0x3D1`) still need their receive consumer located through the RX
interrupt/buffer path rather than through the `0x021A40` dispatch table.

The generated static message map now records this distinction explicitly:

```text
analysis/generated/bcm_group_a_tx_trace.csv
analysis/generated/bcm_group_b_rx_trace.csv
analysis/generated/bcm_can_helper_callsites.csv
analysis/generated/bcm_low_speed_static_message_map.md
```

The focused r2 scan currently finds 61 calls to `0x34BD2` and 7 calls to
`0x34AAE` in the selected CAN/buffer ranges. These are enough to anchor the
controller-helper path, but they are not claimed to be exhaustive across the
whole firmware image.

## Next r2 Commands

Use these for the next pass:

```text
pd 64 @ 0x26848
pd 96 @ 0x2e66e
pd 96 @ 0x2e88c
pd 128 @ 0x2e940
pd 96 @ 0x34bd2
pxw 64 @ 0x22226
pxw 64 @ 0x2247e
```

Once the target regions are labelled, run targeted analysis instead of whole
image analysis:

```text
af @ 0x26848
af @ 0x2e66e
af @ 0x2e88c
af @ 0x2e940
af @ 0x34bd2
axt @ 0x34bd2
```
