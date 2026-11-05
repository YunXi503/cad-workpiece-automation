# Engineering implementation log

This log tracks implementation changes; measured original-case results are separate from analytic fixtures and historical evidence. Original inputs remain local.

## 2026-11-05 — original Case A delivery

- Authorized goal: use original geometry, complete source-aware geometry programs and VSTA editor execution, publish deidentified portfolio without original files.
- Baseline: commit 094f742. Existing public shell classifier assumes near-cap guide positions and the fitter assumes isolated bends; neither is a verified complete original-case route.
- Plan: typed local support evidence -> original triangle/section validation -> reference-offset engineering draft -> general finite G1 line/arc fitting/reduction -> rounding validation -> fingerprint-bound VSTA -> two review rounds -> GitHub main.
- Source/review support domains are explicit input; unknown physical wall/contact identity remains unknown. Reconstructed draft points are never labelled raw observations.
- Success conditions: source identity <=1e-7 mm, finite draft XY <=0.3 mm, rounding movement <=0.05 mm, endpoint/joint and unit tangent difference <=1e-7, actual native readback <=1e-7 mm. Manufacturing fit is a separate, historically unmet condition.
- Initial results were pending; subsequent measured steps are recorded below.

### Source input, first run

- CLI/core built with zero warnings/errors. The first source audit rejected the original reference format `X value Y value Z value`; the old reader only accepted three whitespace-separated numbers.
- Added an explicit labelled-reference parser to the new local-case path, keeping original reference bytes/hashes intact. The legacy fixture parser is unchanged. Source evidence audit is rerun before interpreting any geometry result.

### Source and draft reproduction

- Original STL retained byte-identical (342,369 faces). Re-slicing and reviewed-support validation returned 83 frames: 60 engineering-width candidates, 7 review exclusions, 16 unresolved, 119 source endpoints. Maximum finite source residual 5.684341886080802e-14 mm.
- New reference-offset/natural-spline/3D-resampling implementation matched the independently frozen historical draft: 1,038 / 779 / 1,013 points; maximum XYZ difference 7.11e-15 / 1.21e-13 / 7.35e-14 mm <= unchanged 1e-7. Historical draft was an oracle only, never production input.
- Original supports remain approximate engineering anchors, not certified physical fillet-end points. All 2,830 generated points are derived.

### General fitting and rounding

- Baseline -> reduced primitives: 83->59, 63->41, 81->65; all original/uniform finite XY checks passed 0.3 mm. Rounded maximum XY: 0.29769230404540187 / 0.2941940032187631 / 0.2622031655095236 mm.
- Independent movement upper bounds: 0.008787129314414083 / 0.004677612108471579 / 0.022313970523077323 mm <=0.05. Endpoint/joint and unit tangent checks passed unchanged 1e-7.
- Tradeoff: historical 129 primitives are fewer than new 165, but two historical sides exceed 0.3. No global minimality or overall segment-count improvement is claimed.
- Source-anchor-only 3D contact maximum: 0.279204270483786 / 0.698533564831493 / 1.799857289317415 mm. Last two fail 0.3; actual physical contact band remains unconfirmed.

### Public replay and Windows

- New analytic shell-strip fixture uses radius40.24 and half-width12; an earlier unexecuted radius8.24 draft was rejected during input review because its offset surface folds. No private geometry changed.
- Source acceptance: deterministic fresh-directory replay, 3 rounded sides, 15 actual rejected malformed/tampered inputs, and reused-source identity becomes unresolved.
- Test-harness first run incorrectly passed a stdout/stderr tuple to json.loads; corrected the harness, not the geometry or thresholds. Math test first build used an unavailable MinNormal name; replaced with exact Math.ScaleB(1,-1022).
- Windows CLI/WinForms built without warnings. Existing 7-case suite, 6 arm regressions, STL parser controls and 25 experiments passed. Original Case A viewer opened/painted all three sides; source endpoints and all-frame unresolved coverage remain separate from derived points.
- Figure renderer initially referenced a non-existent top-level sides key; corrected to run.sides/draft.sides. Numerical source outputs unchanged; all three final PNGs visually inspected.

### Linux environment diagnosis

- WHPX failed with unexpected VP exit; retained logs and used TCG. Debian cloud kernel requires virtio block, SCSI CD and network devices: default legacy devices caused missing root/seed/network. Diagnosed missing device support, then booted the unchanged base with a disposable snapshot and all-virtio devices.
- Debian CLI build, math controls, STL parser and reviewed-source acceptance have passed; remaining full regressions/original-case verification are still running. Final receipt will replace this pending status with measured results only.

### VSTA editor, first attempt

- Created an independent C# test library after user completed 3DPassport login. Imported the new public workflow and exact private Case A plan; external .NET Framework C#5 compilation passed.
- First F5 reached DSYVstaDebugHost but reported CATMain not found, consistent with the host's stale blank-project entry. Source contains public static CATMain. No new success receipt was generated. Next diagnostic is editor edit/save and explicit project build to synchronize the fresh host library, without changing geometry or the workflow.

### VSTA source synchronization and capacity

- An actual editor comment edit/save and explicit Build synchronized the new host entry. Next F5 entered CATMain and logged the exact new plan SHA 8ba8142641ee2a5e2ec91a7d56b8d7c0df3bb9e4314e570413dd1227417bb638, then failed ReadPlan before creating CAD: old per-side resource cap was 24 curves, original case needs 59/41/65.
- Single change authorized by the full-case delivery: raise the bounded curve capacity to256 per side, retaining all finite geometry, 0.3/0.05/1e-7 and constraint checks. Prediction: the same plan must pass curve-count preflight; any later native failure is separately diagnosed, not concealed. Original input/plan unchanged.
- Host runtime reports MTA. Record that fact; do not assume VSTA editor entry is STA or reuse external STA evidence as editor evidence.

### VSTA original-case pass

- Capacity-only revision passed with the same input plan SHA. Actual editor Build/F5 produced3 sketches,59/41/65 finite primitives,145/100/160 active valid constraints and3 finite Z[-50,50] extrusions.
- Native maximum residuals:3.2329694477084558e-13 / 6.0291550413456965e-13 / 8.9528384705772623e-13 mm <=1e-7. The workflow re-read the sketches before creating the fingerprint-bound surfaces.
- During execution paused the debugger at the constraint loop and read index63 on the only65-primitive side, confirming progress; continued without changing geometry. Final runtime status=verified, process=DSYVstaDebugHost, entry=editor-catmain, apartment=MTA.
- First viewport export had stale display and no visible geometry; retained privately. CATIA Update then native Reframe/export displayed all3 surfaces. Hid the tree in CATIA and exported an unedited viewport without account/product identifiers. Public receipt contains only whitelisted aggregate fields and hashes.
- New CAD demo does not import or alter the original STL. Local autosave notification was observed; PLM save is not confirmed. Manufacturing contact remains unaccepted.

### Post-run evidence and reader corrections

- Current workflow additionally rejected5 actual external-STA pre-mutation controls: signed-arc reversal, changed plan fingerprint, absent owning-sketch marker, duplicate extrusion attempt and257-curve capacity overflow. All exit1 with expected messages and no success receipts; they are labelled separately from editor F5 proof.
- Full Windows orchestration passed11 steps, including public fixtures, original-case receipt consistency, code/math controls, existing25 experiments and7 Viewer sides. Original STL SHA rechecked unchanged.
- Found a figure-stage error during review: rounded curves were drawn with the pre-round fitting error in the title. Fixed only the plot metrics/labels, recomputed finite distances independently in Python and checked against the C# report within1e-7. Rounded maxima0.297692304045 /0.294194003219 /0.262203165510mm. Moved the shared legend outside axes to avoid covering the lower-side worst point. Geometry unchanged.
- Preliminary reader scores were not accepted while the label error remained. Corrected figures and separate history/source/VSTA entry paths are used for the final two self-review rounds; source data non-publicity remains an explicit limitation.

### Linux verification scopes

- Debian companion run passed CLI build, math controls, STL parser, reviewed-source replay and the seven-case/25-history suite. Its optional six legacy-arm regressions were stopped to release TCG resources for the original-case run; they are not reported as Debian passes. All six have passed on Windows and remain in the required Windows/Ubuntu CI matrix.
- A second disposable Debian snapshot runs the unchanged original private Case A against the same numerical C# source hashes; it has generated3/3 sides and is performing full source/algorithm replay. No original file is sent to GitHub Actions.

### Linux original-case result

- Targeted Debian12/Linux6.1/.NET10.0.403 run finished with PORTFOLIO_CURRENT_EXIT=0 and VERIFIED. Build0 warnings/errors; all3 original-case sides passed independent replay, finite XY, rounding, endpoints/G1 checks. Both disposable guests are stopped; base image was not modified.
- Input fingerprint and numerical C# source hashes match Windows. Counts match59/41/65. Compared scalar metrics agree within4.7178407014403234e-14mm; CAD plan raw byte hashes differ and are retained separately. No bit-identical cross-platform claim or Linux-native CATIA claim.
- Public Linux receipt contains aggregate metrics/source hashes only; original STL/ASC/manifest/CAD plan and complete logs stay local.
