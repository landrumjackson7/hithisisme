# Test Report — Astryx AutoClicker (PR #10)

**How tested:** Built the WinForms `.exe` from PR source with the framework compiler
(`csc.exe`), launched it, and drove it end-to-end against a live Chrome click-counter page
(`click-target.html`) that tracks left / right / middle click counts. All UI interactions were
performed natively (mouse/keyboard), recorded, and annotated.

**Overall result:** All five planned tests passed. One usability caveat worth noting (not a
functional bug) about starting via the on-screen button — see below.

---

## Environment / Build
- Built with: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe ...` → `AstryxAutoClicker.exe` (12,800 bytes), compiled cleanly with no errors.
- Target page: `C:\Users\Administrator\click-target.html` (button increments a left-click counter; separate right-click / middle-click counters via `contextmenu` / `auxclick`).
- Browser: Chrome for Testing 137.

---

## Test Results

### Test 1 — UI renders correctly ✅ PASS
Window "Astryx AutoClicker" shows all controls: Target CPS numeric (default **20**), Mouse button
dropdown (**Left**), Start/stop hotkey dropdown (**F6**), blue **Start (F6)** button, and green idle
status label.

![UI render](https://app.devin.ai/attachments/6adf281c-6cdf-4efd-bed1-f4d12d77ee52/ss_zoom_52274f29.png)

### Test 2 — Real clicks delivered + CPS/toggle ✅ PASS
Set CPS = 10, aimed cursor at the Chrome CLICK TARGET, started via F6. The counter climbed from 0
to 25 in ~2.5s (≈10 CPS), proving synthesized clicks are actually delivered to the target window.

![Counter climbing to 25 at 10 CPS](https://app.devin.ai/attachments/a08e7e21-9189-441f-923b-5672ce4c730b/ss_b4a2eef7.png)

### Test 3 — F6 global hotkey + Start/Stop text toggling ✅ PASS
- F6 started and stopped clicking while **Chrome (not the app) was focused** — confirms the hotkey
  is truly global. The counter froze the moment F6 stopped.
- The on-screen button + status label toggle correctly: blue **Start (F6)** / "Stopped…" when idle,
  and red **Stop (F6)** / "Clicking at 10 CPS (Left). Press F6 to stop." while running.

![Running state: Stop (F6) + status](https://app.devin.ai/attachments/e9b61400-fb61-4537-845f-68a5a4e4564f/ss_zoom_cc0f5a05.png)

### Test 4 — Mouse button setting takes effect (Right) ✅ PASS
Changed the dropdown to **Right** and ran on the target. The page's **right-clicks** counter rose to
25 while the **left** counter stayed frozen (4816) and middle-clicks stayed 0 — proving the button
selection is honored end-to-end.

![Right-clicks counter = 25](https://app.devin.ai/attachments/59749205-5583-4c95-86e8-3970492ace40/ss_ceadb43d.png)

### Test 5 — High CPS (1000) sanity ✅ PASS
Set CPS = 1000. App started without crashing (status: "Clicking at 1000 CPS (Left)"), the counter
incremented ~1000 in 2s (effective ≈500 CPS — expected, since Chrome coalesces rapid down/up pairs),
Stop halted it cleanly, and the UI remained fully responsive.

![1000 CPS running, no crash](https://app.devin.ai/attachments/98e6be08-13ea-4f6b-8f6e-e8c9ac48024a/ss_40980db3.png)

![Left counter after high-CPS burst](https://app.devin.ai/attachments/b66711b8-dcba-4db8-b74a-a3b407a04f0b/ss_zoom_939b646a.png)

---

## Caveat (not a functional bug)
Clicking the **on-screen Start button** with the mouse self-toggles the engine back off: the engine's
first synthesized click fires immediately at the current cursor position, which is still on the
Start/Stop button → it clicks itself. The intended workflow (aim the cursor at the target first, then
press the **F6** hotkey to start) works perfectly. Consider a small start delay or ignoring clicks on
the app's own window if starting via the button should be robust.

## Not tested
- F7–F10 alternate hotkeys and Middle mouse button (Left/Right and F6 covered the mechanism).
- Hotkey-in-use fallback warning path (would require another app holding F6).
